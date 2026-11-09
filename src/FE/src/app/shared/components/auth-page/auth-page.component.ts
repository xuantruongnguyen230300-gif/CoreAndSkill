import { ChangeDetectionStrategy, Component } from '@angular/core';

/**
 * Cột của khung xác thực — Design/Components/AuthCard.md §Responsive. Một cột cao TỐI THIỂU
 * `100dvh` bọc `main` (trong `AuthCard`) và `Footer`: đủ chỗ thì `Footer` sát đáy khung nhìn,
 * không thanh cuộn; thiếu chỗ thì cột cao theo nội dung và trang cuộn. Dùng chung mọi màn xác thực;
 * không nằm trong `AuthCard` (Footer không lồng vào khung), không `position: fixed`, không `100vh`.
 */
@Component({
  selector: 'app-auth-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<ng-content></ng-content>',
  styleUrl: './auth-page.component.scss',
})
export class AuthPageComponent {}
