import { CanActivateFn, Router } from '@angular/router';
import { inject } from '@angular/core';

import { AuthService } from '../auth/auth.service';
import { CORE_ROUTES } from '../config/core-routes';

/**
 * Định nghĩa gốc: quy-uoc/fe-routing-guard.md §3.5. Khu `/he-thong` gác bằng CỜ `isSystemOperator`
 * của phiên, KHÔNG bằng ma trận quyền — THAY CHO `permissionGuard`, không cộng thêm (§4 dòng 3').
 * Cờ không nằm trong `permissions`, nên đây là con đường duy nhất kiểm nó.
 */
export const systemOperatorGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const routes = inject(CORE_ROUTES);

  return auth.laVanHanhHeThong() ? true : router.createUrlTree([routes.khongCoQuyen]);
};
