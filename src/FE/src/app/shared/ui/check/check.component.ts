import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  input,
  output,
  signal,
} from '@angular/core';
import { ControlValueAccessor, FormsModule, NgControl } from '@angular/forms';
import { CheckboxModule } from 'primeng/checkbox';
import { RadioButtonModule } from 'primeng/radiobutton';

let dem = 0;

/**
 * Design/Components/Check.md. Bọc PrimeNG (`Checkbox`/`RadioButton`) — nửa chọn (`indeterminate`)
 * chỉ đặt được bằng property DOM, không có ở template thuần (§Nền).
 *
 * 🚧 F3 tối giản: dựng đủ biến thể `checkbox` (dùng ở ma trận phân quyền — Design/Screens/12) với
 * cả hai chế độ CVA (`[formControl]`) và không-CVA (`checked`/`checkedChange`, ca "ô chọn hàng
 * trong Table" mà Check.md §API dự kiến nói tới). Biến thể `radio` khai đủ input/output theo đúng
 * chữ ký spec nhưng CHƯA có màn F3 nào cần — chưa qua kiểm chứng thực tế; dựng khi có màn thật cần.
 */
@Component({
  selector: 'app-check',
  standalone: true,
  imports: [FormsModule, CheckboxModule, RadioButtonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './check.component.html',
  styleUrl: './check.component.scss',
})
export class CheckComponent implements ControlValueAccessor {
  protected readonly ngControl = inject(NgControl, { self: true, optional: true }); // token tha ở F11 — 05-gate.md §8.8

  constructor() {
    if (this.ngControl) {
      this.ngControl.valueAccessor = this;
    }
  }

  readonly type = input<'checkbox' | 'radio'>('checkbox');
  readonly size = input<'sm' | 'md'>('md');
  readonly indeterminate = input(false);
  readonly describedBy = input<string | null>(null);
  readonly name = input<string | null>(null);
  readonly value = input<string | number | null>(null);
  readonly ariaLabel = input<string | null>(null);
  readonly inputId = input<string>(`check-${++dem}`);

  /**
   * MỌI thuộc tính trợ năng đi chung một đường: pass-through `pt` của thư viện (cùng khuôn
   * `ptBang` của `DataTable`), ăn vào `[pBind]="ptm('input')"` có sẵn trên `<input>` ở cả hai
   * template thư viện (`primeng/checkbox`, `primeng/radiobutton`).
   *
   * 🛑 Không đặt bằng `[attr.…]` trên `<p-checkbox>`/`<p-radioButton>`: thuộc tính đó rơi lên
   * HOST của component thư viện, không phải lên `<input>`. Trình đọc màn hình đọc theo phần tử
   * đang FOCUS, và host `<p-checkbox>` còn không mang role nào — thuộc tính nằm đó bị bỏ qua
   * hoàn toàn, trong khi DOM vẫn "trông đúng" nên không có gì báo.
   *
   * 🛑 `aria-checked` CHỈ có mặt ở nhánh `checkbox`. Template `p-radioButton` của bản đang cài đã
   * có `[attr.aria-checked]="checked"` riêng trên `<input>` của nó; ghi đè qua `pt` là hai nguồn
   * cùng viết một thuộc tính — thư viện viết bằng binding của template, `pBind` viết bằng
   * `renderer.setAttribute` trong một `effect` — nên kết quả phụ thuộc thứ tự chạy và sai trong
   * im lặng. Template `p-checkbox` KHÔNG có binding đó, nên nửa-chọn phải tự mang
   * `aria-checked="mixed"` tới (Check.md §Accessibility, dòng "Nửa chọn").
   *
   * Khoá `aria-checked` giữ nguyên mặt ở nhánh `checkbox` kể cả khi hết nửa chọn, lúc đó mang giá
   * trị `null`: `pBind` chỉ gỡ thuộc tính cho những khoá CÓ trong đối tượng, nên bỏ hẳn khoá đi
   * sẽ để lại `aria-checked="mixed"` cũ bám trên `<input>`.
   */
  protected readonly ptO = computed(() => {
    const laRadio = this.type() === 'radio';
    return {
      input: {
        'aria-describedby': this.describedBy(),
        'aria-label': this.ariaLabel(),
        ...(laRadio ? {} : { 'aria-checked': this.indeterminate() ? 'mixed' : null }),
      },
    };
  });

  /** Chỉ dùng khi ô KHÔNG gắn control — ô chọn hàng trong Table.md. */
  readonly checked = input(false);
  readonly disabled = input(false);
  readonly checkedChange = output<boolean>();

  /**
   * 🛑 Hai trường dưới đây PHẢI là `signal`, cùng khuôn `gia` / `voHieuHoa` của
   * `input.component.ts` — không phải trường thường.
   *
   * Component khai `OnPush` và ứng dụng chạy zoneless: `writeValue` và `setDisabledState` do
   * `@angular/forms` gọi thẳng, **ngoài** mọi binding của template, nên không có gì đánh dấu view
   * bẩn. Ghi vào trường thường thì giá trị vào đúng chỗ mà DOM đứng yên — đo được:
   * `control.setValue(true)` sau lượt dựng đầu xong `<input>.checked` vẫn `false`, cả hai nhánh;
   * `control.disable()` xong `<input>` vẫn không mang `disabled`, tức một ô đã khoá vẫn bấm được.
   *
   * Hỏng kiểu này KHÔNG phát tín hiệu nào: không lỗi, không cảnh báo, DOM vẫn hợp lệ, chỉ là giá
   * trị cũ. Ghi vào `signal` thì template đọc nó qua getter `daChon` / `voHieuHoa`, lượt đọc đó
   * nằm trong ngữ cảnh phản ứng của template nên `set` đánh dấu view bẩn và lên lịch vẽ lại.
   * Khoá bằng test: `check.component.spec.ts` § `vẽ lại được sau lượt dựng đầu`, mỗi ca dựng xong
   * lượt đầu TRƯỚC rồi mới đổi control — đổi trước lượt dựng đầu thì ca xanh cả khi lỗi còn nguyên.
   */
  private readonly disabledCVA = signal(false);
  /** Giá trị hiện của control CVA — `boolean` cho `checkbox`, giá trị lựa chọn cho `radio`. */
  protected readonly giaTriCVA = signal<unknown>(false);

  protected get daChon(): boolean {
    if (!this.ngControl) {
      return this.checked();
    }
    return this.type() === 'radio' ? this.giaTriCVA() === this.value() : this.giaTriCVA() === true;
  }

  protected get voHieuHoa(): boolean {
    return this.ngControl ? this.disabledCVA() : this.disabled();
  }

  protected get loi(): boolean {
    return !!this.ngControl && this.ngControl.invalid === true && this.ngControl.touched === true;
  }

  /** `checkbox` không CVA: phát boolean mới. `checkbox` có CVA: ghi boolean vào control. */
  protected doiGiaTriCheckbox(gt: boolean): void {
    if (!this.ngControl) {
      this.checkedChange.emit(gt);
      return;
    }
    this.giaTriCVA.set(gt);
    this.onChange(gt);
    this.onTouched();
  }

  /** `radio`: mọi lựa chọn trong nhóm ghi CÙNG một control — chọn lựa chọn này thì ghi `value()`. */
  protected chonRadio(): void {
    if (this.voHieuHoa) return;
    this.giaTriCVA.set(this.value());
    this.onChange(this.giaTriCVA());
    this.onTouched();
  }

  // --- ControlValueAccessor ---
  private onChange: (value: unknown) => void = () => undefined;
  private onTouched: () => void = () => undefined;

  writeValue(value: unknown): void {
    this.giaTriCVA.set(value);
  }

  registerOnChange(fn: (value: unknown) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabledCVA.set(isDisabled);
  }
}
