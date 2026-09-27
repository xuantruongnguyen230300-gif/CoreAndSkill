import { Routes } from '@angular/router';

import { permissionGuard } from '../../../core/guards/permission.guard';

/** Design/Screens/11-vai-tro.md. Quyền vào màn: core.role.read (roles.md §1). */
export const VAI_TRO_ROUTES: Routes = [
  {
    path: '',
    title: 'vaiTro.tieuDe',
    canActivate: [permissionGuard('core.role.read')],
    loadComponent: () =>
      import('./pages/danh-sach/danh-sach-vai-tro.page').then((m) => m.DanhSachVaiTroPage),
  },
];
