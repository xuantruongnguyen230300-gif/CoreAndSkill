import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/** Chữ ký FooterLink — fe-ui-conventions.md §9. */
export interface FooterLink {
  readonly label: string;
  readonly href: string;
  readonly external?: boolean;
}

/**
 * Design/Components/Footer.md. Nội dung TRANG, không phải khung — cuộn cùng trang, không dính đáy.
 * Component dumb: không tự đọc phiên bản từ service cấu hình.
 */
@Component({
  selector: 'app-footer',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './footer.component.html',
  styleUrl: './footer.component.scss',
})
export class FooterComponent {
  readonly variant = input<'full' | 'compact' | 'auth'>('full');
  readonly size = input<'sm' | 'md'>('md');
  readonly version = input<string | null>(null);
  readonly copyright = input<string | null>(null);
  readonly links = input<readonly FooterLink[]>([]);
  readonly showDivider = input(true);
}
