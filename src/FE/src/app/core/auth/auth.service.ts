import { HttpClient, HttpContext } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import {
  Observable,
  Subscription,
  catchError,
  finalize,
  map,
  of,
  switchMap,
  tap,
  throwError,
} from 'rxjs';

import { MenuStore } from '../menu/menu.store';
import {
  ApiFailureError,
  ApiResult,
  BO_QUA_HET_PHIEN,
  BO_QUA_TOAST_LOI,
} from '../http/api-result.model';
import { unwrapData } from '../http/unwrap';
import { XsrfTokenStore } from './xsrf-token.store';
import { DangNhapPayload, PhienDto } from './phien.dto';
import { NguoiDungHienTai } from './nguoi-dung-hien-tai.model';
import { sangNguoiDungHienTai } from './phien.mapper';
import { TabSessionBroadcastService } from './tab-session-broadcast.service';

/**
 * Giữ CẢ người dùng hiện tại LẪN câu hỏi "có quyền không" — một service cho cùng một trạng thái
 * (quy-uoc/fe-routing-guard.md §3.3). Không tách service phiên/service quyền riêng.
 *
 * 🛑 `MenuStore` KHÔNG được inject `AuthService` ngược lại — vòng DI (NG0200, menu.store.ts).
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly menu = inject(MenuStore);
  private readonly xsrf = inject(XsrfTokenStore);
  private readonly broadcast = inject(TabSessionBroadcastService);

  private readonly _nguoiDung = signal<NguoiDungHienTai | null>(null);
  private goiMe: Subscription | null = null;
  private canNapLaiMenu = false;

  readonly nguoiDung = this._nguoiDung.asReadonly();
  readonly daDangNhap = computed(() => this._nguoiDung() !== null);
  readonly phaiDoiMatKhau = computed(() => this._nguoiDung()?.mustChangePassword ?? false);
  /** Cờ vận hành hệ thống — ĐƯỜNG GÁC RIÊNG, không nằm trong tập quyền (contracts/auth.md §3, §5). */
  readonly laVanHanhHeThong = computed(() => this._nguoiDung()?.isSystemOperator ?? false);

  private readonly tapQuyen = computed(() => new Set(this._nguoiDung()?.quyen ?? []));

  coQuyen(ma: string): boolean {
    return this.tapQuyen().has(ma);
  }

  /**
   * `napPhienKhoiDong()` là lời gọi `me` DUY NHẤT chạy khi `daDangNhap()` còn `false` — chạy song
   * song với `XsrfTokenStore.lamMoi()` trong `provideAppInitializer` (fe-routing-guard.md §3.6).
   * 401 là trạng thái BÌNH THƯỜNG lúc này — không toast, không điều hướng, tự resolve; lỗi
   * KHÁC 401 mới ném tiếp để `app.config.ts` bọc `catchError(() => of(null))` theo đúng mẫu gốc.
   */
  napPhienKhoiDong(): Observable<void> {
    const ctx = new HttpContext().set(BO_QUA_HET_PHIEN, true);
    return this.http.get<ApiResult<PhienDto>>('/core/auth/me', { context: ctx }).pipe(
      map(unwrapData),
      tap((d) => this._nguoiDung.set(sangNguoiDungHienTai(d))),
      map(() => undefined),
      catchError((err: unknown) => {
        if (
          err instanceof ApiFailureError &&
          err.body?.error.code === 'CORE.AUTH.NOT_AUTHENTICATED'
        ) {
          this._nguoiDung.set(null);
          return of(undefined);
        }
        return throwError(() => err);
      }),
    );
  }

  /**
   * `POST /core/auth/login` (contracts/auth.md §3). Phiên thiết lập từ DTO trong CHÍNH response —
   * KHÔNG gọi lại `me` (fe-routing-guard.md §3.6, Design/Screens/01-dang-nhap.md). Lấy lại token
   * XSRF ngay sau đó — token cũ gắn với danh tính lúc phát (07-auth-identity.md §3.2); đọc DTO và
   * lấy token không phụ thuộc thứ tự, nhưng cả hai phải xong trước khi `subscribe` báo hoàn tất.
   * Màn đăng nhập tự hiện lỗi ở khu lỗi của mình → tắt toast mặc định.
   */
  dangNhap(payload: DangNhapPayload): Observable<void> {
    const ctx = new HttpContext().set(BO_QUA_TOAST_LOI, true);
    return this.http.post<ApiResult<PhienDto>>('/core/auth/login', payload, { context: ctx }).pipe(
      map(unwrapData),
      tap((d) => this._nguoiDung.set(sangNguoiDungHienTai(d))),
      switchMap(() => this.xsrf.lamMoi()),
      map(() => undefined),
    );
  }

  /**
   * `POST /core/auth/logout` (contracts/auth.md §4). 401 ở đây KHÔNG phải hết phiên — phiên có
   * thể đã hết trước khi bấm đăng xuất — nên mang `BO_QUA_HET_PHIEN` VÀ được coi là "đã đăng
   * xuất" giống 200 (D6 — Design/Screens/00-khung-ung-dung.md). Lỗi khác thì để nguyên trạng thái,
   * interceptor tự toast, màn ở lại và thử lại được.
   */
  dangXuat(): Observable<void> {
    const ctx = new HttpContext().set(BO_QUA_HET_PHIEN, true);
    return this.http.post<ApiResult<null>>('/core/auth/logout', {}, { context: ctx }).pipe(
      map(() => undefined),
      catchError((err: unknown) => {
        if (
          err instanceof ApiFailureError &&
          err.body?.error.code === 'CORE.AUTH.NOT_AUTHENTICATED'
        ) {
          return of(undefined);
        }
        return throwError(() => err);
      }),
      tap(() => {
        this.donPhien();
        // Đăng xuất CHỦ ĐỘNG cũng phải báo tab khác — 07-auth-identity.md §7.3 bẫy (3). Đường
        // 401 báo qua SessionExpiryHandler; đường này không đi qua đó (tránh vòng DI) nên tự gọi.
        this.broadcast.baoPhienKetThuc();
      }),
      // Token mới phải về TRƯỚC khi nơi gọi điều hướng (fe-api-client.md §2.1) — cùng khuôn
      // `dangNhap()`. Lấy token hỏng không chặn đăng xuất: phiên đã dọn, token sẽ lấy lại ở lần
      // gặp CSRF_REJECTED.
      switchMap(() => this.xsrf.lamMoi().pipe(catchError(() => of(null)))),
      map(() => undefined),
    );
  }

  /** `errorInterceptor` gọi khi gặp 403 `CORE.AUTH.FORBIDDEN` (fe-api-client.md §2.2). */
  lamMoiQuyen(): void {
    this.canNapLaiMenu = true;
    if (this.goiMe === null) {
      void this.goiLaiMe();
    }
  }

  /**
   * Nơi gọi: màn hồ sơ sau khi lưu, và sau khi từ bỏ cờ đặc quyền (kèm `napLaiMenu`); `errorInterceptor`
   * khi 403 `PASSWORD_CHANGE_REQUIRED` (fe-routing-guard.md §5.4); màn đổi mật khẩu bắt buộc (§5.3);
   * khung ứng dụng sau đổi ngôn ngữ. Huỷ lời gọi đang chạy rồi gửi lại.
   *
   * `napLaiMenu: true` — hành động của CHÍNH người dùng vừa đổi tập quyền (màn hồ sơ: từ bỏ cờ đặc
   * quyền thành công, hoặc máy chủ báo `PERMISSION_BYPASS_NOT_HELD`): `me` về thì nạp lại menu bỏ qua
   * cache, cùng cờ `canNapLaiMenu` với `lamMoiQuyen()`. Khác `lamMoiQuyen()` ở chỗ huỷ lời gọi đang
   * chạy — lời gọi đó có thể đã đi trước hành động và mang tập quyền cũ.
   */
  lamMoiPhien(tuyChon: { readonly napLaiMenu?: boolean } = {}): Promise<void> {
    if (tuyChon.napLaiMenu) {
      this.canNapLaiMenu = true;
    }
    return this.goiLaiMe();
  }

  /** `SessionExpiryHandler` gọi (fe-api-client.md §2.5). Dọn gì, giữ gì: 07-auth-identity.md §7.2. */
  donPhien(): void {
    this.goiMe?.unsubscribe();
    this.goiMe = null;
    this.canNapLaiMenu = false;
    this._nguoiDung.set(null);
    this.menu.donPhien();
  }

  /** Thay TOÀN BỘ người dùng hiện tại bằng `me`. Promise xong khi lời gọi kết thúc — về, lỗi, hay bị huỷ. */
  private goiLaiMe(): Promise<void> {
    if (!this.daDangNhap()) {
      return Promise.resolve();
    }
    this.goiMe?.unsubscribe();
    return new Promise((xong) => {
      this.goiMe = this.http
        .get<ApiResult<PhienDto>>('/core/auth/me')
        .pipe(
          map(unwrapData),
          finalize(() => {
            this.goiMe = null;
            xong();
          }),
        )
        .subscribe({
          next: (d) => {
            const nguoiDung = sangNguoiDungHienTai(d);
            this._nguoiDung.set(nguoiDung);
            if (this.canNapLaiMenu) {
              this.canNapLaiMenu = false;
              // Bỏ qua cache: quyền vừa đổi, `userId` không đổi nên `lamMoi()` sẽ trúng cache cũ
              // (menu.store.ts §lamMoi, wiki-core/fe/07-auth-identity.md §5.4).
              this.menu.thuLai(d.id);
            }
            if (nguoiDung.mustChangePassword) {
              // Chạy lại guard của URL hiện tại — mustChangePasswordGuard trả đích (§5.4).
              void this.router.navigateByUrl(this.router.url, { onSameUrlNavigation: 'reload' });
            }
          },
          error: () => undefined,
        });
    });
  }
}
