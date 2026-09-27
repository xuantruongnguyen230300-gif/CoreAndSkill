import { CanActivateFn, Router } from '@angular/router';
import { inject } from '@angular/core';

import { AuthService } from '../auth/auth.service';
import { CORE_ROUTES } from '../config/core-routes';

/**
 * Định nghĩa gốc: quy-uoc/fe-routing-guard.md §3.3. Nhận mã permission từ chính route (đối số
 * factory) — Core không biết tên permission nào tồn tại, chuỗi do nơi khai route truyền vào.
 * Mọi quyền trong danh sách đều phải có (VÀ, không phải HOẶC); cần ngữ nghĩa HOẶC thì khai một
 * permission mới ở BE, đừng nới guard này.
 */
export function permissionGuard(...quyenCanCo: string[]): CanActivateFn {
  return () => {
    const auth = inject(AuthService);
    const router = inject(Router);
    const routes = inject(CORE_ROUTES);

    return quyenCanCo.every((q) => auth.coQuyen(q)) ? true : router.createUrlTree([routes.khongCoQuyen]);
  };
}
