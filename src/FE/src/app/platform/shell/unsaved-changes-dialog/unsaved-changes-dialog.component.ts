import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import { UnsavedChangesService } from '../../../core/unsaved-changes/unsaved-changes.service';
import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog.component';

/**
 * Tầng SMART — inject `UnsavedChangesService` nên KHÔNG được nằm ở `shared/components/` (luật F11)
 * và không ở `core/` (core cấm component có template — fe-architecture.md §2.1). Nằm cạnh
 * `platform/shell` — phần smart của khung ứng dụng (fe-architecture.md §2.3).
 *
 * Định nghĩa gốc: quy-uoc/fe-routing-guard.md §4.1. Hộp DUY NHẤT toàn app cho "Rời trang?" —
 * dựng MỘT lần ở gốc (`app.html`, cùng khuôn `<app-toast>`), đọc `UnsavedChangesService` (core/,
 * không biết thư viện UI) rồi vẽ bằng `ConfirmDialog`. `unsavedChangesGuard` và `platform/shell`
 * (đăng xuất, đổi ngôn ngữ) đều đi qua CÙNG service này nên chỉ một hộp, không hỏi hai lần.
 * `cancelLabel` để mặc định (`null`) → dùng nhãn huỷ chung `chung.huy` (§4.1 bảng khoá i18n).
 */
@Component({
  selector: 'app-unsaved-changes-dialog',
  standalone: true,
  imports: [TranslatePipe, ConfirmDialogComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './unsaved-changes-dialog.component.html',
})
export class UnsavedChangesDialogComponent {
  protected readonly prompt = inject(UnsavedChangesService);
}
