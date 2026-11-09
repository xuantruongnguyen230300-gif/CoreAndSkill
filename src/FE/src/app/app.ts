import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';

import { LoiTaiPhanMaService } from './core/navigation/loi-tai-phan-ma';
import { ButtonComponent } from './shared/components/button/button.component';
import { EmptyStateComponent } from './shared/components/empty-state/empty-state.component';
import { ToastComponent } from './shared/ui/toast/toast.component';
import { UnsavedChangesDialogComponent } from './platform/shell/unsaved-changes-dialog/unsaved-changes-dialog.component';

/**
 * Gốc bootstrap của app — KHÔNG phải `platform/shell` (khung thật có sidebar/topbar, thuộc F2).
 * `<app-unsaved-changes-dialog>` (F2, ADR-0040) đứng cạnh `<app-toast>`: cùng khuôn hộp thoại
 * toàn app dựng MỘT lần ở gốc, đọc registry của `core/` — quy-uoc/fe-routing-guard.md §4.1.
 */
@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    RouterOutlet,
    TranslatePipe,
    ButtonComponent,
    EmptyStateComponent,
    ToastComponent,
    UnsavedChangesDialogComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  /**
   * Khối "Không tải được phần ứng dụng" (Design/Screens/02-trang-loi.md) thay `<router-outlet />` khi
   * chưa màn nào hiện mà phần mã của tuyến hỏng. Khối và mọi thứ nó dùng nằm ở gói khởi động — đặt
   * nó ở gói theo tuyến thì nó hỏng cùng lúc với thứ nó báo.
   */
  protected readonly loiTaiPhanMa = inject(LoiTaiPhanMaService);
}
