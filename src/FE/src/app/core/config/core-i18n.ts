import { registerLocaleData } from '@angular/common';
import {
  EnvironmentProviders,
  InjectionToken,
  LOCALE_ID,
  makeEnvironmentProviders,
} from '@angular/core';

/** Khai ở core vì CORE_I18N cấp nó; code: mã BCP 47, nativeName: tên viết bằng CHÍNH ngôn ngữ đó. */
export interface LanguageOption {
  readonly code: string;
  readonly nativeName: string;
  /**
   * Dữ liệu locale của chính mã trên — `import localeXx from '@angular/common/locales/<mã>'` ở
   * composition root. Không nạp được từ một chuỗi lúc chạy, nên mục ngôn ngữ mang nó theo
   * (ADR-0063, ADR-0057).
   */
  readonly localeData: unknown;
}

export interface CoreI18n {
  /** Một mục ⇒ bộ chọn ngôn ngữ không render (F2). */
  readonly languages: readonly LanguageOption[];
  /** MÃ — phải là `code` của một mục trong `languages`. */
  readonly defaultLanguage: string;
  /** Đường dẫn thư mục tệp dịch, TẦNG SAU ghi đè tầng trước. F0 chỉ có tầng Core. */
  readonly sources: readonly string[];
}

export const CORE_I18N = new InjectionToken<CoreI18n>('CORE_I18N');

/**
 * Seam ngôn ngữ — luật F36, ADR-0063: `LOCALE_ID` và dữ liệu locale của pipe đến từ ĐÂY, không cấp
 * riêng ở `app.config.ts`. Ba chỗ cùng nói một mã ngôn ngữ mà không chỗ nào biết chỗ kia thì dự án
 * hạ nguồn đổi `defaultLanguage` sẽ có câu chữ một ngôn ngữ còn ngày tháng ngôn ngữ khác — pipe
 * không báo lỗi (08-i18n.md §6.2). Thêm một ngôn ngữ = thêm một mục ở đây, không sửa gì khác.
 */
export function provideCoreI18n(i18n: CoreI18n): EnvironmentProviders {
  // Angular chỉ đóng gói sẵn locale en-US: không đăng ký thì pipe date/number im lặng rơi về khuôn
  // Anh-Mỹ. Đăng ký cho MỌI ngôn ngữ đã khai, không riêng ngôn ngữ mặc định — bộ chọn ngôn ngữ
  // (F2) đổi được sang bất kỳ mục nào trong danh sách.
  for (const ngonNgu of i18n.languages) {
    registerLocaleData(ngonNgu.localeData, ngonNgu.code);
  }
  return makeEnvironmentProviders([
    { provide: CORE_I18N, useValue: i18n },
    { provide: LOCALE_ID, useValue: i18n.defaultLanguage },
  ]);
}
