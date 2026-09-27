import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

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
  imports: [RouterOutlet, ToastComponent, UnsavedChangesDialogComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {}
