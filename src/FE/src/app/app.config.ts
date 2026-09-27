import { provideHttpClient, withInterceptors } from '@angular/common/http';
import localeVi from '@angular/common/locales/vi';
import {
  ApplicationConfig,
  provideAppInitializer,
  provideBrowserGlobalErrorListeners,
  provideZonelessChangeDetection,
  inject,
} from '@angular/core';
import { provideRouter, TitleStrategy } from '@angular/router';
import { provideTranslateLoader, provideTranslateService } from '@ngx-translate/core';
import { providePrimeNG } from 'primeng/config';
import { catchError, forkJoin, of } from 'rxjs';

import { routes } from './app.routes';
import { AuthService } from './core/auth/auth.service';
import { XsrfTokenStore } from './core/auth/xsrf-token.store';
import { provideCoreBranding } from './core/config/core-branding';
import { provideCoreI18n } from './core/config/core-i18n';
import { provideCoreRoutes } from './core/config/core-routes';
import { API_BASE_URL } from './core/http/api-base-url';
import { CoreTitleStrategy } from './core/i18n/core-title.strategy';
import { provideDongBoNgonNgu } from './core/i18n/dong-bo-ngon-ngu';
import { MultiSourceTranslateLoader } from './core/i18n/multi-source-translate-loader';
import { authInterceptor } from './core/interceptors/auth.interceptor';
import { errorInterceptor } from './core/interceptors/error.interceptor';
import { loadingInterceptor } from './core/interceptors/loading.interceptor';
import { provideCoreAnimations } from './core/theme/core-animations';
import { CORE_PRIME_PRESET } from './core/theme/prime-preset';
import { environment } from '../environments/environment';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZonelessChangeDetection(),
    provideRouter(routes),

    // Chuỗi interceptor FE — định nghĩa gốc: fe-api-client.md §2. Đúng thứ tự này, không thêm cái thứ tư.
    provideHttpClient(withInterceptors([authInterceptor, loadingInterceptor, errorInterceptor])),
    { provide: API_BASE_URL, useValue: environment.apiBaseUrl },

    // Seam — đường dẫn thật của dự án này (guard thật từ F2, quy-uoc/fe-routing-guard.md §6).
    provideCoreRoutes({
      dangNhap: '/dang-nhap',
      doiMatKhauBatBuoc: '/doi-mat-khau-bat-buoc',
      khongCoQuyen: '/khong-co-quyen',
      sauDangNhap: '/trang-chu',
    }),

    // Tên sản phẩm — hậu tố <title> (CoreTitleStrategy) và vùng thương hiệu của AuthCard
    // (fe-architecture.md §2.5, Design/Screens/01-dang-nhap.md).
    provideCoreBranding({ name: 'CoreAndSkill', shortName: 'CS' }),
    { provide: TitleStrategy, useClass: CoreTitleStrategy },

    // i18n — F0 chỉ có tầng Core (public/i18n/); tầng dự án và đổi ngôn ngữ lúc chạy thuộc F2+.
    // Mục ngôn ngữ mang theo dữ liệu locale của nó: seam vừa cấp mã locale của app vừa đăng ký
    // dữ liệu locale cho pipe, nên tệp này KHÔNG làm lại hai việc đó (luật F36, ADR-0063 — cổng
    // F36 của scripts/fe-gate.sh đỏ nếu hai tên đó xuất hiện ngoài core/config/core-i18n.ts).
    provideCoreI18n({
      languages: [{ code: 'vi', nativeName: 'Tiếng Việt', localeData: localeVi }],
      defaultLanguage: 'vi',
      sources: ['/i18n/'],
    }),
    // Không khai `lang`/`fallbackLang` ở đây: ngôn ngữ ban đầu và dự phòng đến từ
    // `CORE_I18N.defaultLanguage` ngay trên, do `provideDongBoNgonNgu` đặt — cùng chỗ gán `<html lang>`
    // và nạp bộ chuỗi của PrimeNG (wiki-core/fe/08-i18n.md §7, §8).
    provideTranslateService({ loader: provideTranslateLoader(MultiSourceTranslateLoader) }),
    provideDongBoNgonNgu(),

    // Khởi động phiên — quy-uoc/fe-routing-guard.md §3.6: hai GET song song, app render sau khi
    // CẢ HAI về. Lỗi trong TỪNG nhánh không được chặn app render — catchError ở ĐÂY, không
    // trong AuthService/XsrfTokenStore (mẫu gốc).
    provideAppInitializer(() => {
      const xsrf = inject(XsrfTokenStore);
      const auth = inject(AuthService);
      return forkJoin([
        xsrf.lamMoi().pipe(catchError(() => of(null))),
        auth.napPhienKhoiDong().pipe(catchError(() => of(null))),
      ]);
    }),

    // Hoạt ảnh — hàm của Core hỏi cài đặt giảm chuyển động của hệ điều hành MỘT lần lúc khởi động
    // rồi chọn bộ chạy. Tệp này KHÔNG khai provider hoạt ảnh nào khác (luật F39, ADR-0080): thay
    // dòng này bằng provider hoạt ảnh trần của Angular là mất nửa provider — khối CSS toàn cục
    // không với tới hoạt ảnh Angular. Lý do đầy đủ ở core/theme/core-animations.ts.
    provideCoreAnimations(),

    // PrimeNG — preset của Core (core/theme/prime-preset.ts) trỏ toàn bộ tầng semantic vào
    // var(--color-*), không giữ mã màu nào (F1, wiki-core/fe/04-design-token-system.md §7).
    providePrimeNG({
      theme: {
        preset: CORE_PRIME_PRESET,
        // darkModeSelector: false — biến --color-* đã đổi theo data-theme; PrimeNG không cần
        // cơ chế tối thứ hai. cssLayer: true — style của thư viện vào lớp 'primeng'; CSS của app
        // KHÔNG khai lớp nào nên luôn thắng, không cần !important (xác nhận từ tài liệu PrimeNG
        // chính thức lúc thi công — 04-design-token-system.md §7 cảnh báo cú pháp chưa xác minh).
        options: { darkModeSelector: false, cssLayer: true },
      },
    }),
  ],
};
