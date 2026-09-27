import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

let dem = 0;

/**
 * Design/Components/FormRow.md. Gói MỘT ô nhập cùng nhãn, dấu bắt buộc, gợi ý và chỗ hiện lỗi —
 * nơi DUY NHẤT quyết định lỗi hiện Ở ĐÂU. `FormRow` là component dumb: nó KHÔNG biết luật kiểm
 * tra dữ liệu, chỉ vẽ đúng chuỗi `error` nhận vào.
 */
@Component({
  selector: 'app-form-row',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './form-row.component.html',
  styleUrl: './form-row.component.scss',
})
export class FormRowComponent {
  readonly label = input.required<string>();
  readonly layout = input<'stacked' | 'inline' | 'group'>('stacked');
  readonly required = input(false);
  readonly hint = input<string | null>(null);
  /** `null` = không lỗi. Chuỗi rỗng CŨNG coi là không lỗi. */
  readonly error = input<string | null>(null);
  readonly disabled = input(false);
  readonly controlId = input<string>(`form-row-${++dem}`);

  protected readonly coLoi = computed(() => (this.error() ?? '').length > 0);
  protected readonly idLoi = computed(() => `${this.controlId()}-error`);
  protected readonly idGoiY = computed(() => `${this.controlId()}-hint`);
}
