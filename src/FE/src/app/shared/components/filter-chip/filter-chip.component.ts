import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import { IconButtonComponent } from '../icon-button/icon-button.component';
import { TooltipComponent } from '../../ui/tooltip/tooltip.component';

/**
 * Design/Components/FilterChip.md. Tự dựng (§Nền). Component dumb — không biết điều kiện này lọc
 * ra bao nhiêu bản ghi, không tự gọi lại danh sách; trang truyền `label`/`value` đã dịch
 * (COMPONENTS.md §5).
 */
@Component({
  selector: 'app-filter-chip',
  standalone: true,
  imports: [IconButtonComponent, TooltipComponent, TranslatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './filter-chip.component.html',
  styleUrl: './filter-chip.component.scss',
})
export class FilterChipComponent {
  readonly key = input.required<string>();
  readonly label = input.required<string>();
  readonly value = input<string | null>(null);
  readonly variant = input<'applied' | 'toggle' | 'readonly'>('applied');
  /** Chỉ có nghĩa với `variant = 'toggle'`. */
  readonly pressed = input(false);
  /** Bắt buộc khác `null` khi `variant` là `'readonly'`. */
  readonly lockReason = input<string | null>(null);

  /** Phát `key`. KHÔNG phát ở biến thể `readonly` — chặn ở component (FilterChip.md §API). */
  readonly removed = output<string>();
  /** Chỉ phát ở biến thể `toggle`. */
  readonly toggled = output<boolean>();

  protected readonly nhanNutGo = computed(() => ({
    nhan: this.label(),
    giaTri: this.value() ?? '',
  }));

  protected onToggle(): void {
    if (this.variant() !== 'toggle') {
      return;
    }
    this.toggled.emit(!this.pressed());
  }

  protected onRemove(): void {
    if (this.variant() === 'readonly') {
      return;
    }
    this.removed.emit(this.key());
  }
}
