import { CanDeactivateFn } from '@angular/router';
import { inject } from '@angular/core';

import { AuthService } from '../auth/auth.service';
import { UnsavedChangesService } from '../unsaved-changes/unsaved-changes.service';

/**
 * Định nghĩa gốc: quy-uoc/fe-routing-guard.md §4.1. Chạy lúc RỜI route (canDeactivate), không
 * lúc vào — cuối thứ tự guard, sau `permissionGuard`/`systemOperatorGuard` (§4). Không đọc
 * component đang rời: đọc registry chung `UnsavedChangesService` để CÙNG một cơ chế phục vụ cả
 * điều hướng trong app (ở đây) và hành động ngoài router của khung ứng dụng (đăng xuất, đổi
 * ngôn ngữ — `platform/shell` gọi thẳng `xinRoiTrang()`), tránh hỏi hai lần theo hai đường khác
 * nhau (ADR-0040).
 *
 * Phiên đã kết thúc thì cho rời ngay, không hỏi: 401 luôn về màn đăng nhập kể cả khi form dở
 * (§8). Cùng điều kiện phủ tab khác báo hết phiên, và đăng xuất chủ động đã xong —
 * `AuthService.dangXuat()` dọn phiên trước khi khung điều hướng, và khung đã hỏi một lần rồi.
 * `AuthService` không inject `UnsavedChangesService` (và không inject guard) nên không có vòng DI.
 */
export const unsavedChangesGuard: CanDeactivateFn<unknown> = () => {
  const thayDoi = inject(UnsavedChangesService);
  const auth = inject(AuthService);

  if (!auth.daDangNhap()) {
    thayDoi.roiTrangChacChan();
    return true;
  }
  if (!thayDoi.coThayDoiChuaLuu()) {
    return true;
  }
  return thayDoi.xinRoiTrang().then((roiDuoc) => {
    if (roiDuoc) {
      // Router sẽ rời route: gỡ đăng ký để lần Router chạy lại guard này (chuyển hướng) không hỏi lại.
      thayDoi.roiTrangChacChan();
    }
    return roiDuoc;
  });
};
