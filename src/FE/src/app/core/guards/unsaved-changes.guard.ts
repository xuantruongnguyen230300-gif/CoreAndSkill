import { inject } from '@angular/core';
import {
  CanDeactivateFn,
  NavigationCancel,
  NavigationCancellationCode,
  NavigationEnd,
  NavigationError,
  NavigationSkipped,
  Router,
} from '@angular/router';
import { filter } from 'rxjs';

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
  const router = inject(Router);

  if (!auth.daDangNhap()) {
    thayDoi.roiTrangChacChan();
    return true;
  }
  // Lần chạy lại của cùng một lần rời (canActivate phía sau chuyển hướng): đã hỏi rồi, không hỏi lần hai.
  if (thayDoi.daDongYRoiLanNay() || !thayDoi.coThayDoiChuaLuu()) {
    return true;
  }
  return thayDoi.xinRoiTrang().then((roiDuoc) => {
    if (roiDuoc) {
      // "Rời đi" mới là lời đồng ý, chưa phải việc rời: đăng ký chỉ gỡ khi điều hướng xong. Hỏng/huỷ
      // thì giữ đăng ký (cả hai đường bảo vệ còn nguyên). Huỷ vì chuyển hướng thì Router chạy lại
      // canDeactivate cho một điều hướng mới — giữ lời đồng ý và chờ kết cục của điều hướng đó.
      thayDoi.danhDauDongYRoi();
      // Router dựng màn đích (constructor của nó có thể `dangKy`) TRƯỚC khi phát `NavigationEnd`:
      // chỉ gỡ đúng hàm của màn đang rời, không xoá đăng ký của màn đích.
      const kiemTraManCu = thayDoi.kiemTraDangDangKy();
      const dangNghe = router.events
        .pipe(
          filter(
            (e) =>
              e instanceof NavigationEnd ||
              e instanceof NavigationError ||
              e instanceof NavigationSkipped ||
              (e instanceof NavigationCancel && e.code !== NavigationCancellationCode.Redirect),
          ),
        )
        .subscribe((e) => {
          dangNghe.unsubscribe();
          if (e instanceof NavigationEnd && kiemTraManCu) {
            thayDoi.huyDangKy(kiemTraManCu);
          }
          thayDoi.huyDongYRoi();
        });
    }
    return roiDuoc;
  });
};
