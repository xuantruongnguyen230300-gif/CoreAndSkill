import { CanActivateFn, Router } from '@angular/router';
import { inject } from '@angular/core';

import { AuthService } from '../auth/auth.service';
import { CORE_ROUTES } from '../config/core-routes';

/**
 * Định nghĩa gốc: quy-uoc/fe-routing-guard.md §5.2. Gác HAI CHIỀU — có mặt ở cả nhánh khung app
 * (đẩy người bị ép vào màn đổi mật khẩu) và nhánh xác thực (đẩy người KHÔNG bị ép ra khỏi màn
 * đó). Thứ tự: SAU `authGuard`, TRƯỚC `permissionGuard` — đảo là người buộc đổi mật khẩu nhận
 * "không đủ quyền" thay vì được đưa tới màn đổi mật khẩu (§4.3).
 */
export const mustChangePasswordGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const routes = inject(CORE_ROUTES);

  const dangVaoManDoiMatKhau = state.url.startsWith(routes.doiMatKhauBatBuoc);

  if (!auth.phaiDoiMatKhau()) {
    // Không bị ép đổi mà vẫn gõ thẳng URL màn đó → đẩy về đích sau đăng nhập.
    return dangVaoManDoiMatKhau ? router.createUrlTree([routes.sauDangNhap]) : true;
  }

  // Đang ở chính màn đổi mật khẩu thì cho qua — thiếu nhánh này là vòng lặp điều hướng vô hạn.
  return dangVaoManDoiMatKhau ? true : router.createUrlTree([routes.doiMatKhauBatBuoc]);
};
