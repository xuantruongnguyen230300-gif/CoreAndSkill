import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { DatePipe } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';

import { CardComponent } from '../../../../../shared/components/card/card.component';
import { FormRowComponent } from '../../../../../shared/components/form-row/form-row.component';
import { InputComponent } from '../../../../../shared/components/input/input.component';
import { NguoiDung } from '../../models/nguoi-dung.model';

/** Thẻ "Thông tin" của màn chi tiết người dùng (Design/Screens/10-nguoi-dung.md §Chi tiết). Chỉ đọc. */
@Component({
  selector: 'app-the-thong-tin-nguoi-dung',
  standalone: true,
  imports: [TranslatePipe, DatePipe, CardComponent, FormRowComponent, InputComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './the-thong-tin-nguoi-dung.component.html',
  styleUrl: './the-thong-tin-nguoi-dung.component.scss',
})
export class TheThongTinNguoiDungComponent {
  readonly nguoiDung = input.required<NguoiDung>();
}
