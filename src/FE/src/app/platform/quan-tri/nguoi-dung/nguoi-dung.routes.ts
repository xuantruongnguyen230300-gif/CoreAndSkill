import { Routes } from '@angular/router';

import { permissionGuard } from '../../../core/guards/permission.guard';

/** Design/Screens/10-nguoi-dung.md. Quyền vào CẢ HAI màn: core.user.read (users.md §1, §3, §4). */
export const NGUOI_DUNG_ROUTES: Routes = [
  {
    path: '',
    title: 'nguoiDung.tieuDe',
    canActivate: [permissionGuard('core.user.read')],
    loadComponent: () =>
      import('./pages/danh-sach/danh-sach-nguoi-dung.page').then((m) => m.DanhSachNguoiDungPage),
  },
  {
    path: ':id',
    title: 'nguoiDung.chiTiet',
    canActivate: [permissionGuard('core.user.read')],
    loadComponent: () =>
      import('./pages/chi-tiet/chi-tiet-nguoi-dung.page').then((m) => m.ChiTietNguoiDungPage),
  },
];
