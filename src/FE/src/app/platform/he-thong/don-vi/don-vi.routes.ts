import { Routes } from '@angular/router';

import { systemOperatorGuard } from '../../../core/guards/system-operator.guard';

/**
 * Design/Screens/20-don-vi.md. Guard: `systemOperatorGuard` — cờ `isSystemOperator` của phiên,
 * THAY CHO `permissionGuard`, không cộng thêm (fe-routing-guard.md §3.5, §4 dòng 3'). Một màn, ba
 * hộp thoại, một hộp xác nhận — không có route con "chi tiết" (người vận hành không xem được dữ
 * liệu bên trong đơn vị).
 */
export const DON_VI_ROUTES: Routes = [
  {
    path: '',
    title: 'donVi.tieuDe',
    canActivate: [systemOperatorGuard],
    loadComponent: () =>
      import('./pages/danh-sach/danh-sach-don-vi.page').then((m) => m.DanhSachDonViPage),
  },
];
