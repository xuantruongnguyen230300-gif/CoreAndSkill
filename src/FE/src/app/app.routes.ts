import { Routes } from '@angular/router';

import { authGuard } from './core/guards/auth.guard';
import { mustChangePasswordGuard } from './core/guards/must-change-password.guard';

/**
 * quy-uoc/fe-routing-guard.md §2.1. Nhánh xác thực khai TRƯỚC nhánh khung app; `xac-thuc.routes.ts`
 * KHÔNG khai path `''` hay `'**'`. `app.routes.ts` CHỈ khai `loadChildren`/`loadComponent` — luật F20.
 */
export const routes: Routes = [
  {
    path: '',
    loadChildren: () => import('./platform/xac-thuc/xac-thuc.routes').then((m) => m.XAC_THUC_ROUTES),
  },
  {
    path: '',
    canActivate: [authGuard, mustChangePasswordGuard],
    // Cho AuthService chạy lại guard của URL hiện tại khi cờ buộc đổi mật khẩu bật giữa phiên (§5.4).
    runGuardsAndResolvers: 'always',
    loadComponent: () => import('./platform/shell/shell.component').then((m) => m.ShellComponent),
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'trang-chu' },
      {
        path: 'trang-chu',
        loadChildren: () => import('./platform/trang-chu/trang-chu.routes').then((m) => m.TRANG_CHU_ROUTES),
      },
      {
        path: 'ho-so',
        loadChildren: () => import('./platform/ho-so/ho-so.routes').then((m) => m.HO_SO_ROUTES),
      },
      {
        path: 'quan-tri',
        loadChildren: () => import('./platform/quan-tri/quan-tri.routes').then((m) => m.QUAN_TRI_ROUTES),
      },
      {
        // Khu quản trị đơn vị — gác bằng systemOperatorGuard TRONG he-thong/don-vi.routes.ts, không
        // ở đây (fe-routing-guard.md §2.2, §3.5). Sau F3, đóng khi B3 xong (00-lo-trinh-tong-the.md §1).
        path: 'he-thong',
        loadChildren: () => import('./platform/he-thong/he-thong.routes').then((m) => m.HE_THONG_ROUTES),
      },
      // Route nghiệp vụ — mỗi module một dòng, không import component trực tiếp. F2 chưa có
      // module nghiệp vụ nào (modules/ ra đời cùng module đầu tiên — fe-architecture.md §2.4).

      // Trang lỗi nằm TRONG khung (§1). '**' là dòng cuối.
      {
        path: 'khong-co-quyen',
        title: 'trangLoi.khongCoQuyen.tieuDe',
        loadComponent: () => import('./platform/loi/khong-co-quyen.page').then((m) => m.KhongCoQuyenPage),
      },
      {
        path: '**',
        title: 'trangLoi.khongTimThay.tieuDe',
        loadComponent: () => import('./platform/loi/khong-tim-thay.page').then((m) => m.KhongTimThayPage),
      },
    ],
  },
];
