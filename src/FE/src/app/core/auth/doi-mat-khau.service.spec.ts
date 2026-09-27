import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';

import { DoiMatKhauService } from './doi-mat-khau.service';

describe('DoiMatKhauService', () => {
  let service: DoiMatKhauService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection(), provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(DoiMatKhauService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('doiBatBuoc() gọi đúng đường dẫn change-password-required', () => {
    let xong = false;
    service.doiBatBuoc({ currentPassword: 'a', newPassword: 'b' }).subscribe(() => (xong = true));

    const req = httpMock.expectOne('/core/auth/change-password-required');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ currentPassword: 'a', newPassword: 'b' });
    req.flush({ success: true, data: null, error: null, traceId: 't' });

    expect(xong).toBeTrue();
  });

  it('doiTuNguyen() gọi đúng đường dẫn change-password', () => {
    service.doiTuNguyen({ currentPassword: 'a', newPassword: 'b' }).subscribe();
    const req = httpMock.expectOne('/core/auth/change-password');
    expect(req.request.method).toBe('POST');
    req.flush({ success: true, data: null, error: null, traceId: 't' });
  });
});
