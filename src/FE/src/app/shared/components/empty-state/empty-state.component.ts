import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { RouterLink } from '@angular/router';

import { ButtonComponent } from '../button/button.component';

type EmptyStateVariant =
  | 'first-use'
  | 'no-results'
  | 'error'
  | 'no-permission'
  | 'not-configured'
  | 'not-found'
  | 'record-not-found';

const ICON_MAC_DINH: Readonly<Record<EmptyStateVariant, string>> = {
  'first-use': 'inbox',
  'no-results': 'search',
  error: 'times-circle',
  'no-permission': 'lock',
  'not-configured': 'cog',
  'not-found': 'compass',
  'record-not-found': 'compass',
};

/**
 * Design/Components/EmptyState.md. Tự dựng — component trình bày thuần, biến thể chính là câu chữ
 * (§Nền). Component dumb: nhận biến thể qua `input()`, không tự suy đoán vì sao trống (COMPONENTS.md §5).
 */
@Component({
  selector: 'app-empty-state',
  standalone: true,
  imports: [RouterLink, ButtonComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './empty-state.component.html',
  styleUrl: './empty-state.component.scss',
})
export class EmptyStateComponent {
  readonly variant = input.required<EmptyStateVariant>();
  readonly size = input<'compact' | 'default' | 'page'>('default');
  readonly title = input.required<string>();
  readonly description = input<string | null>(null);
  readonly icon = input<string | null>(null);
  readonly actionLabel = input<string | null>(null);
  readonly actionRoute = input<string | null>(null);
  readonly headingLevel = input<2 | 3 | 4>(3);

  readonly actionClicked = output<void>();

  protected readonly iconThat = computed(() => this.icon() ?? ICON_MAC_DINH[this.variant()]);
  protected readonly bienTheNut = computed<'primary' | 'secondary'>(() =>
    this.variant() === 'first-use' || this.variant() === 'not-configured' ? 'primary' : 'secondary',
  );

  protected onAction(): void {
    if (this.actionRoute() === null) {
      this.actionClicked.emit();
    }
  }
}
