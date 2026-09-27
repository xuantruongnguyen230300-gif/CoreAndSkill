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
import { AutocompleteComponent } from '../../../../../shared/ui/autocomplete/autocomplete.component';
import { ButtonComponent } from '../../../../../shared/components/button/button.component';
import { DialogComponent } from '../../../../../shared/ui/dialog/dialog.component';
import { InputComponent } from '../../../../../shared/components/input/input.component';
import {
  LoiTruongTaoNguoiDung,
  TaoNguoiDungForm,
  VaiTroChon,
} from '../../models/nguoi-dung-hop.model';

/**
 * Hộp thoại "Thêm người dùng" (danh-sach-nguoi-dung.page). Component dumb ĐÚNG NGHĨA của ADR-0049
 * điều 4: không `inject` gì, không biết `TraCuuVaiTroStore` hay service — chỉ nhận GIÁ TRỊ (form,
 * cờ, chuỗi lỗi, trạng thái ô chọn vai trò) qua `input()` và phát `output()` cho mọi hành động.
 * Hộp xác nhận "rời hộp khi còn thay đổi chưa lưu" do page giữ.
 */
@Component({
  selector: 'app-hop-tao-nguoi-dung',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    TranslatePipe,
    AutocompleteComponent,
    FormRowComponent,
    NoticeBannerComponent,
    ButtonComponent,
    DialogComponent,
    InputComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './hop-tao-nguoi-dung.component.html',
  styleUrl: './hop-tao-nguoi-dung.component.scss',
})
export class HopTaoNguoiDungComponent {
  readonly hienThi = input.required<boolean>();
  readonly form = input.required<TaoNguoiDungForm>();
  /** Câu lỗi từng ô, đã tính sẵn — `null` = ô không có lỗi để hiện. */
  readonly loiTruong = input.required<LoiTruongTaoNguoiDung>();
  readonly loiChung = input.required<string | null>();
  readonly dangLuu = input.required<boolean>();
  /** Có quyền gán vai trò và xem danh mục vai trò — quyết định ô "Vai trò" có hiện không. */
  readonly coQuyenGanVaiTro = input.required<boolean>();
  readonly vaiTro = input.required<VaiTroChon>();
  /** Tăng mỗi lần bấm gửi mà form không hợp lệ — hộp đưa focus về ô sai đầu tiên. */
  readonly lanGuiSai = input.required<number>();

  readonly submitted = output<void>();
  readonly closed = output<void>();
  readonly dismissAttempted = output<void>();
  readonly timKiem = output<string>();
  readonly chon = output<string | readonly string[]>();

  private readonly formEl = viewChild<ElementRef<HTMLFormElement>>('formEl');

  constructor() {
    // Bấm gửi khi form không hợp lệ → focus về ô sai đầu tiên (09-forms-validation.md §5).
    focusOSaiKhiGuiSai(this.lanGuiSai, () => this.formEl()?.nativeElement);
  }
}
