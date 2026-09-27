import { DatePipe, registerLocaleData } from '@angular/common';
import localeFr from '@angular/common/locales/fr';
import localeVi from '@angular/common/locales/vi';
import { LOCALE_ID, provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';

import { CORE_I18N, provideCoreI18n } from './core-i18n';

/**
 * Luật F36 / ADR-0063 — `LOCALE_ID` và dữ liệu locale của pipe đến từ CHÍNH seam `CORE_I18N`.
 * Trước đó ba chỗ trong `app.config.ts` cùng nói "vi" mà không chỗ nào biết chỗ kia: đổi
 * `defaultLanguage` thì câu chữ đổi ngôn ngữ còn ngày tháng ở lại — pipe không báo lỗi.
 *
 * `registerLocaleData` được import ở đây CHỈ để chứng minh phép thử có nghĩa (xem ca cuối);
 * tệp này là `*.spec.ts` nên nằm ngoài tầm quét của cổng F36.
 */
describe('provideCoreI18n — LOCALE_ID và dữ liệu locale', () => {
  it('cấp LOCALE_ID bằng ĐÚNG defaultLanguage của seam, không phải một giá trị viết cứng', () => {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideCoreI18n({
          languages: [
            { code: 'vi', nativeName: 'Tiếng Việt', localeData: localeVi },
            { code: 'fr', nativeName: 'Français', localeData: localeFr },
          ],
          defaultLanguage: 'fr',
          sources: ['/i18n/'],
        }),
      ],
    });

    expect(TestBed.inject(LOCALE_ID)).toBe('fr');
    expect(TestBed.inject(CORE_I18N).defaultLanguage).toBe('fr');
  });

  it('đăng ký dữ liệu locale cho MỌI ngôn ngữ khai trong seam, không riêng ngôn ngữ mặc định', () => {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideCoreI18n({
          languages: [
            { code: 'vi', nativeName: 'Tiếng Việt', localeData: localeVi },
            { code: 'fr', nativeName: 'Français', localeData: localeFr },
          ],
          defaultLanguage: 'vi',
          sources: ['/i18n/'],
        }),
      ],
    });

    // Locale chưa đăng ký thì DatePipe NÉM lỗi — nên hai dòng này chứng minh cả hai đã đăng ký.
    expect(new DatePipe('vi').transform('2026-09-10T03:12:44Z', 'shortDate', 'UTC')).toBe(
      '10/09/2026',
    );
    expect(new DatePipe('fr').transform('2026-09-10T03:12:44Z', 'shortDate', 'UTC')).toBe(
      '10/09/2026',
    );
  });

  it('phép thử có nghĩa: một locale KHÔNG được khai trong seam thì DatePipe ném lỗi', () => {
    // Chốt chống "xanh rỗng" — nếu DatePipe im lặng chấp nhận mọi locale thì ca trên vô giá trị.
    expect(() =>
      new DatePipe('sv').transform('2026-09-10T03:12:44Z', 'shortDate', 'UTC'),
    ).toThrow();
    // ...và đăng ký thủ công thì hết ném: đúng cơ chế mà provideCoreI18n gánh hộ composition root.
    registerLocaleData(localeFr, 'sv-canary');
    expect(() =>
      new DatePipe('sv-canary').transform('2026-09-10T03:12:44Z', 'shortDate', 'UTC'),
    ).not.toThrow();
  });
});
