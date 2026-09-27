import { Routes } from '@angular/router';

/**
 * fe-routing-guard.md §2.2. Guard riêng của màn khai trong `don-vi.routes.ts`, không rải ở đây —
 * file này chỉ ghép nhánh lazy của khu quản trị hệ thống. Hôm nay chỉ một nhánh; cấu trúc để ngỏ
 * cho các khu vận hành hệ thống khác nếu phát sinh (tất cả gác cùng systemOperatorGuard).
 */
export const HE_THONG_ROUTES: Routes = [
  {
    path: 'don-vi',
    loadChildren: () => import('./don-vi/don-vi.routes').then((m) => m.DON_VI_ROUTES),
  },
];
