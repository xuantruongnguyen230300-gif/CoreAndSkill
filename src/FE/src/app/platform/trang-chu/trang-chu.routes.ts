import { inject } from '@angular/core';
import { Routes } from '@angular/router';

import { CORE_HOME, CoreHomeLoader } from '../../core/config/core-home';

/** Trang chào tối giản của Core (fe-architecture.md §2.3) — nạp lười như mọi trang. */
const TRANG_CHAO_CORE: CoreHomeLoader = () =>
  import('./pages/trang-chu/trang-chu.page').then((m) => m.TrangChuPage);

/**
 * fe-architecture.md §2.5, ADR-0057: `loadComponent` chạy trong ngữ cảnh tiêm của Router, nên
 * đọc seam `CORE_HOME` NGAY TẠI ĐÂY — dự án cấp hàm nạp lười qua `provideCoreHome`, không sửa route.
 * Không khai ⇒ trang chào của Core. `trang-chu.routes.spec.ts` chạy `loadComponent` thật qua Router.
 */
export const TRANG_CHU_ROUTES: Routes = [
  {
    path: '',
    title: 'trangChu.tieuDe',
    loadComponent: () => (inject(CORE_HOME, { optional: true }) ?? TRANG_CHAO_CORE)(),
  },
];
