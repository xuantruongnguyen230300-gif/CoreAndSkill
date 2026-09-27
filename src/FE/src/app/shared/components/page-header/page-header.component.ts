import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';

import { SkeletonLoaderComponent } from '../skeleton-loader/skeleton-loader.component';

let dem = 0;

export interface PageHeaderBreadcrumb {
  readonly label: string;
  readonly route: string | null;
}

/**
 * Design/Components/PageHeader.md. 🚧 Minimal F2 (ADR-0037): biến thể hôm nay chỉ cần `minimal`
 * (chỉ tiêu đề, dùng ở `ho-so.page`) — breadcrumb, nút quay lại và slot badge ĐÃ dựng đúng API
 * dự kiến (không thêm biến thể `variant` — quyết định này là chủ đích của chính spec: hình dạng
 * suy ra từ input nào có giá trị, không phải một cờ biến thể riêng), nhưng chưa có màn nào dùng
 * tới ở F2 nên chưa được thử qua thực tế. Component dumb: nhận breadcrumb đã dịch qua input(),
 * không tự đọc router (COMPONENTS.md §5).
 */
@Component({
  selector: 'app-page-header',
  standalone: true,
  imports: [RouterLink, TranslatePipe, SkeletonLoaderComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './page-header.component.html',
  styleUrl: './page-header.component.scss',
})
export class PageHeaderComponent {
  readonly title = input.required<string>();
  readonly description = input<string | null>(null);
  readonly breadcrumbs = input<readonly PageHeaderBreadcrumb[]>([]);
  readonly backRoute = input<string | null>(null);
  readonly backLabel = input<string | null>(null);
  readonly loading = input(false);
  readonly headingId = input<string>(`page-header-tieu-de-${++dem}`);
}
