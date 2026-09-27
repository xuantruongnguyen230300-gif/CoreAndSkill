import { Injectable } from '@angular/core';

/** Khoá `localStorage` dùng chung để báo các tab khác biết phiên vừa kết thúc. */
export const KHOA_BAO_TAB_PHIEN_KET_THUC = 'core.session-ended';

/**
 * Tách khỏi `SessionExpiryHandler` để `AuthService.dangXuat()` (đăng xuất CHỦ ĐỘNG) gọi được mà
 * không tạo vòng DI — `SessionExpiryHandler` đã `inject(AuthService)`, nên `AuthService` không
 * được inject ngược `SessionExpiryHandler` (NG0200).
 *
 * Dùng bởi CẢ hai đường phiên kết thúc: đăng xuất chủ động (`AuthService.dangXuat()`) VÀ hết
 * phiên do 401 (`SessionExpiryHandler`) — 07-auth-identity.md §7.3 bẫy (3): "đăng xuất ở một tab
 * không tự đóng các tab khác... cách rẻ nhất: khi xử lý hết phiên, ghi một dấu hiệu vào
 * `localStorage`". Giá trị là thời điểm — ghi cùng giá trị thì trình duyệt không phát sự kiện.
 */
@Injectable({ providedIn: 'root' })
export class TabSessionBroadcastService {
  baoPhienKetThuc(): void {
    try {
      localStorage.setItem(KHOA_BAO_TAB_PHIEN_KET_THUC, String(Date.now()));
    } catch {
      // localStorage không dùng được (chế độ riêng tư, dung lượng đầy) — tab hiện tại vẫn đã
      // dọn phiên đúng; chỉ mất khả năng báo tab khác, không chặn người dùng.
    }
  }
}
