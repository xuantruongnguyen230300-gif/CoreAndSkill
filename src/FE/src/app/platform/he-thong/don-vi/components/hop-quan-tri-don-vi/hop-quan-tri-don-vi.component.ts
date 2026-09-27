import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  input,
  output,
  viewChild,
} from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';

import { focusOSaiKhiGuiSai } from '../../../../../shared/forms/focus-o-sai-dau-tien';
import { ConfirmDialogComponent } from '../../../../../shared/components/confirm-dialog/confirm-dialog.component';
import { FormRowComponent } from '../../../../../shared/components/form-row/form-row.component';
import { NoticeBannerComponent } from '../../../../../shared/components/notice-banner/notice-banner.component';
import { ButtonComponent } from '../../../../../shared/components/button/button.component';
import { DialogComponent } from '../../../../../shared/ui/dialog/dialog.component';
import { InputComponent } from '../../../../../shared/components/input/input.component';
import {
  KhoiPhucForm,
  LoaiHopQuanTri,
  LoiTruongQuanTri,
  TaoQuanTriForm,
} from '../../models/don-vi-hop.model';
import { DonVi } from '../../models/don-vi.model';

/**
 * Hai hộp thoại "Khôi phục quản trị" (tenants.md §4) và "Tạo quản trị mới" (§6) —
 * Design/Screens/20-don-vi.md. Gộp một component vì chỉ một hộp mở tại một thời điểm (`dangMo()`)
 * và cùng chia sẻ đơn vị đang thao tác. Tách khỏi page để giữ *.html dưới ngưỡng dòng
 * (fe-architecture.md §5). Component dumb ĐÚNG NGHĨA của bảng trách nhiệm §3.1 và ADR-0049 điều 4:
 * không `inject` gì, không biết store, không biết `HttpClient`, không import DTO — chỉ nhận GIÁ TRỊ
 * qua `input()` và phát `output()` cho mọi hành động. Trang cha nối store ↔ component, và quyết
 * định Toast.
 */
@Component({
  selector: 'app-hop-quan-tri-don-vi',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    TranslatePipe,
    ConfirmDialogComponent,
    FormRowComponent,
    NoticeBannerComponent,
    ButtonComponent,
    DialogComponent,
    InputComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './hop-quan-tri-don-vi.component.html',
})
export class HopQuanTriDonViComponent {
  /** Hộp nào đang mở — `null` là không hộp nào. */
  readonly dangMo = input.required<LoaiHopQuanTri>();
  /** Đơn vị đang thao tác — chỉ để đọc tên (tiêu đề) và mã (gợi ý gõ lại mã). */
  readonly donVi = input.required<DonVi | null>();
  readonly formKhoiPhuc = input.required<KhoiPhucForm>();
  readonly formTaoQuanTri = input.required<TaoQuanTriForm>();
  /** Câu lỗi từng ô của cả hai hộp, đã tính sẵn — `null` = ô không có lỗi để hiện. */
  readonly loiTruong = input.required<LoiTruongQuanTri>();
  readonly loiChung = input.required<string | null>();
  readonly dangGui = input.required<boolean>();
  readonly hienThiXacNhanHuy = input.required<boolean>();
  /** Tăng mỗi lần bấm gửi mà form không hợp lệ — hộp đưa focus về ô sai đầu tiên. */
  readonly lanGuiSai = input.required<number>();

  readonly submittedKhoiPhuc = output<void>();
  readonly submittedTaoQuanTri = output<void>();
  readonly closed = output<void>();
  readonly dismissAttempted = output<void>();
  readonly discardConfirmed = output<void>();
  readonly discardCancelled = output<void>();

  private readonly formKhoiPhucEl = viewChild<ElementRef<HTMLFormElement>>('formKhoiPhucEl');
  private readonly formTaoQuanTriEl = viewChild<ElementRef<HTMLFormElement>>('formTaoQuanTriEl');

  constructor() {
    // Bấm gửi khi form không hợp lệ → focus về ô sai đầu tiên của HỘP ĐANG MỞ (09-forms-validation.md §5).
    focusOSaiKhiGuiSai(
      this.lanGuiSai,
      () =>
        (this.dangMo() === 'khoi-phuc' ? this.formKhoiPhucEl() : this.formTaoQuanTriEl())
          ?.nativeElement,
    );
  }
}
