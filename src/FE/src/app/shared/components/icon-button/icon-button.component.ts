import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

/**
 * Design/Components/IconButton.md. Tự dựng, không bọc PrimeNG (§Nền) — một `<button>` vuông chứa
 * một icon. Component dumb (COMPONENTS.md §5).
 */
@Component({
  selector: 'app-icon-button',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './icon-button.component.html',
  styleUrl: './icon-button.component.scss',
})
export class IconButtonComponent {
  readonly icon = input.required<string>();
  readonly ariaLabel = input.required<string>();
  readonly variant = input<'ghost' | 'secondary' | 'primary' | 'danger'>('ghost');
  readonly size = input<'sm' | 'md' | 'lg'>('md');
  readonly type = input<'button' | 'submit' | 'reset'>('button');
  readonly disabled = input(false);
  readonly loading = input(false);
  readonly pressed = input<boolean | null>(null);

  readonly clicked = output<void>();

  protected onClick(): void {
    if (this.disabled() || this.loading()) {
      return;
    }
    this.clicked.emit();
  }
}
