import { Routes } from '@angular/router';

/**
 * fe-routing-guard.md §2.2. Guard riêng của từng màn khai trong `<feature>.routes.ts` của chính
 * nó, không rải ở đây — file này chỉ ghép ba nhánh lazy.
 */
export const QUAN_TRI_ROUTES: Routes = [
  {
    path: 'nguoi-dung',
    loadChildren: () => import('./nguoi-dung/nguoi-dung.routes').then((m) => m.NGUOI_DUNG_ROUTES),
  },
  {
    path: 'vai-tro',
    loadChildren: () => import('./vai-tro/vai-tro.routes').then((m) => m.VAI_TRO_ROUTES),
  },
  {
    path: 'phan-quyen',
    loadChildren: () => import('./phan-quyen/phan-quyen.routes').then((m) => m.PHAN_QUYEN_ROUTES),
  },
];
