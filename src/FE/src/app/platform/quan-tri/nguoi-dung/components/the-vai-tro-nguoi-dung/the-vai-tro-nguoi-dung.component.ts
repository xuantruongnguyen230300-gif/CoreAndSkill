import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import { BadgeComponent } from '../../../../../shared/components/badge/badge.component';
import { CardComponent } from '../../../../../shared/components/card/card.component';
import { EmptyStateComponent } from '../../../../../shared/components/empty-state/empty-state.component';
import { ButtonComponent } from '../../../../../shared/components/button/button.component';
import { VaiTroRutGon } from '../../models/nguoi-dung.model';

/**
 * Thẻ "Vai trò" của màn chi tiết người dùng (Design/Screens/10-nguoi-dung.md §Chi tiết). Component
 * dumb: nhận danh sách vai trò và cờ quyền bằng `input()`, chỉ phát `gan` khi bấm nút.
 */
@Component({
  selector: 'app-the-vai-tro-nguoi-dung',
  standalone: true,
  imports: [TranslatePipe, BadgeComponent, CardComponent, EmptyStateComponent, ButtonComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './the-vai-tro-nguoi-dung.component.html',
  styleUrl: './the-vai-tro-nguoi-dung.component.scss',
})
export class TheVaiTroNguoiDungComponent {
  readonly roles = input.required<readonly VaiTroRutGon[]>();
  /** Có quyền gán vai trò và xem danh mục vai trò — quyết định nút "Gán vai trò" có hiện không. */
  readonly coQuyenGan = input.required<boolean>();

  readonly gan = output<void>();
}
