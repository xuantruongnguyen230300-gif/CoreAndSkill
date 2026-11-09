import localeVi from '@angular/common/locales/vi';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import {
  TranslateLoader,
  TranslationObject,
  provideTranslateLoader,
  provideTranslateService,
} from '@ngx-translate/core';
import { Observable, of } from 'rxjs';

import { provideCoreI18n } from '../config/core-i18n';
import { CORE_ROUTES } from '../config/core-routes';
import { ClientErrorReporter } from '../observability/client-error-reporter';
import { ToastService } from '../toast/toast.service';
import { errorInterceptor } from './error.interceptor';

class DichRong extends TranslateLoader {
  getTranslation(): Observable<TranslationObject> {
    return of({});
  }
}

/**
 * 10-observability.md §5.2: bộ thu lỗi giữ `traceId` của request gần nhất. Nguồn của nó là chuỗi
 * interceptor — mọi phản hồi, thành công lẫn thất bại, đi qua `errorInterceptor`.
 */
describe('errorInterceptor — ghi traceId cho bộ thu lỗi runtime', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;
  let boThuLoi: ClientErrorReporter;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(withInterceptors([errorInterceptor])),
        provideHttpClientTesting(),
        provideRouter([]),
        provideCoreI18n({
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
        }),
        provideTranslateService({
          lang: 'vi',
          fallbackLang: 'vi',
          loader: provideTranslateLoader(DichRong),
        }),
        {
          provide: CORE_ROUTES,
          useValue: {
            dangNhap: '/dang-nhap',
            doiMatKhauBatBuoc: '/doi-mat-khau-bat-buoc',
            khongCoQuyen: '/khong-co-quyen',
            sauDangNhap: '/',
          },
        },
      ],
    });
    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
    boThuLoi = TestBed.inject(ClientErrorReporter);
    spyOn(TestBed.inject(ToastService), 'loi');
  });

  afterEach(() => httpMock.verify());

  it('phản hồi thành công mang traceId → được ghi', () => {
    http.get('/ok').subscribe();
    httpMock
      .expectOne('/ok')
      .flush({ success: true, data: { a: 1 }, error: null, traceId: 'trace-thanh-cong' });

    expect(boThuLoi.traceIdGanDay()).toEqual(['trace-thanh-cong']);
  });

  it('phản hồi lỗi có envelope → traceId của nó được ghi, sau cái trước', () => {
    http.get('/ok').subscribe();
    httpMock.expectOne('/ok').flush({ success: true, data: null, error: null, traceId: 'trace-1' });
    http.get('/loi').subscribe({ error: () => undefined });
    httpMock.expectOne('/loi').flush(
      {
        success: false,
        data: null,
        error: {
          code: 'CORE.X.Y',
          type: 'X',
          message: 'm',
          messageParams: null,
          fieldErrors: null,
        },
        traceId: 'trace-2',
      },
      { status: 422, statusText: 'x' },
    );

    expect(boThuLoi.traceIdGanDay()).toEqual(['trace-1', 'trace-2']);
  });

  it('phản hồi không phải envelope (HTML của proxy, tệp tải về) → không ghi gì, không ném', () => {
    http.get('/html', { responseType: 'text' }).subscribe();
    httpMock.expectOne('/html').flush('<html>ok</html>');
    http.get('/proxy').subscribe({ error: () => undefined });
    httpMock
      .expectOne('/proxy')
      .flush('<html>Bad Gateway</html>', { status: 502, statusText: 'x' });

    expect(boThuLoi.traceIdGanDay()).toEqual([]);
  });
});
