import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

/**
 * Design/Components/SkeletonLoader.md. 🚧 Minimal F2 (ADR-0037): `variant` `text`/`block`/`circle`
 * dựng đủ; `group` + `preset` khai trong API (chữ ký khớp F3) nhưng KHÔNG render gì — danh sách
 * khuôn `preset` còn để ngỏ ở spec (Cần chốt #2), F2 không cần: `ho-so.page` tự ghép nhiều
 * `SkeletonLoader` đơn thay vì gọi một preset chưa được định nghĩa. Component dumb tuyệt đối —
 * không `output()`, không biết mình đang chờ gì (COMPONENTS.md §5).
 *
 * Dải sáng chạy tắt khi `prefers-reduced-motion: reduce` qua quy tắc TOÀN CỤC đã có ở
 * `styles.scss` (ép `animation-duration: 0.01ms`) — không cần lặp lại ở đây.
 */
@Component({
  selector: 'app-skeleton-loader',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './skeleton-loader.component.html',
  styleUrl: './skeleton-loader.component.scss',
})
export class SkeletonLoaderComponent {
  readonly variant = input<'text' | 'block' | 'circle' | 'group'>('text');
  readonly size = input<'sm' | 'md' | 'lg'>('md');
  readonly width = input<'full' | 'wide' | 'half' | 'short'>('full');
  readonly lines = input(1);
  readonly repeat = input(1);
  /** Khai cho đủ chữ ký — CHƯA có preset nào được hiện thực (xem ghi chú đầu file). */
  readonly preset = input<string | null>(null);
  readonly rounded = input(false);

  protected readonly danhSachLap = computed(() =>
    Array.from({ length: Math.max(1, this.repeat()) }, (_, i) => i),
  );
  protected readonly danhSachDong = computed(() =>
    Array.from({ length: Math.max(1, this.lines()) }, (_, i) => i),
  );
}
