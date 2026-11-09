import { ChangeDetectionStrategy, Component, ViewEncapsulation, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import { NoticeBannerComponent } from '../notice-banner/notice-banner.component';

/**
 * Design/Components/AuthCard.md. Khung của MÀN XÁC THỰC — không `Sidebar`, không `Topbar`
 * (§Khi nào dùng). Component dumb: không gọi API đăng nhập, không đọc token, không điều hướng.
 *
 * Đơn giản hoá có ghi nhận: bỏ qua biến thể `wide`/`LanguageSwitcher` — `CORE_I18N.languages` v1
 * chỉ có một mục nên `LanguageSwitcher` không render (đúng theo spec, không phải thiếu sót).
 *
 * `ViewEncapsulation.None` theo fe-ui-conventions.md §4.2 luật 4 (luật F55): khe trong form
 * (§Kích thước) là của KHUNG, nhưng `<form>` và các trường nằm trong nội dung chiếu `<ng-content>`
 * — style đóng gói theo component không chạm tới chúng, còn `::ng-deep` bị cấm. Style chạm phần tử
 * chiếu: `<form>` con trực tiếp của `.auth-card` (khe giữa các trường) và `app-button` con trực tiếp
 * của `<form>` đó (khoảng cách trên nút chính). Toàn bộ SCSS bọc trong khối `app-auth-card { … }`
 * nên style chỉ có hiệu lực trong cây DOM của khung.
 */
@Component({
  selector: 'app-auth-card',
  standalone: true,
  imports: [NoticeBannerComponent, TranslatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './auth-card.component.html',
  styleUrl: './auth-card.component.scss',
  encapsulation: ViewEncapsulation.None,
})
export class AuthCardComponent {
  readonly title = input.required<string>();
  readonly description = input<string | null>(null);
  readonly errorMessage = input<string | null>(null);
  /**
   * Vùng thương hiệu (Screens/01-dang-nhap.md): tên sản phẩm dạng chữ. Trang (smart) đọc seam
   * `CORE_BRANDING` rồi truyền xuống — component dumb không tự inject token (luật F11).
   */
  readonly brandName = input<string | null>(null);
}
