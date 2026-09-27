import { Routes } from '@angular/router';

import { permissionGuard } from '../../../core/guards/permission.guard';
import { unsavedChangesGuard } from '../../../core/guards/unsaved-changes.guard';

/** Design/Screens/12-ma-tran-phan-quyen.md. Quyền vào màn: core.permission.read (permissions.md §1). */
export const PHAN_QUYEN_ROUTES: Routes = [
  {
    path: '',
    title: 'phanQuyen.tieuDe',
    canActivate: [permissionGuard('core.permission.read')],
    canDeactivate: [unsavedChangesGuard],
    loadComponent: () =>
      import('./pages/ma-tran/ma-tran-phan-quyen.page').then((m) => m.MaTranPhanQuyenPage),
  },
];
