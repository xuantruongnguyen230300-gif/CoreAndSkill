import { provideHttpClient, withInterceptors } from '@angular/common/http';
import localeVi from '@angular/common/locales/vi';
import {
  ApplicationConfig,
  EnvironmentProviders,
  provideAppInitializer,
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
import { CoreI18n, provideCoreI18n } from './core/config/core-i18n';
import { provideCoreRoutes } from './core/config/core-routes';
import { API_BASE_URL } from './core/http/api-base-url';
import { CoreTitleStrategy } from './core/i18n/core-title.strategy';
import { provideDongBoNgonNgu } from './core/i18n/dong-bo-ngon-ngu';
import { MultiSourceTranslateLoader } from './core/i18n/multi-source-translate-loader';
import { authInterceptor } from './core/interceptors/auth.interceptor';
import { errorInterceptor } from './core/interceptors/error.interceptor';
import { loadingInterceptor } from './core/interceptors/loading.interceptor';
import { LoiTaiPhanMaService } from './core/navigation/loi-tai-phan-ma';
import { provideClientErrorOutput } from './core/observability/client-error-output';
import { HttpClientErrorOutput } from './core/observability/http-client-error-output';
import { provideCoreObservability } from './core/observability/provide-core-observability';
import { CORE_PRIME_PRESET } from './core/theme/prime-preset';
import { CORE_PRIME_Z_INDEX } from './core/theme/prime-z-index';
import { TENANT_CODE } from './core/config/tenant-code';
import { environment } from '../environments/environment';

/**
 * Cấu hình ngôn ngữ của app — tách khỏi `appConfig` để test dựng lại đúng bộ giá trị này mà không
 * phải nạp cả `appConfig`. `dateTimeFormat` của `vi` do người dùng chốt 2026-11-06
 * (fe-ui-conventions.md §5.5).
 */
export const APP_I18N: CoreI18n = {
  languages: [
    {
      code: 'vi',
      nativeName: 'Tiếng Việt',
      dateTimeFormat: 'HH:mm dd/MM/y',
      localeData: localeVi,
    },
  ],
  defaultLanguage: 'vi',
  sources: ['/i18n/'],
};

/**
 * Đầu ra của bộ thu lỗi runtime theo môi trường (10-observability.md §4): bản production gửi báo cáo
 * về BE; dev giữ đầu ra mặc định là console. Tách thành hàm nhận cờ để test được cả hai nhánh mà
 * không phải chạy `appConfig` — `environment.production` của `ng test` luôn là `true`.
 */
export function provideDauRaLoiTheoMoiTruong(production: boolean): EnvironmentProviders[] {
  return production ? [provideClientErrorOutput(HttpClientErrorOutput)] : [];
}

export const appConfig: ApplicationConfig = {
  providers: [
    // Bộ thu lỗi runtime: ErrorHandler tuỳ biến, hai sự kiện lỗi của cửa sổ (qua
    // provideBrowserGlobalErrorListeners() bên trong), và tài nguyên tải hỏng. Đầu ra mặc định là
    // console; bản production cắm HttpClientErrorOutput ở dòng ngay dưới, đích khác cắm bằng
    // provideClientErrorOutput(…) (wiki-core/fe/10-observability.md §3, §4).
    provideCoreObservability(),
    ...provideDauRaLoiTheoMoiTruong(environment.production),
    provideZonelessChangeDetection(),
    provideRouter(routes),
    // Nghe Router.events từ lúc khởi động để bắt lần điều hướng hỏng vì không tải được phần mã
    // (quy-uoc/fe-routing-guard.md §8.1). Phải sống TRƯỚC lần điều hướng đầu tiên.
    provideAppInitializer(() => {
      inject(LoiTaiPhanMaService);
    }),

    // Chuỗi interceptor FE — định nghĩa gốc: fe-api-client.md §2. Đúng thứ tự này, không thêm cái thứ tư.
    provideHttpClient(withInterceptors([authInterceptor, loadingInterceptor, errorInterceptor])),
    { provide: API_BASE_URL, useValue: environment.apiBaseUrl },
    { provide: TENANT_CODE, useValue: environment.tenantCode },

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
    provideCoreI18n(APP_I18N),
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
      // Thang lớp chồng duy nhất là `--z-*`; thư viện chỉ nhận số — xem core/theme/prime-z-index.ts.
      zIndex: CORE_PRIME_Z_INDEX,
    }),
  ],
};
