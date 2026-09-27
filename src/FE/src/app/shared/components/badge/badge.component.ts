import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/**
 * Design/Components/Badge.md. Tự dựng — một `<span>` có đệm và có màu, không hành vi (§Nền).
 * Component dumb: không ánh xạ mã trạng thái sang màu, nơi gọi truyền `variant` (COMPONENTS.md §5).
 */
@Component({
  selector: 'app-badge',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './badge.component.html',
  styleUrl: './badge.component.scss',
})
export class BadgeComponent {
  readonly variant = input<'neutral' | 'outline' | 'success' | 'warning' | 'danger' | 'info'>('neutral');
  readonly size = input<'sm' | 'md'>('md');
  readonly shape = input<'pill' | 'square'>('pill');
  readonly icon = input<string | null>(null);
  readonly uppercase = input(false);
  readonly onSurface2 = input(false);
}
