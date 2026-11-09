import { NgTemplateOutlet } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  computed,
  inject,
  input,
  output,
} from '@angular/core';

/**
 * Design/Components/Button.md. Tự dựng, không bọc PrimeNG (§Nền). Component **dumb**: không
 * inject service lấy dữ liệu, không biết route.
 *
 * Hai host trên CÙNG một component (Button.md §Liên kết mang hình thức nút): `<app-button>` dựng
 * `<button>`; `<a app-button routerLink>` dùng chính thẻ `<a>` của nơi dùng làm nút — không lồng
 * `<button>` trong `<a>`. Ở host `<a>`, `type`, `loading`, `clicked` không có nghĩa và bị bỏ qua.
 */
@Component({
  selector: 'app-button, a[app-button]',
  host: {
    '[class]': 'lopHost()',
    '[attr.aria-disabled]': 'laLienKet && disabled() ? "true" : null',
    '[attr.aria-label]': 'laLienKet ? ariaLabel() : null',
    '[attr.tabindex]': 'laLienKet && disabled() ? -1 : null',
    '(click)': 'chanKhiVoHieu($event)',
  },
  standalone: true,
  imports: [NgTemplateOutlet],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './button.component.html',
  styleUrl: './button.component.scss',
})
export class ButtonComponent {
  protected readonly laLienKet =
    inject<ElementRef<HTMLElement>>(ElementRef).nativeElement.tagName === 'A';

  /** Mặc định `secondary` CÓ CHỦ ĐÍCH — quên khai biến thể ra nút phụ, không ra nút chính. */
  readonly variant = input<'primary' | 'secondary' | 'ghost' | 'danger'>('secondary');
  readonly size = input<'sm' | 'md' | 'lg'>('md');
  readonly block = input(false);
  readonly icon = input<string | null>(null);
  readonly iconPosition = input<'leading' | 'trailing'>('leading');
  readonly type = input<'button' | 'submit' | 'reset'>('button');
  /** `id` của `<form>` mà nút `submit` thuộc về khi nút đứng NGOÀI thẻ form (vd. chân của `Drawer`). */
  readonly form = input<string | null>(null);
  readonly disabled = input(false);
  readonly loading = input(false);
  readonly ariaLabel = input<string | null>(null);
  /** Nút mở lớp nổi (vd. nút lọc của Toolbar): sinh `aria-haspopup`; `null` không sinh. Chỉ host `<button>`. */
  readonly haspopup = input<'dialog' | 'menu' | null>(null);
  /** Chỉ có nghĩa khi `haspopup` khác `null`; `null` không sinh `aria-expanded`. */
  readonly expanded = input<boolean | null>(null);
  /** `id` của lớp nổi nút điều khiển → `aria-controls`; `null` không sinh. */
  readonly controls = input<string | null>(null);

  /** Lớp hình thức đặt lên host — CHỈ khi host là `<a>`; host `<app-button>` không mang lớp nào. */
  protected readonly lopHost = computed(() =>
    this.laLienKet
      ? `nut nut--${this.variant()} nut--${this.size()}${this.block() ? ' nut--block' : ''}`
      : '',
  );

  /** KHÔNG phát khi đang `disabled` hoặc `loading` — chặn ở component, không bắt nơi gọi tự nhớ. */
  readonly clicked = output<void>();

  protected onClick(): void {
    if (this.disabled() || this.loading()) {
      return;
    }
    this.clicked.emit();
  }

  /** Host `<a>` đang `disabled`: chặn điều hướng (nơi dùng còn đặt `[routerLink]` = `null`). */
  protected chanKhiVoHieu(su: Event): void {
    if (this.laLienKet && this.disabled()) {
      su.preventDefault();
      su.stopImmediatePropagation();
    }
  }
}
