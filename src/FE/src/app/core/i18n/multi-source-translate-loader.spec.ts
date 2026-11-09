import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ErrorHandler, provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';

import { CORE_I18N } from '../config/core-i18n';
import { LoiNapTepDichError, MultiSourceTranslateLoader } from './multi-source-translate-loader';

describe('MultiSourceTranslateLoader', () => {
  let httpMock: HttpTestingController;
  let daBao: unknown[];

  function dungVoi(sources: readonly string[]): MultiSourceTranslateLoader {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(),
        provideHttpClientTesting(),
        MultiSourceTranslateLoader,
        { provide: ErrorHandler, useValue: { handleError: (e: unknown) => daBao.push(e) } },
        { provide: CORE_I18N, useValue: { languages: [], defaultLanguage: 'vi', sources } },
      ],
    });
    httpMock = TestBed.inject(HttpTestingController);
    return TestBed.inject(MultiSourceTranslateLoader);
  }

  beforeEach(() => (daBao = []));
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

  // 08-i18n.md §2.3: thiếu tệp thì khởi động hỏng ồn ào. Tầng Core và tầng dự án đối xử như nhau.
  describe('thiếu tệp dịch', () => {
    function layLoi(loader: MultiSourceTranslateLoader): { loi?: unknown; ketQua?: unknown } {
      const out: { loi?: unknown; ketQua?: unknown } = {};
      loader.getTranslation('vi').subscribe({
        next: (t) => (out.ketQua = t),
        error: (e: unknown) => (out.loi = e),
      });
      return out;
    }

    it('thiếu tệp tầng Core → lỗi nêu đúng tệp, báo qua ErrorHandler, không có bản dịch', () => {
      const loader = dungVoi(['/i18n/', '/i18n-app/']);
      const out = layLoi(loader);

      httpMock.expectOne('/i18n/vi.json').flush('x', { status: 404, statusText: 'Not Found' });
      // forkJoin bỏ yêu cầu còn lại ngay khi một tệp hỏng: không trả bản dịch thiếu tầng.
      expect(httpMock.expectOne('/i18n-app/vi.json').cancelled).toBeTrue();

      expect(out.loi).toBeInstanceOf(LoiNapTepDichError);
      expect((out.loi as LoiNapTepDichError).url).toBe('/i18n/vi.json');
      expect((out.loi as LoiNapTepDichError).message).toContain('/i18n/vi.json');
      expect((out.loi as LoiNapTepDichError).cause).toBeDefined();
      expect(out.ketQua).toBeUndefined();
      expect(daBao).toEqual([out.loi]);
    });

    it('thiếu tệp tầng dự án (Core đủ) → vẫn hỏng, không trả bản dịch chỉ có Core', () => {
      const loader = dungVoi(['/i18n/', '/i18n-app/']);
      const out = layLoi(loader);

      httpMock.expectOne('/i18n/vi.json').flush({ chung: { luu: 'Lưu' } });
      httpMock.expectOne('/i18n-app/vi.json').flush('x', { status: 404, statusText: 'Not Found' });

      expect(out.loi).toBeInstanceOf(LoiNapTepDichError);
      expect((out.loi as LoiNapTepDichError).url).toBe('/i18n-app/vi.json');
      expect(out.ketQua).toBeUndefined();
      expect(daBao).toEqual([out.loi]);
    });

    it('lỗi mạng khi nạp tệp → cũng là lỗi nêu tệp, không phải bản dịch rỗng', () => {
      const loader = dungVoi(['/i18n/']);
      const out = layLoi(loader);

      httpMock.expectOne('/i18n/vi.json').error(new ProgressEvent('error'));

      expect(out.loi).toBeInstanceOf(LoiNapTepDichError);
      expect(out.ketQua).toBeUndefined();
    });

    it('đủ tệp mọi tầng → không báo lỗi nào', () => {
      const loader = dungVoi(['/i18n/', '/i18n-app/']);
      const out = layLoi(loader);

      httpMock.expectOne('/i18n/vi.json').flush({ a: '1' });
      httpMock.expectOne('/i18n-app/vi.json').flush({ b: '2' });

      expect(out.ketQua).toEqual({ a: '1', b: '2' });
      expect(out.loi).toBeUndefined();
      expect(daBao).toEqual([]);
    });
  });
});
