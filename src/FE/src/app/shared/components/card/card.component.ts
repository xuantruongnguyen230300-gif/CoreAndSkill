import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';

let dem = 0;

/**
 * Design/Components/Card.md. 🚧 Minimal F2 (ADR-0037): đủ API cho nhu cầu `ho-so.page` — `heading`
 * qua `<h2>`/`<h3>`/`<h4>` thật, `loading` chỉ đặt `aria-busy` trên thân (nội dung thân do TRANG
 * quyết định render gì lúc đang tải — Card không giữ slot loading riêng, xem `Cần chốt` #2 mới
 * trong Card.md nếu có). Biến thể `interactive` dựng tối giản (chưa có `routerLink`, API dự kiến
 * hôm nay cũng chưa có input đó). Component dumb: không inject service (COMPONENTS.md §5).
 */
@Component({
  selector: 'app-card',
  standalone: true,
  imports: [NgTemplateOutlet],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './card.component.html',
  styleUrl: './card.component.scss',
})
export class CardComponent {
  readonly variant = input<'default' | 'interactive' | 'flat'>('default');
  readonly size = input<'sm' | 'md' | 'lg'>('md');
  readonly heading = input<string | null>(null);
  readonly headingLevel = input<2 | 3 | 4>(2);
  readonly description = input<string | null>(null);
  readonly loading = input(false);
  readonly disabled = input(false);

  /** Chỉ phát ở biến thể `interactive`, và không phát khi `disabled` hoặc `loading`. */
  readonly activated = output<void>();

  protected readonly idTieuDe = `card-tieu-de-${++dem}`;

  protected kichHoat(): void {
    if (this.variant() !== 'interactive' || this.disabled() || this.loading()) {
      return;
    }
    this.activated.emit();
  }
}
