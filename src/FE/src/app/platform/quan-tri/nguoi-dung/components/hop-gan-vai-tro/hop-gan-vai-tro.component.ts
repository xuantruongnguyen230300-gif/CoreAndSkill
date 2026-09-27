import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import { BadgeComponent } from '../../../../../shared/components/badge/badge.component';
import { FormRowComponent } from '../../../../../shared/components/form-row/form-row.component';
import { AutocompleteComponent } from '../../../../../shared/ui/autocomplete/autocomplete.component';
import { ButtonComponent } from '../../../../../shared/components/button/button.component';
import { DialogComponent } from '../../../../../shared/ui/dialog/dialog.component';
import { VaiTroChon } from '../../models/nguoi-dung-hop.model';
import { BannerLoiHopComponent } from '../banner-loi-hop/banner-loi-hop.component';

/** Vai trò đứng ngoài ô chọn — chỉ cần id và tên để hiển thị. */
export interface VaiTroHienThi {
  readonly id: string;
  readonly name: string;
}

/**
 * Hộp thoại "Gán vai trò" (chi-tiet-nguoi-dung.page). Component dumb (ADR-0049 điều 4): KHÔNG
 * nhận `GanVaiTroChonStore` — page đọc store rồi truyền GIÁ TRỊ (`vaiTro`, `dirty`, …) và nghe
 * `output()` để gọi hàm của store.
 */
@Component({
  selector: 'app-hop-gan-vai-tro',
  standalone: true,
  imports: [
    TranslatePipe,
    AutocompleteComponent,
    BadgeComponent,
    FormRowComponent,
    ButtonComponent,
    DialogComponent,
    BannerLoiHopComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './hop-gan-vai-tro.component.html',
  styleUrl: './hop-gan-vai-tro.component.scss',
})
export class HopGanVaiTroComponent {
  readonly hienThi = input.required<boolean>();
  readonly tenDangNhap = input.required<string>();
  /** Còn thay đổi chưa lưu — Dialog dựa vào đây để hỏi trước khi đóng. */
  readonly dirty = input.required<boolean>();
  readonly vaiTro = input.required<VaiTroChon>();
  /** Vai trò hệ thống của chính người đang xem, đứng ngoài ô chọn (users.md §2 luật 2). */
  readonly heThongCuaMinh = input.required<readonly VaiTroHienThi[]>();
  readonly xungDot = input.required<boolean>();
  readonly loiChung = input.required<string | null>();
  readonly dangGui = input.required<boolean>();

  readonly submitted = output<void>();
  readonly closed = output<void>();
  readonly dismissAttempted = output<void>();
  readonly thuLai = output<void>();
  readonly timKiem = output<string>();
  readonly chon = output<string | readonly string[]>();
}
