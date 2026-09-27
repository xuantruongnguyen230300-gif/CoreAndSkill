import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';

import { ButtonComponent } from '../button/button.component';

/** Cây menu — fe-ui-conventions.md §9. Tối đa hai cấp (Sidebar.md §Kích thước). */
export interface NavItem {
  readonly id: string;
  readonly label: string;
  readonly icon?: string;
  /** Bỏ trống = mục chỉ để nhóm, không điều hướng. */
  readonly route?: string;
  readonly children?: readonly NavItem[];
  readonly disabled?: boolean;
}

/**
 * Design/Components/Sidebar.md. 🚧 Minimal F2 (ADR-0037): chỉ `mode = 'expanded'` dựng đủ hình
 * ảnh — `collapsed` (flyout, ghi nhớ localStorage) và `drawer` (bọc `Drawer.md`, CHƯA dựng) hoãn
 * tới F3, xem bảng "đã có → còn thiếu" ở Sidebar.md. Component DUMB: nhận `items`/`activeRoute`/
 * `state` qua `input()`, không tự đọc router, không tự gọi menu store (COMPONENTS.md §5) —
 * `platform/shell` là tầng smart truyền dữ liệu xuống.
 */
@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [RouterLink, TranslatePipe, ButtonComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './sidebar.component.html',
  styleUrl: './sidebar.component.scss',
})
export class SidebarComponent {
  readonly items = input<readonly NavItem[]>([]);
  readonly mode = input<'expanded' | 'collapsed' | 'drawer'>('expanded');
  readonly drawerOpen = input(false);
  /** Bắt buộc. Truyền vào thay vì tự đọc router — điều giữ component dumb (Sidebar.md §API). */
  readonly activeRoute = input.required<string>();
  readonly state = input<'idle' | 'loading' | 'error' | 'empty'>('idle');

  readonly modeChange = output<'expanded' | 'collapsed'>();
  readonly drawerClose = output<void>();
  readonly retry = output<void>();

  /** So khớp bỏ query string; coi là active cả khi đang ở một tuyến con của mục. */
  protected dangO(route: string | undefined): boolean {
    if (!route) {
      return false;
    }
    const hienTai = this.activeRoute().split('?')[0];
    return hienTai === route || hienTai.startsWith(`${route}/`);
  }

  /**
   * Active cho mục CẤP CHA (dùng cho `<a>` ở cấp 1 của cây menu). Mục cha có cả `route` và
   * `children` chỉ sáng khi khớp CHÍNH XÁC — không dùng so khớp tiền tố của {@link dangO}.
   * Nếu không, một route con là tiền tố của route cha (ví dụ cha `/quan-tri`, con
   * `/quan-tri/nguoi-dung`) sẽ làm CẢ HAI mục cùng nhận `aria-current="page"`, vi phạm
   * Sidebar.md §Accessibility (đúng một mục được đánh dấu hiện tại). Mục không có children
   * (mục lá) vẫn dùng so khớp tiền tố như cũ.
   */
  protected dangOCha(muc: NavItem): boolean {
    if (!muc.route) {
      return false;
    }
    if (this.coCon(muc)) {
      return this.activeRoute().split('?')[0] === muc.route;
    }
    return this.dangO(muc.route);
  }

  protected coCon(muc: NavItem): boolean {
    return (muc.children?.length ?? 0) > 0;
  }
}
