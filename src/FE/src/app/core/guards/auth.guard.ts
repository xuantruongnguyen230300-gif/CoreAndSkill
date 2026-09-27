import { CanActivateFn, Router } from '@angular/router';
import { inject } from '@angular/core';

import { AuthService } from '../auth/auth.service';
import { CORE_ROUTES } from '../config/core-routes';

/**
 * Định nghĩa gốc: quy-uoc/fe-routing-guard.md §3.2. Chưa đăng nhập → `UrlTree` về màn đăng nhập
 * kèm `returnUrl`. Trả `UrlTree` chứ không `router.navigate()` rồi `return false` — router thi
 * công điều hướng đó đúng một lần, không có khung hình trung gian render lộn.
 */
export const authGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const routes = inject(CORE_ROUTES);

  if (auth.daDangNhap()) {
    return true;
  }

  return router.createUrlTree([routes.dangNhap], { queryParams: { returnUrl: state.url } });
};
