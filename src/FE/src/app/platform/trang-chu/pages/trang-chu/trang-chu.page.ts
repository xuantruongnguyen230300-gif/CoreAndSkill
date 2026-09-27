import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import { AuthService } from '../../../../core/auth/auth.service';

/**
 * fe-architecture.md §2.3 — "Core chỉ cấp trang chào tối giản: tên đơn vị, lời chào, lối tắt tới
 * các màn người dùng có quyền; không gọi endpoint riêng, chỉ dùng lại thông tin phiên và menu."
 * Dự án thay bằng bảng tổng hợp của mình qua seam `CORE_HOME` (§2.5, ADR-0057): route trang chủ
 * (`trang-chu.routes.ts`) chỉ nạp trang này khi dự án KHÔNG khai `provideCoreHome`.
 */
@Component({
  selector: 'app-trang-chu',
  standalone: true,
  imports: [TranslatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './trang-chu.page.html',
})
export class TrangChuPage {
  protected readonly auth = inject(AuthService);
}
