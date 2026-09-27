import { Injectable, computed, inject, signal } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { EMPTY, Subscription, catchError } from 'rxjs';

import { CORE_I18N } from '../config/core-i18n';
import { MenuService } from './menu.service';
import { MenuNode } from './menu.model';

type TrangThaiMenu = 'idle' | 'loading' | 'error' | 'empty';

/**
 * 🛑 KHÔNG inject `AuthService` — vòng DI (NG0200), như `auth.service.ts` đã ghi chú
 * (fe-routing-guard.md §3.3). `AuthService` gọi `lamMoi(userId)` sau khi phiên đổi, không phải
 * ngược lại.
 *
 * Cache khoá theo CẢ người dùng lẫn ngôn ngữ (wiki-core/fe/07-auth-identity.md §6, luật 2 —
 * "cache menu có khoá gồm cả người dùng lẫn ngôn ngữ") — v1 chỉ một ngôn ngữ nên khoá luôn cùng
 * hậu tố, nhưng cơ chế đã đúng cho ngày có ngôn ngữ thứ hai.
 */
@Injectable({ providedIn: 'root' })
export class MenuStore {
  private readonly service = inject(MenuService);
  private readonly translate = inject(TranslateService);
  private readonly i18n = inject(CORE_I18N);

  private readonly cache = new Map<string, readonly MenuNode[]>();
  private dangTai: Subscription | null = null;

  private readonly _items = signal<readonly MenuNode[]>([]);
  private readonly _state = signal<TrangThaiMenu>('idle');

  readonly items = this._items.asReadonly();
  readonly state = this._state.asReadonly();
  readonly rong = computed(() => this._state() === 'idle' && this._items().length === 0);

  /** `AuthService` gọi sau khi phiên thiết lập/làm mới quyền (fe-api-client.md §2.2). */
  lamMoi(userId: string): void {
    const khoa = this.khoaCache(userId);
    const coSan = this.cache.get(khoa);
    if (coSan) {
      this._items.set(coSan);
      this._state.set(coSan.length === 0 ? 'empty' : 'idle');
      return;
    }

    this.dangTai?.unsubscribe();
    this._state.set('loading');
    this.dangTai = this.service
      .layMenu()
      .pipe(
        // Request mang BO_QUA_TOAST_LOI nên lỗi của màn không toast — riêng lớp lỗi xuyên suốt (5xx,
        // 403 `CORE.AUTH.*`) interceptor VẪN toast kèm traceId (ADR-0094). Không dữ liệu dự phòng:
        // Sidebar tự hiện `error` kèm Thử lại (Design/Screens/00-khung-ung-dung.md, Trạng thái → lỗi).
        // Lỗi không vào cache — lần sau vẫn đi server.
        catchError(() => {
          this._items.set([]);
          this._state.set('error');
          return EMPTY;
        }),
      )
      .subscribe({
        // Service đã map sang cây `MenuNode` (fe-api-client.md §4.1) — store không thấy DTO.
        next: (cay: readonly MenuNode[]) => {
          this.cache.set(khoa, cay);
          this._items.set(cay);
          this._state.set(cay.length === 0 ? 'empty' : 'idle');
        },
      });
  }

  /**
   * `AuthService.donPhien()` gọi khi đăng xuất/hết phiên — dọn menu VÀ cache theo người dùng
   * (07-auth-identity.md §7.2). Giữ cache qua đăng xuất thì người đăng nhập lại trên cùng tab trúng
   * cây menu của phiên trước — kể cả mục của quyền đã bị thu hồi.
   */
  donPhien(): void {
    this.dangTai?.unsubscribe();
    this.dangTai = null;
    this.cache.clear();
    this._items.set([]);
    this._state.set('idle');
  }

  thuLai(userId: string): void {
    this.cache.delete(this.khoaCache(userId));
    this.lamMoi(userId);
  }

  private khoaCache(userId: string): string {
    return `${userId}:${this.translate.currentLang() ?? this.translate.fallbackLang() ?? this.i18n.defaultLanguage}`;
  }
}
