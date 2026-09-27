import { ChangeDetectionStrategy, Component, ElementRef, computed, inject, input, signal, viewChild } from '@angular/core';
import { ControlValueAccessor, NgControl } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';

let dem = 0;

/**
 * Design/Components/AuthField.md. Ô nhập riêng cho màn xác thực — icon dẫn, cỡ `lg` mặc định.
 * Form control chuẩn Angular qua CVA. Đơn giản hoá có ghi nhận so với spec đầy đủ: hiện thực
 * TRỰC TIẾP trên `<input>` gốc thay vì lồng `app-input` bên trong (tránh CVA lồng CVA) — vẫn
 * cùng ngôn ngữ token và cùng hợp đồng accessibility với `Input.md`.
 */
@Component({
  selector: 'app-auth-field',
  standalone: true,
  imports: [TranslatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './auth-field.component.html',
  styleUrl: './auth-field.component.scss',
})
export class AuthFieldComponent implements ControlValueAccessor {
  protected readonly ngControl = inject(NgControl, { self: true, optional: true });

  constructor() {
    if (this.ngControl) {
      this.ngControl.valueAccessor = this;
    }
  }

  readonly variant = input<'identifier' | 'password' | 'code' | 'tenantCode'>('identifier');
  readonly size = input<'md' | 'lg'>('lg');
  readonly label = input.required<string>();
  readonly placeholder = input<string | null>(null);
  readonly autocomplete = input.required<string>();
  readonly errorMessage = input<string | null>(null);
  readonly hint = input<string | null>(null);
  readonly controlId = input<string>(`auth-field-${++dem}`);
  readonly revealable = input(true);

  protected readonly icon = computed<string | null>(() => {
    switch (this.variant()) {
      case 'identifier':
        return 'user';
      case 'password':
        return 'key';
      case 'tenantCode':
        return 'building';
      default:
        return null;
    }
  });

  protected readonly idLoi = computed(() => `${this.controlId()}-error`);
  protected readonly idGoiY = computed(() => `${this.controlId()}-hint`);
  protected readonly describedBy = computed(() => {
    if (this.errorMessage()) return this.idLoi();
    if (this.hint()) return this.idGoiY();
    return null;
  });

  protected readonly gia = signal('');
  protected readonly voHieuHoa = signal(false);
  protected readonly hienMatKhau = signal(false);

  private readonly oNhap = viewChild<ElementRef<HTMLInputElement>>('oNhap');

  /** Trang xác thực gọi để đặt focus ban đầu vào ô nhập đầu tiên còn trống (AuthCard.md §Trạng thái). */
  focus(): void {
    this.oNhap()?.nativeElement.focus();
  }

  protected get kieuThat(): 'text' | 'password' {
    return this.variant() === 'password' && !this.hienMatKhau() ? 'password' : 'text';
  }

  protected doiGia(gt: string): void {
    this.gia.set(gt);
    this.onChange(gt);
  }

  protected chamVao(): void {
    this.onTouched();
  }

  protected toggleHienMatKhau(): void {
    this.hienMatKhau.update((v) => !v);
  }

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
