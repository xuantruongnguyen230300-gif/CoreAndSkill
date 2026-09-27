import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';

import { BO_QUA_TOAST_LOI } from '../http/api-result.model';
import { XsrfTokenStore } from './xsrf-token.store';

describe('XsrfTokenStore', () => {
  let store: XsrfTokenStore;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });
    store = TestBed.inject(XsrfTokenStore);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('token() là null trước khi lamMoi() từng chạy', () => {
    expect(store.token()).toBeNull();
  });

  it('lamMoi() gọi ĐÚNG đường dẫn ngắn, đọc data.token, và giữ nó ở bộ nhớ', () => {
    let ketQua: string | undefined;
    store.lamMoi().subscribe((t) => (ketQua = t));

    const req = httpMock.expectOne('/core/antiforgery/token');
    expect(req.request.method).toBe('GET');
    req.flush({ success: true, data: { token: 'xsrf-abc' }, error: null, traceId: 't-1' });

    expect(ketQua).toBe('xsrf-abc');
    expect(store.token()).toBe('xsrf-abc');
  });

  // errorInterceptor truyền cờ tắt toast của REQUEST GỐC ở nhánh CSRF (fe-ui-conventions.md §6.2).
  it('mặc định GET token KHÔNG mang BO_QUA_TOAST_LOI; lamMoi({ boQuaToastLoi: true }) thì mang', () => {
    store.lamMoi().subscribe();
    const macDinh = httpMock.expectOne('/core/antiforgery/token');
    expect(macDinh.request.context.get(BO_QUA_TOAST_LOI)).toBeFalse();
    macDinh.flush({ success: true, data: { token: 'a' }, error: null, traceId: 't' });

    store.lamMoi({ boQuaToastLoi: true }).subscribe();
    const coCo = httpMock.expectOne('/core/antiforgery/token');
    expect(coCo.request.context.get(BO_QUA_TOAST_LOI)).toBeTrue();
    coCo.flush({ success: true, data: { token: 'b' }, error: null, traceId: 't' });
  });
});
