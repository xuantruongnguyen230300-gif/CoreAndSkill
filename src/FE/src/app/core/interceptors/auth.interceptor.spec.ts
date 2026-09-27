import { HttpClient, HttpContext, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';

import { authInterceptor } from './auth.interceptor';
import { XsrfTokenStore } from '../auth/xsrf-token.store';
import { API_BASE_URL } from '../http/api-base-url';
import { CO_TAC_DUNG_PHU } from '../http/api-result.model';

describe('authInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        { provide: API_BASE_URL, useValue: 'https://api.example.test/api/v1' },
      ],
    });
    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('ghép base URL vào đường dẫn tương đối và gửi kèm cookie phiên', () => {
    http.get('/core/users').subscribe();

    const req = httpMock.expectOne('https://api.example.test/api/v1/core/users');
    expect(req.request.withCredentials).toBeTrue();
    req.flush({});
  });

  it('KHÔNG ghép base URL vào URL tuyệt đối, và không gắn credentials', () => {
    http.get('https://khac-nguon.example.test/file.json').subscribe();

    const req = httpMock.expectOne('https://khac-nguon.example.test/file.json');
    expect(req.request.withCredentials).toBeFalse();
    req.flush({});
  });

  it('GET không gắn header X-XSRF-TOKEN dù đã có token trong store', () => {
    const xsrf = TestBed.inject(XsrfTokenStore);
    spyOn(xsrf, 'token').and.returnValue('token-abc');

    http.get('/core/users').subscribe();
    const req = httpMock.expectOne('https://api.example.test/api/v1/core/users');
    expect(req.request.headers.has('X-XSRF-TOKEN')).toBeFalse();
    req.flush({});
  });

  it('POST gắn header X-XSRF-TOKEN khi store đã có token', () => {
    const xsrf = TestBed.inject(XsrfTokenStore);
    spyOn(xsrf, 'token').and.returnValue('token-abc');

    http.post('/core/users', {}).subscribe();
    const req = httpMock.expectOne('https://api.example.test/api/v1/core/users');
    expect(req.request.headers.get('X-XSRF-TOKEN')).toBe('token-abc');
    req.flush({});
  });

  // Ngoại lệ CÓ TÊN của ràng buộc 3 (fe-api-client.md §2.1): GET được đánh dấu tường minh là có
  // tác dụng phụ — hôm nay chỉ endpoint xuất, nó ghi nhật ký kiểm toán nên BE kiểm token như lệnh
  // ghi (ADR-0062). Điều hướng không mang được header, nên FE phải tải bằng HttpClient.
  it('GET mang dấu CO_TAC_DUNG_PHU gắn header X-XSRF-TOKEN như lệnh ghi', () => {
    const xsrf = TestBed.inject(XsrfTokenStore);
    spyOn(xsrf, 'token').and.returnValue('token-abc');
    const ctx = new HttpContext().set(CO_TAC_DUNG_PHU, true);

    http.get('/core/users/export', { context: ctx }).subscribe();
    const req = httpMock.expectOne('https://api.example.test/api/v1/core/users/export');
    expect(req.request.headers.get('X-XSRF-TOKEN')).toBe('token-abc');
    req.flush({});
  });

  it('URL tuyệt đối mang dấu CO_TAC_DUNG_PHU vẫn KHÔNG mang token — không rò sang origin khác', () => {
    const xsrf = TestBed.inject(XsrfTokenStore);
    spyOn(xsrf, 'token').and.returnValue('token-abc');
    const ctx = new HttpContext().set(CO_TAC_DUNG_PHU, true);

    http.get('https://khac-nguon.example.test/tep.csv', { context: ctx }).subscribe();
    const req = httpMock.expectOne('https://khac-nguon.example.test/tep.csv');
    expect(req.request.headers.has('X-XSRF-TOKEN')).toBeFalse();
    req.flush({});
  });
});
