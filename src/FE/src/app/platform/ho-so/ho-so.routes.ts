import { Routes } from '@angular/router';

import { unsavedChangesGuard } from '../../core/guards/unsaved-changes.guard';

/**
 * Design/Screens/03-ho-so-ca-nhan.md — không permissionGuard, guard chung của nhánh khung app
 * lo đủ. `canDeactivate: [unsavedChangesGuard]` — hai form của trang (thông tin cá nhân, đổi
 * mật khẩu) đăng ký "còn thay đổi chưa lưu" với `UnsavedChangesService` (ADR-0040).
 */
export const HO_SO_ROUTES: Routes = [
  {
    path: '',
    title: 'hoSo.tieuDe',
    canDeactivate: [unsavedChangesGuard],
    loadComponent: () => import('./pages/ho-so/ho-so.page').then((m) => m.HoSoPage),
  },
];
