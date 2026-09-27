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
import { LoiTruongTaoDonVi, TaoDonViForm } from '../../models/don-vi-hop.model';

/**
 * Hộp thoại "Tạo đơn vị" (danh-sach-don-vi.page, Design/Screens/20-don-vi.md) — tách khỏi page để
 * giữ *.html dưới ngưỡng dòng (fe-architecture.md §5). Component dumb ĐÚNG NGHĨA của bảng trách
 * nhiệm §3.1 và ADR-0049 điều 4: không `inject` gì, không biết store, không biết `HttpClient`, không
 * import DTO — chỉ nhận GIÁ TRỊ (form, cờ, chuỗi lỗi) qua `input()` và phát `output()` cho mọi hành
 * động. Trang cha nối store ↔ component, và quyết định Toast + `list.reload()`.
 */
@Component({
  selector: 'app-hop-tao-don-vi',
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
  templateUrl: './hop-tao-don-vi.component.html',
  styleUrl: './hop-tao-don-vi.component.scss',
})
export class HopTaoDonViComponent {
  readonly hienThi = input.required<boolean>();
  readonly form = input.required<TaoDonViForm>();
  /** Câu lỗi từng ô, đã tính sẵn — `null` = ô không có lỗi để hiện. */
  readonly loiTruong = input.required<LoiTruongTaoDonVi>();
  readonly loiChung = input.required<string | null>();
  readonly dangLuu = input.required<boolean>();
  readonly hienThiXacNhanHuy = input.required<boolean>();
  /** Tăng mỗi lần bấm gửi mà form không hợp lệ — hộp đưa focus về ô sai đầu tiên. */
  readonly lanGuiSai = input.required<number>();

  readonly submitted = output<void>();
  readonly closed = output<void>();
  readonly dismissAttempted = output<void>();
  readonly discardConfirmed = output<void>();
  readonly discardCancelled = output<void>();
  /** Ô mã vừa mất focus — page nhờ store chuẩn hoá về chữ hoa. */
  readonly maBlurred = output<void>();

  private readonly formEl = viewChild<ElementRef<HTMLFormElement>>('formEl');

  constructor() {
    // Bấm gửi khi form không hợp lệ → focus về ô sai đầu tiên (09-forms-validation.md §5).
    focusOSaiKhiGuiSai(this.lanGuiSai, () => this.formEl()?.nativeElement);
  }
}
