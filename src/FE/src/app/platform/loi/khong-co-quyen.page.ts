import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import { CORE_ROUTES } from '../../core/config/core-routes';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';

/**
 * Design/Screens/02-trang-loi.md — màn "Không có quyền". Hiện khi `permissionGuard` từ chối một
 * tuyến; nằm TRONG khung app (fe-routing-guard.md §1) nên người dùng còn Sidebar để đi tiếp.
 * PageHeader `minimal` mang <h1>, thân là EmptyState cỡ `page` biến thể `no-permission`, đường đi
 * tiếp là liên kết tới `sauDangNhap` của seam CORE_ROUTES.
 */
@Component({
  selector: 'app-khong-co-quyen',
  standalone: true,
  imports: [TranslatePipe, PageHeaderComponent, EmptyStateComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './khong-co-quyen.page.html',
})
export class KhongCoQuyenPage {
  protected readonly routes = inject(CORE_ROUTES);
}
