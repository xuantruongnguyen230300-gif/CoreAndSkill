import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

import type { LanguageOption } from '../../../core/config/core-i18n';
import type { UiMenuItem } from '../../ui/types';

export interface TopbarUser {
  readonly fullName: string;
  readonly userName: string;
  readonly email: string | null;
}

/**
 * Design/Components/Topbar.md. 🚧 Minimal F2 (ADR-0037): khu tài khoản hiện INLINE (không phải
 * lớp nổi `Menu.md` — chưa dựng ở F2); nút theme, mục "Ngôn ngữ" và hamburger/nút thu-gọn Sidebar
 * (gắn với `sidebarMode` khác `'expanded'`, cũng hoãn) CHƯA render — API khai đủ chữ ký cho F3,
 * xem bảng "đã có → còn thiếu" ở Topbar.md. Component DUMB: không tự đọc phiên, không tự gọi
 * router (COMPONENTS.md §5) — `platform/shell` truyền dữ liệu qua `input()`, nhận lại qua
 * `output()`.
 */
@Component({
  selector: 'app-topbar',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './topbar.component.html',
  styleUrl: './topbar.component.scss',
})
export class TopbarComponent {
  readonly title = input<string | null>(null);
  readonly user = input<TopbarUser | null>(null);
  readonly tenantName = input('');
  readonly menuItems = input<readonly UiMenuItem[]>([]);
  readonly sidebarMode = input<'expanded' | 'collapsed' | 'drawer'>('expanded');
  readonly drawerOpen = input(false);
  readonly state = input<'idle' | 'loading' | 'error'>('idle');
  readonly languages = input<readonly LanguageOption[]>([]);
  readonly currentLanguage = input<string | null>(null);
  readonly pendingLanguage = input<string | null>(null);
  readonly theme = input<'light' | 'dark' | 'system'>('light');

  readonly menuToggled = output<void>();
  /** `key` của mục trong menu tài khoản — `platform/shell` diễn giải, Topbar không biết route. */
  readonly menuItemSelected = output<string>();
  readonly languageChangeRequested = output<string>();
  readonly themeChangeRequested = output<'light' | 'dark' | 'system'>();

  protected readonly chuCaiDau = computed(() =>
    (this.user()?.fullName.trim().charAt(0) ?? '?').toUpperCase(),
  );
}
