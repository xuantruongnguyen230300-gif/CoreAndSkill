import { Routes } from '@angular/router';

import { authGuard } from '../../core/guards/auth.guard';
import { mustChangePasswordGuard } from '../../core/guards/must-change-password.guard';

/**
 * quy-uoc/fe-routing-guard.md §2.3. Hai màn KHÔNG có khung app; nằm ở nhánh route ĐẦU TIÊN của
 * `app.routes.ts`, không bọc trong component khung. Guard khác nhau: `/dang-nhap` KHÔNG có
 * `authGuard` — nơi duy nhất người CHƯA đăng nhập được vào; `/doi-mat-khau-bat-buoc` cần cả hai.
 */
export const XAC_THUC_ROUTES: Routes = [
  {
    path: 'dang-nhap',
    title: 'xacThuc.dangNhap.tieuDe',
    loadComponent: () => import('./pages/dang-nhap/dang-nhap.page').then((m) => m.DangNhapPage),
  },
  {
    path: 'doi-mat-khau-bat-buoc',
    canActivate: [authGuard, mustChangePasswordGuard],
    title: 'xacThuc.doiMatKhauBatBuoc.tieuDe',
    loadComponent: () =>
      import('./pages/doi-mat-khau-bat-buoc/doi-mat-khau-bat-buoc.page').then(
        (m) => m.DoiMatKhauBatBuocPage,
      ),
  },
];
