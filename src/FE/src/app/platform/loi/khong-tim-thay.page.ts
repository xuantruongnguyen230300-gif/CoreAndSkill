import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import { CORE_ROUTES } from '../../core/config/core-routes';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';

/**
 * Design/Screens/02-trang-loi.md — màn "Không tìm thấy", tuyến cuối `/**` của nhánh khung app
 * (fe-routing-guard.md §1). Cùng khuôn với màn "Không có quyền": PageHeader `minimal`, thân là
 * EmptyState cỡ `page` biến thể `not-found`, đường đi tiếp là liên kết tới `sauDangNhap`.
 */
@Component({
  selector: 'app-khong-tim-thay',
  standalone: true,
  imports: [TranslatePipe, PageHeaderComponent, EmptyStateComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './khong-tim-thay.page.html',
})
export class KhongTimThayPage {
  protected readonly routes = inject(CORE_ROUTES);
}
