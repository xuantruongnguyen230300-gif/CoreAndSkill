import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import { NoticeBannerComponent } from '../../../../../shared/components/notice-banner/notice-banner.component';
import { ButtonComponent } from '../../../../../shared/components/button/button.component';

/**
 * Banner đầu hộp thoại của khu người dùng: xung đột đồng thời (kèm nút "Thử lại") hoặc một câu lỗi
 * chung — dùng lại ở ba hộp thay vì chép ba lần. Component dumb: chỉ nhận GIÁ TRỊ, phát `thuLai`.
 */
@Component({
  selector: 'app-banner-loi-hop',
  standalone: true,
  imports: [TranslatePipe, NoticeBannerComponent, ButtonComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './banner-loi-hop.component.html',
  styleUrl: './banner-loi-hop.component.scss',
})
export class BannerLoiHopComponent {
  readonly xungDot = input.required<boolean>();
  readonly loiChung = input.required<string | null>();

  readonly thuLai = output<void>();
}
