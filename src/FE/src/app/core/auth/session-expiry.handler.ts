import { DestroyRef, Injectable, inject } from '@angular/core';
import { Router } from '@angular/router';

import { CORE_ROUTES } from '../config/core-routes';
import { AuthService } from './auth.service';
import { KHOA_BAO_TAB_PHIEN_KET_THUC, TabSessionBroadcastService } from './tab-session-broadcast.service';

/**
 * Ba đường dẫn tới hết phiên quy về MỘT lớp: `errorInterceptor` gọi ở nhánh 401 (fe-api-client.md §2.2),
 * tab khác báo qua sự kiện `storage` của trình duyệt. Đăng xuất CHỦ ĐỘNG (`AuthService.dangXuat()`)
 * không đi qua lớp này — nó tự gọi `TabSessionBroadcastService` (tránh vòng DI, xem file đó) —
 * nhưng dùng CHUNG khoá `localStorage`, nên tab khác nghe ở đây vẫn bắt được cả hai đường.
 */
@Injectable({ providedIn: 'root' })
export class SessionExpiryHandler {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly routes = inject(CORE_ROUTES);
  private readonly broadcast = inject(TabSessionBroadcastService);

  constructor() {
    // Tab khác đã hết phiên (401 hoặc đăng xuất chủ động) → dọn theo, KHÔNG báo lại.
    const nghe = (e: StorageEvent): void => {
      if (e.key === KHOA_BAO_TAB_PHIEN_KET_THUC) {
        this.ketThuc(false);
      }
    };
    window.addEventListener('storage', nghe);
    inject(DestroyRef).onDestroy(() => window.removeEventListener('storage', nghe));
  }

  /** `errorInterceptor` gọi khi gặp 401 ở request không mang `BO_QUA_HET_PHIEN`. */
  handle(): void {
    this.ketThuc(true);
  }

  private ketThuc(baoTabKhac: boolean): void {
    // Chốt là CHÍNH trạng thái phiên: đã dọn thì mọi 401 đến sau bỏ qua, tới lần đăng nhập kế.
    if (!this.auth.daDangNhap()) {
      return;
    }
    this.auth.donPhien();
    if (baoTabKhac) {
      this.broadcast.baoPhienKetThuc();
    }
    void this.router.navigate([this.routes.dangNhap], {
      queryParams: { returnUrl: this.router.url },
    });
  }
}
