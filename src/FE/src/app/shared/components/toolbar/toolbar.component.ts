import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import { FilterChipComponent } from '../filter-chip/filter-chip.component';
import { BadgeComponent } from '../badge/badge.component';
import { ButtonComponent } from '../button/button.component';

/** Chữ ký đầy đủ ở quy-uoc/fe-ui-conventions.md §9. */
export interface ToolbarChip {
  readonly key: string;
  readonly label: string;
  readonly value: string | null;
  readonly removable: boolean;
  readonly lockReason?: string;
}

/** Bộ đếm sinh `id` ô tìm duy nhất mỗi thể hiện (Toolbar.md §API, `searchLabel`). */
let boDemIdOTim = 0;

/**
 * Design/Components/Toolbar.md. Tự dựng — dải bố cục gom control đã có sẵn (§Nền). Component
 * dumb: KHÔNG debounce, không gọi API — chống dồn dập thuộc `ListStateStore` (COMPONENTS.md §5).
 *
 * 🚧 Biến thể `full` (ô tìm + nút lọc có badge đếm + hàng chip + nhóm hành động) và `search`.
 * `actions`/`selection` chưa màn nào cần. Nút lọc chỉ PHÁT yêu cầu mở/đóng panel; panel
 * (`FilterPanel` trong `Drawer`) do trang cha dựng. Thứ tự Tab: ô tìm → nút lọc → chip → nhóm hành
 * động (DOM theo thứ tự đó; vị trí nhìn thấy do `order` trong SCSS).
 */
@Component({
  selector: 'app-toolbar',
  standalone: true,
  imports: [FilterChipComponent, ButtonComponent, BadgeComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './toolbar.component.html',
  styleUrl: './toolbar.component.scss',
})
export class ToolbarComponent {
  readonly variant = input<'full' | 'search' | 'actions' | 'selection'>('full');
  /** `attached`: không viền/bo/bóng của riêng dải — đặt vào chỗ cắm đầu khung `DataTable`. */
  readonly placement = input<'standalone' | 'attached'>('standalone');
  readonly size = input<'sm' | 'md'>('md');
  readonly searchValue = input('');
  readonly searchLabel = input.required<string>();
  readonly chips = input<readonly ToolbarChip[]>([]);
  readonly readonlyChips = input<readonly ToolbarChip[]>([]);
  readonly disabled = input(false);
  readonly loading = input(false);
  readonly clearAllLabel = input<string>('');

  /** Số điều kiện người dùng đặt trong panel; `0` thì badge ẩn. Không đếm chip chỉ đọc. */
  readonly activeFilterCount = input(0);
  /** Chữ nhìn thấy của nút lọc, đã dịch. Bắt buộc khác rỗng ở `variant = 'full'`. */
  readonly filterLabel = input('');
  /** Câu đọc lên mang số điều kiện, đã dịch; phải mở đầu bằng đúng `filterLabel` (WCAG 2.5.3). */
  readonly filterAriaLabel = input<string | null>(null);
  readonly filterPanelOpen = input(false);
  readonly filterPanelId = input<string | null>(null);

  readonly searchChanged = output<string>();
  readonly chipRemoved = output<string>();
  readonly filtersCleared = output<void>();
  /** Trạng thái mở/đóng MONG MUỐN của panel lọc — bấm nút lọc phát `!filterPanelOpen`. */
  readonly filterPanelToggled = output<boolean>();

  protected readonly idOTim = `toolbar-tim-${++boDemIdOTim}`;

  protected onSearchInput(gt: string): void {
    this.searchChanged.emit(gt);
  }
}
