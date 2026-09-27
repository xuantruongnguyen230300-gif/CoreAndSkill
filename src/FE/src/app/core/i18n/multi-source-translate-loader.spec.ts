import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';

import { CORE_I18N } from '../config/core-i18n';
import { MultiSourceTranslateLoader } from './multi-source-translate-loader';

describe('MultiSourceTranslateLoader', () => {
  let httpMock: HttpTestingController;

  function dungVoi(sources: readonly string[]): MultiSourceTranslateLoader {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(),
        provideHttpClientTesting(),
        MultiSourceTranslateLoader,
        { provide: CORE_I18N, useValue: { languages: [], defaultLanguage: 'vi', sources } },
      ],
    });
    httpMock = TestBed.inject(HttpTestingController);
    return TestBed.inject(MultiSourceTranslateLoader);
  }

  afterEach(() => httpMock.verify());

  it('không có nguồn nào → trả object rỗng, không gọi HTTP', () => {
    const loader = dungVoi([]);
    let ketQua: unknown;
    loader.getTranslation('vi').subscribe((t) => (ketQua = t));
    expect(ketQua).toEqual({});
  });

  it('một nguồn → tải đúng đường dẫn <source><lang>.json', () => {
    const loader = dungVoi(['/i18n/']);
    let ketQua: unknown;
    loader.getTranslation('vi').subscribe((t) => (ketQua = t));

    httpMock.expectOne('/i18n/vi.json').flush({ chung: { luu: 'Lưu' } });

    expect(ketQua).toEqual({ chung: { luu: 'Lưu' } });
  });

  it('nhiều nguồn → TẦNG SAU ghi đè khoá trùng của tầng trước, khoá không trùng thì gộp', () => {
    const loader = dungVoi(['/i18n/', '/i18n-app/']);
    let ketQua: unknown;
    loader.getTranslation('vi').subscribe((t) => (ketQua = t));

    httpMock
      .expectOne('/i18n/vi.json')
      .flush({ chung: { luu: 'Lưu (Core)' }, core: { rieng: 'A' } });
    httpMock.expectOne('/i18n-app/vi.json').flush({ chung: { luu: 'Lưu (dự án)' } });

    expect(ketQua).toEqual({ chung: { luu: 'Lưu (dự án)' }, core: { rieng: 'A' } });
  });
});
