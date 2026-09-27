import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

/**
 * Design/Components/NoticeBanner.md. Nói một điều về BỐI CẢNH HIỆN TẠI, ở lại cho tới khi bối
 * cảnh đổi — khác `Toast` (thoáng qua). Component không tự gỡ mình khỏi DOM; nơi gọi quyết định
 * (dùng `@if` bọc ngoài).
 */
@Component({
  selector: 'app-notice-banner',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './notice-banner.component.html',
  styleUrl: './notice-banner.component.scss',
})
export class NoticeBannerComponent {
  readonly severity = input<'info' | 'success' | 'warning' | 'danger'>('info');
  readonly heading = input<string | null>(null);
  readonly headingLevel = input<2 | 3 | 4 | null>(null);
  readonly size = input<'sm' | 'md'>('md');

  protected readonly role = computed(() => (this.severity() === 'danger' ? 'alert' : 'status'));
  protected readonly icon = computed(() => {
    switch (this.severity()) {
      case 'success':
        return 'check-circle';
      case 'warning':
        return 'exclamation-triangle';
      case 'danger':
        return 'times-circle';
      default:
        return 'info-circle';
    }
  });
}
