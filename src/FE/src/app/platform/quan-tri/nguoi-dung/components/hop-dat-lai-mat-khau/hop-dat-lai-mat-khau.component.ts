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

import { FormRowComponent } from '../../../../../shared/components/form-row/form-row.component';
import { focusOSaiKhiGuiSai } from '../../../../../shared/forms/focus-o-sai-dau-tien';
import { NoticeBannerComponent } from '../../../../../shared/components/notice-banner/notice-banner.component';
import { ButtonComponent } from '../../../../../shared/components/button/button.component';
import { DialogComponent } from '../../../../../shared/ui/dialog/dialog.component';
import { InputComponent } from '../../../../../shared/components/input/input.component';
import { DatLaiMatKhauForm } from '../../models/nguoi-dung-hop.model';
import { BannerLoiHopComponent } from '../banner-loi-hop/banner-loi-hop.component';

/**
 * Hộp thoại "Đặt lại mật khẩu" (chi-tiet-nguoi-dung.page). Component dumb (ADR-0049 điều 4): chỉ
 * nhận GIÁ TRỊ qua `input()`, phát `output()` cho mọi hành động.
 */
@Component({
  selector: 'app-hop-dat-lai-mat-khau',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    TranslatePipe,
    FormRowComponent,
    NoticeBannerComponent,
    ButtonComponent,
    DialogComponent,
    InputComponent,
    BannerLoiHopComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './hop-dat-lai-mat-khau.component.html',
})
export class HopDatLaiMatKhauComponent {
  readonly hienThi = input.required<boolean>();
  readonly tenDangNhap = input.required<string>();
  readonly form = input.required<DatLaiMatKhauForm>();
  /** Câu lỗi của ô mật khẩu tạm, đã tính sẵn — `null` = không có lỗi để hiện. */
  readonly loiMatKhau = input.required<string | null>();
  readonly xungDot = input.required<boolean>();
  readonly loiChung = input.required<string | null>();
  readonly dangGui = input.required<boolean>();
  /** Tăng mỗi lần bấm gửi mà form không hợp lệ — hộp đưa focus về ô sai. */
  readonly lanGuiSai = input.required<number>();

  readonly submitted = output<void>();
  readonly closed = output<void>();
  readonly dismissAttempted = output<void>();
  readonly thuLai = output<void>();

  private readonly formEl = viewChild<ElementRef<HTMLFormElement>>('formEl');

  constructor() {
    // Bấm gửi khi form không hợp lệ → focus về ô sai (09-forms-validation.md §5).
    focusOSaiKhiGuiSai(this.lanGuiSai, () => this.formEl()?.nativeElement);
  }
}
