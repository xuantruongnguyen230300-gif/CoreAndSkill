import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

/**
 * Design/Components/Button.md. Tự dựng, không bọc PrimeNG (§Nền). Component **dumb**: không
 * inject service lấy dữ liệu, không biết route.
 */
@Component({
  selector: 'app-button',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './button.component.html',
  styleUrl: './button.component.scss',
})
export class ButtonComponent {
  /** Mặc định `secondary` CÓ CHỦ ĐÍCH — quên khai biến thể ra nút phụ, không ra nút chính. */
  readonly variant = input<'primary' | 'secondary' | 'ghost' | 'danger'>('secondary');
  readonly size = input<'sm' | 'md' | 'lg'>('md');
  readonly block = input(false);
  readonly icon = input<string | null>(null);
  readonly iconPosition = input<'leading' | 'trailing'>('leading');
  readonly type = input<'button' | 'submit' | 'reset'>('button');
  readonly disabled = input(false);
  readonly loading = input(false);
  readonly ariaLabel = input<string | null>(null);

  /** KHÔNG phát khi đang `disabled` hoặc `loading` — chặn ở component, không bắt nơi gọi tự nhớ. */
  readonly clicked = output<void>();

  protected onClick(): void {
    if (this.disabled() || this.loading()) {
      return;
    }
    this.clicked.emit();
  }
}
