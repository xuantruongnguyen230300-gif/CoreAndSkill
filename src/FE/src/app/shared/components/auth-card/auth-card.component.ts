import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/**
 * Design/Components/AuthCard.md. Khung của MÀN XÁC THỰC — không `Sidebar`, không `Topbar`
 * (§Khi nào dùng). Component dumb: không gọi API đăng nhập, không đọc token, không điều hướng.
 *
 * Đơn giản hoá có ghi nhận: bỏ qua biến thể `wide`/`LanguageSwitcher` — `CORE_I18N.languages` v1
 * chỉ có một mục nên `LanguageSwitcher` không render (đúng theo spec, không phải thiếu sót).
 */
@Component({
  selector: 'app-auth-card',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './auth-card.component.html',
  styleUrl: './auth-card.component.scss',
})
export class AuthCardComponent {
  readonly title = input.required<string>();
  readonly description = input<string | null>(null);
  readonly errorMessage = input<string | null>(null);
  readonly loading = input(false);
}
