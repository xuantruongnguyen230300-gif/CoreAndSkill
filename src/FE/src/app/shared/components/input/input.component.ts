import { ChangeDetectionStrategy, Component, computed, inject, input, signal } from '@angular/core';
import { ControlValueAccessor, NgControl } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';

/** Một lựa chọn của biến thể `select` — dùng lại kiểu đã có (fe-ui-conventions.md §9). */
export interface SegmentOption {
  readonly value: string;
  readonly label: string;
}

/**
 * Design/Components/Input.md. `Input` KHÔNG tự vẽ nhãn — bọc trong `FormRow`. Form control chuẩn
 * Angular qua `ControlValueAccessor`; trạng thái `error` suy từ control: `invalid && touched`.
 *
 * F2 chỉ hiện thực đúng ba biến thể mà các màn F2 cần (`text`, `password`, `select`) — `number`,
 * `textarea`, `search` để lại cho lúc có màn thật cần chúng.
 */
@Component({
  selector: 'app-input',
  standalone: true,
  imports: [TranslatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './input.component.html',
  styleUrl: './input.component.scss',
})
export class InputComponent implements ControlValueAccessor {
  protected readonly ngControl = inject(NgControl, { self: true, optional: true });

  constructor() {
    if (this.ngControl) {
      this.ngControl.valueAccessor = this;
    }
  }

  readonly id = input<string | null>(null);
  readonly type = input<'text' | 'password' | 'select'>('text');
  readonly size = input<'sm' | 'md' | 'lg'>('md');
  readonly width = input<'full' | 'md' | 'sm'>('full');
  readonly placeholder = input<string | null>(null);
  readonly readonly = input(false);
  readonly describedBy = input<string | null>(null);
  readonly autocomplete = input<string | null>(null);
  readonly inputmode = input<'text' | 'decimal' | 'numeric' | 'tel' | 'email' | 'url' | 'search' | null>(null);
  readonly revealable = input(true);
  readonly options = input<readonly SegmentOption[]>([]);
  /** Chỉ dùng khi ô KHÔNG gắn control — hiển thị thuần (Input.md §API dự kiến, ca `readonly`). */
  readonly value = input<string | number | null>(null);

  protected readonly gia = signal('');
  protected readonly voHieuHoa = signal(false);
  protected readonly hienMatKhau = signal(false);

  /** Không gắn control → hiện `value()` truyền vào; có control → hiện giá trị CVA giữ. */
  protected readonly hienThi = computed(() => (this.ngControl ? this.gia() : String(this.value() ?? '')));

  protected get loi(): boolean {
    return !!this.ngControl && this.ngControl.invalid === true && this.ngControl.touched === true;
  }

  protected get kieuThat(): 'text' | 'password' {
    return this.type() === 'password' && !this.hienMatKhau() ? 'password' : 'text';
  }

  protected doiGia(gt: string): void {
    this.gia.set(gt);
    this.onChange(gt);
    this.onTouched();
  }

  protected chamVao(): void {
    this.onTouched();
  }

  protected toggleHienMatKhau(): void {
    this.hienMatKhau.update((v) => !v);
  }

  // --- ControlValueAccessor ---
  private onChange: (value: string) => void = () => undefined;
  private onTouched: () => void = () => undefined;

  writeValue(value: string | null): void {
    this.gia.set(value ?? '');
  }

  registerOnChange(fn: (value: string) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.voHieuHoa.set(isDisabled);
  }
}
