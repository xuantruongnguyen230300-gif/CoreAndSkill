import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';

import { HoSoService } from './ho-so.service';
import { HoSoDto } from '../models/ho-so.dto';

const DTO: HoSoDto = {
  userName: 'an.nv',
  email: 'an.nv@vd.vn',
  fullName: 'Nguyễn Văn An',
  phoneNumber: '0912345678',
  preferredLanguage: 'vi',
  hasPermissionBypass: false,
  version: 'v1',
};

describe('HoSoService', () => {
  let service: HoSoService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection(), provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(HoSoService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('layHoSo() bóc envelope và map DTO → model', () => {
    let ketQua: unknown;
    service.layHoSo().subscribe((d) => (ketQua = d));

    httpMock.expectOne('/core/profile').flush({ success: true, data: DTO, error: null, traceId: 't' });

    expect(ketQua).toEqual({
      userName: 'an.nv',
      email: 'an.nv@vd.vn',
      hoTen: 'Nguyễn Văn An',
      soDienThoai: '0912345678',
      ngonNguUaThich: 'vi',
      coDacQuyen: false,
      version: 'v1',
    });
  });

  it('capNhatHoSo() gửi PUT với đúng payload', () => {
    service.capNhatHoSo({ fullName: 'A', phoneNumber: null, preferredLanguage: null, version: 'v1' }).subscribe();

    const req = httpMock.expectOne('/core/profile');
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual({ fullName: 'A', phoneNumber: null, preferredLanguage: null, version: 'v1' });
    req.flush({ success: true, data: DTO, error: null, traceId: 't' });
  });

  it('tuBoCoDacQuyen() gọi đúng endpoint', () => {
    service.tuBoCoDacQuyen().subscribe();
    const req = httpMock.expectOne('/core/profile/renounce-permission-bypass');
    expect(req.request.method).toBe('POST');
    req.flush({ success: true, data: null, error: null, traceId: 't' });
  });
});
