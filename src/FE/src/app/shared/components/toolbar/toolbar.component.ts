import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import { FilterChipComponent } from '../filter-chip/filter-chip.component';
import { ButtonComponent } from '../button/button.component';

/** Chữ ký đầy đủ ở quy-uoc/fe-ui-conventions.md §9. */
export interface ToolbarChip {
  readonly key: string;
  readonly label: string;
  readonly value: string | null;
  readonly removable: boolean;
  readonly lockReason?: string;
}

/**
 * Design/Components/Toolbar.md. Tự dựng — dải bố cục gom control đã có sẵn (§Nền). Component
 * dumb: KHÔNG debounce, không gọi API — chống dồn dập thuộc `ListStateStore` (COMPONENTS.md §5).
 *
 * 🚧 F3 tối giản: biến thể `full` (khu lọc 1–2 trường nằm thẳng trong dải, qua slot nội dung —
 * đúng nhu cầu của cả ba màn F3) và `search`. `actions`/`selection` chưa màn nào cần.
 */
@Component({
  selector: 'app-toolbar',
  standalone: true,
  imports: [FilterChipComponent, ButtonComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './toolbar.component.html',
  styleUrl: './toolbar.component.scss',
})
export class ToolbarComponent {
  readonly variant = input<'full' | 'search' | 'actions' | 'selection'>('full');
  readonly size = input<'sm' | 'md'>('md');
  readonly searchValue = input('');
  readonly searchLabel = input.required<string>();
  readonly chips = input<readonly ToolbarChip[]>([]);
  readonly readonlyChips = input<readonly ToolbarChip[]>([]);
  readonly disabled = input(false);
  readonly loading = input(false);
  readonly clearAllLabel = input<string>('');

  readonly searchChanged = output<string>();
  readonly chipRemoved = output<string>();
  readonly filtersCleared = output<void>();

  protected onSearchInput(gt: string): void {
    this.searchChanged.emit(gt);
  }
}
