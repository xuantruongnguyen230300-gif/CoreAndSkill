import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';

import { ApiFailureError, BO_QUA_TOAST_LOI } from '../http/api-result.model';
import { XsrfTokenService } from './xsrf-token.service';

describe('XsrfTokenService', () => {
  let service: XsrfTokenService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });
    service = TestBed.inject(XsrfTokenService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('layToken() gọi ĐÚNG đường dẫn ngắn bằng GET và trả data.token đã bóc envelope', () => {
    let ketQua: string | undefined;
    service.layToken(false).subscribe((t) => (ketQua = t));

    const req = httpMock.expectOne('/core/antiforgery/token');
    expect(req.request.method).toBe('GET');
    req.flush({ success: true, data: { token: 'xsrf-abc' }, error: null, traceId: 't-1' });

    expect(ketQua).toBe('xsrf-abc');
  });

  // errorInterceptor truyền cờ tắt toast của REQUEST GỐC ở nhánh CSRF (fe-ui-conventions.md §6.2).
  it('đặt BO_QUA_TOAST_LOI đúng theo tham số', () => {
    service.layToken(false).subscribe();
    const macDinh = httpMock.expectOne('/core/antiforgery/token');
    expect(macDinh.request.context.get(BO_QUA_TOAST_LOI)).toBeFalse();
    macDinh.flush({ success: true, data: { token: 'a' }, error: null, traceId: 't' });

    service.layToken(true).subscribe();
    const coCo = httpMock.expectOne('/core/antiforgery/token');
    expect(coCo.request.context.get(BO_QUA_TOAST_LOI)).toBeTrue();
    coCo.flush({ success: true, data: { token: 'b' }, error: null, traceId: 't' });
  });

  it('envelope success:false → ném ApiFailureError (unwrapData), không trả token', () => {
    let loi: unknown;
    service.layToken(false).subscribe({ error: (e: unknown) => (loi = e) });

    httpMock.expectOne('/core/antiforgery/token').flush({
      success: false,
      data: null,
      error: { code: 'CORE.X', type: 'T', message: 'm', messageParams: null, fieldErrors: null },
      traceId: 't-2',
    });

    expect(loi).toBeInstanceOf(ApiFailureError);
  });
});
