import {
  DATE_PIPE_DEFAULT_OPTIONS,
  DATE_PIPE_DEFAULT_TIMEZONE,
  DatePipe,
  registerLocaleData,
} from '@angular/common';
import localeFr from '@angular/common/locales/fr';
import localeVi from '@angular/common/locales/vi';
import { Component, LOCALE_ID, provideZonelessChangeDetection } from '@angular/core';
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
            {
              code: 'vi',
              nativeName: 'Tiếng Việt',
              dateTimeFormat: 'HH:mm dd/MM/y',
              localeData: localeVi,
            },
            {
              code: 'fr',
              nativeName: 'Français',
              dateTimeFormat: 'dd/MM/y HH:mm',
              localeData: localeFr,
            },
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
            {
              code: 'vi',
              nativeName: 'Tiếng Việt',
              dateTimeFormat: 'HH:mm dd/MM/y',
              localeData: localeVi,
            },
            {
              code: 'fr',
              nativeName: 'Français',
              dateTimeFormat: 'dd/MM/y HH:mm',
              localeData: localeFr,
            },
          ],
          defaultLanguage: 'vi',
          sources: ['/i18n/'],
        }),
      ],
    });

    // Locale chưa đăng ký thì DatePipe NÉM lỗi — nên hai dòng này chứng minh cả hai đã đăng ký.
    // `vi` theo CLDR của Angular 21 là `d/M/yy`; `fr` là `dd/MM/y` — khác nhau, nên cũng chứng minh
    // hai dữ liệu locale không bị lẫn vào nhau.
    expect(new DatePipe('vi').transform('2026-09-10T03:12:44Z', 'shortDate', 'UTC')).toBe(
      '10/9/26',
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

/**
 * Khuôn ngày giờ — luật F36, fe-ui-conventions.md §5.5: pipe `date` không nhận đối số ở template;
 * khuôn đến từ `LanguageOption.dateTimeFormat` của ngôn ngữ mặc định qua `DATE_PIPE_DEFAULT_OPTIONS`.
 */
describe('provideCoreI18n — khuôn ngày giờ', () => {
  const NGON_NGU = [
    {
      code: 'vi',
      nativeName: 'Tiếng Việt',
      dateTimeFormat: 'HH:mm dd/MM/y',
      localeData: localeVi,
    },
    {
      code: 'fr',
      nativeName: 'Français',
      dateTimeFormat: 'dd/MM/y HH:mm',
      localeData: localeFr,
    },
  ];

  @Component({
    selector: 'app-chu-ngay-gio',
    imports: [DatePipe],
    template: `{{ thoiDiem | date }}`,
  })
  class ChuNgayGioComponent {
    readonly thoiDiem = '2026-09-10T03:12:44Z';
  }

  /** Múi giờ UTC CHỈ trong test để kết quả không phụ thuộc máy chạy; mã sản xuất không cấp nó. */
  function renderVoiMacDinh(defaultLanguage: string): string {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideCoreI18n({ languages: NGON_NGU, defaultLanguage, sources: ['/i18n/'] }),
        { provide: DATE_PIPE_DEFAULT_TIMEZONE, useValue: 'UTC' },
      ],
    });
    const fixture = TestBed.createComponent(ChuNgayGioComponent);
    fixture.detectChanges();
    return (fixture.nativeElement as HTMLElement).textContent.trim();
  }

  it('cấp DATE_PIPE_DEFAULT_OPTIONS chỉ có dateFormat của ngôn ngữ mặc định — không có timezone', () => {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideCoreI18n({ languages: NGON_NGU, defaultLanguage: 'fr', sources: ['/i18n/'] }),
      ],
    });

    expect(TestBed.inject(DATE_PIPE_DEFAULT_OPTIONS)).toEqual({ dateFormat: 'dd/MM/y HH:mm' });
  });

  it('`{{ t | date }}` không đối số ra khuôn của vi khi mặc định là vi', () => {
    expect(renderVoiMacDinh('vi')).toBe('03:12 10/09/2026');
  });

  it('`{{ t | date }}` không đối số ra khuôn của fr khi mặc định là fr', () => {
    expect(renderVoiMacDinh('fr')).toBe('10/09/2026 03:12');
  });

  it('phép thử có nghĩa: không có khuôn từ seam thì pipe ra khuôn mặc định của locale, khác hẳn', () => {
    // Chốt chống "xanh rỗng" — nếu pipe bỏ qua DATE_PIPE_DEFAULT_OPTIONS thì hai ca trên đã xanh
    // vì lý do khác; ở đây chứng minh khuôn rơi về 'mediumDate' của locale là chuỗi KHÁC.
    expect(new DatePipe('vi').transform('2026-09-10T03:12:44Z', undefined, 'UTC')).toBe(
      '10 thg 9, 2026',
    );
  });

  it('defaultLanguage không có mục nào trong languages thì ném lỗi nêu mã đó', () => {
    expect(() =>
      provideCoreI18n({ languages: NGON_NGU, defaultLanguage: 'de', sources: ['/i18n/'] }),
    ).toThrowError(/'de'/);
  });
});
