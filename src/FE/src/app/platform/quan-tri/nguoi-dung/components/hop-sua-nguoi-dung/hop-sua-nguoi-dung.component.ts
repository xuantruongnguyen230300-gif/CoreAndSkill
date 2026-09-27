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
import { ButtonComponent } from '../../../../../shared/components/button/button.component';
import { DialogComponent } from '../../../../../shared/ui/dialog/dialog.component';
import { InputComponent } from '../../../../../shared/components/input/input.component';
import { LoiTruongSuaNguoiDung, SuaNguoiDungForm } from '../../models/nguoi-dung-hop.model';
import { BannerLoiHopComponent } from '../banner-loi-hop/banner-loi-hop.component';

/**
 * Hộp thoại "Sửa người dùng" (chi-tiet-nguoi-dung.page). Component dumb ĐÚNG NGHĨA của ADR-0049
 * điều 4: không `inject` gì, không biết store hay service — chỉ nhận GIÁ TRỊ (form, cờ, chuỗi lỗi)
 * qua `input()` và phát `output()` cho mọi hành động. Page nối lại với logic gửi và xử lý lỗi.
 */
@Component({
  selector: 'app-hop-sua-nguoi-dung',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    TranslatePipe,
    FormRowComponent,
    ButtonComponent,
    DialogComponent,
    InputComponent,
    BannerLoiHopComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './hop-sua-nguoi-dung.component.html',
})
export class HopSuaNguoiDungComponent {
  readonly hienThi = input.required<boolean>();
  readonly tenDangNhap = input.required<string>();
  readonly form = input.required<SuaNguoiDungForm>();
  /** Câu lỗi từng ô, đã tính sẵn — `null` = ô không có lỗi để hiện. */
  readonly loiTruong = input.required<LoiTruongSuaNguoiDung>();
  readonly xungDot = input.required<boolean>();
  readonly loiChung = input.required<string | null>();
  readonly dangGui = input.required<boolean>();
  /** Tăng mỗi lần bấm gửi mà form không hợp lệ — hộp đưa focus về ô sai đầu tiên. */
  readonly lanGuiSai = input.required<number>();

  readonly submitted = output<void>();
  readonly closed = output<void>();
  readonly dismissAttempted = output<void>();
  readonly thuLai = output<void>();

  private readonly formEl = viewChild<ElementRef<HTMLFormElement>>('formEl');

  constructor() {
    // Bấm gửi khi form không hợp lệ → focus về ô sai đầu tiên (09-forms-validation.md §5).
    focusOSaiKhiGuiSai(this.lanGuiSai, () => this.formEl()?.nativeElement);
  }
}
