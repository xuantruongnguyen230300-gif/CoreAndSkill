import { DOCUMENT, Location } from '@angular/common';
import { DestroyRef, Injectable, inject, signal } from '@angular/core';
import { NavigationEnd, NavigationError, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';

import { ToastService } from '../toast/toast.service';

/**
 * Mẫu câu báo lỗi khi `import()` động không tải được phần mã — MỖI TRÌNH DUYỆT MỘT CÂU, không chuẩn
 * nào ghim chúng (quy-uoc/fe-routing-guard.md §8.1, "Cái giá"). Trình duyệt đổi câu thì hàm trả
 * `false` và người dùng quay về hành vi cũ mà không gì báo; mỗi mẫu có một ca kiểm riêng ở spec.
 */
const MAU_LOI_TAI_PHAN_MA: readonly RegExp[] = [
  // Chromium
  /Failed to fetch dynamically imported module/i,
  // Firefox
  /error loading dynamically imported module/i,
  // Safari
  /Importing a module script failed/i,
];

/**
 * Lỗi của một lần điều hướng có phải do không tải được phần mã của tuyến (`loadComponent`,
 * `loadChildren`) không. Hàm thuần; `false` với mọi thứ khác — lỗi do guard hay resolver ném không
 * phải ca này.
 */
export function laLoiTaiPhanMa(loi: unknown): boolean {
  if (typeof loi !== 'object' || loi === null) {
    return false;
  }
  const message = (loi as { message?: unknown }).message;
  return typeof message === 'string' && MAU_LOI_TAI_PHAN_MA.some((mau) => mau.test(message));
}

/**
 * Một chỗ bắt duy nhất cho lần điều hướng hỏng vì không tải được phần mã — định nghĩa gốc:
 * quy-uoc/fe-routing-guard.md §8.1. Chỉ NGHE `Router.events`, không nuốt: `NavigationError` vẫn tới
 * mọi nơi đang nghe (lớp bảo vệ dữ liệu dở vẫn thấy lần điều hướng hỏng).
 *
 * Chọn nhánh bằng một câu hỏi: lần tải trang này đã có `NavigationEnd` nào chưa? Chưa → khối toàn
 * trang (`khoiToanTrang`, `app.html` vẽ); có → toast. Không đọc `Router.navigated`.
 */
@Injectable({ providedIn: 'root' })
export class LoiTaiPhanMaService {
  private readonly router = inject(Router);
  private readonly location = inject(Location);
  private readonly toast = inject(ToastService);
  private readonly translate = inject(TranslateService);
  private readonly doc = inject(DOCUMENT);

  private daCoManHien = false;
  private readonly _khoiToanTrang = signal(false);
  /** `true` khi cả trang phải nhường chỗ cho khối "Không tải được phần ứng dụng". */
  readonly khoiToanTrang = this._khoiToanTrang.asReadonly();

  constructor() {
    const dangNghe = this.router.events.subscribe((e) => {
      if (e instanceof NavigationEnd) {
        this.daCoManHien = true;
      } else if (e instanceof NavigationError && laLoiTaiPhanMa(e.error)) {
        this.xuLy(e.url);
      }
    });
    inject(DestroyRef).onDestroy(() => dangNghe.unsubscribe());
  }

  /** Tải lại TOÀN trang tại địa chỉ đang ở trên thanh địa chỉ; không điều hướng trong ứng dụng. */
  taiLaiTrang(): void {
    this.doc.defaultView?.location.reload();
  }

  private xuLy(urlDich: string): void {
    if (!this.daCoManHien) {
      // Router đã đặt lại thanh địa chỉ về URL trước lần điều hướng (ở lần tải đầu là `/`) TRƯỚC khi
      // phát sự kiện này (wiki-core/fe/ly-do/fe-routing-guard.md §8.1) — trả về đích để F5 và nút
      // "Tải lại trang" mở lại đúng trang người dùng muốn.
      this.location.replaceState(urlDich);
      this._khoiToanTrang.set(true);
      return;
    }
    this.toast.loi(this.translate.instant('loi.CORE.CLIENT.RESOURCE_LOAD_FAILED'), null, {
      nhan: this.translate.instant('trangLoi.khongTaiDuoc.taiLai'),
      chay: () => this.taiLaiTrang(),
    });
  }
}
