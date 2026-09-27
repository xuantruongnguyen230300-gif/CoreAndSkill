import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';

import { DonViService } from './don-vi.service';
import { DonViDto } from '../models/don-vi.dto';
import { BO_QUA_TOAST_LOI } from '../../../../core/http/api-result.model';

const DTO: DonViDto = {
  id: 't1',
  code: 'SYT-HN',
  name: 'Sở Y tế Hà Nội',
  isActive: true,
  createdAt: '2026-09-10T03:12:44.000Z',
};

describe('DonViService', () => {
  let service: DonViService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });
    service = TestBed.inject(DonViService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('danhSach() bóc envelope, map DTO → model, đổi createdAt sang Date, KHÔNG tắt toast', () => {
    let ketQua: unknown;
    service.danhSach({ page: 1, pageSize: 20 }).subscribe((d) => (ketQua = d));

    const req = httpMock.expectOne((r) => r.url === '/core/system/tenants');
    expect(req.request.context.get(BO_QUA_TOAST_LOI)).toBe(false);
    req.flush({
      success: true,
      data: { items: [DTO], page: 1, pageSize: 20, totalCount: 1 },
      error: null,
      traceId: 't',
    });

    expect(ketQua).toEqual({
      items: [
        {
          id: 't1',
          code: 'SYT-HN',
          name: 'Sở Y tế Hà Nội',
          isActive: true,
          createdAt: new Date(DTO.createdAt as string),
        },
      ],
      page: 1,
      pageSize: 20,
      totalCount: 1,
    });
  });

  it('tao() gửi POST với đúng payload, tắt toast mặc định', () => {
    service
      .tao({
        code: 'SYT-HN',
        name: 'Sở Y tế Hà Nội',
        adminUserName: 'quantri.syt-hn',
        adminEmail: 'quantri@syt-hn.gov.vn',
        adminFullName: 'Nguyễn Văn An',
        adminTempPassword: 'Matkhau@123',
      })
      .subscribe();
    const req = httpMock.expectOne('/core/system/tenants');
    expect(req.request.method).toBe('POST');
    expect(req.request.context.get(BO_QUA_TOAST_LOI)).toBe(true);
    expect(req.request.body).toEqual({
      code: 'SYT-HN',
      name: 'Sở Y tế Hà Nội',
      adminUserName: 'quantri.syt-hn',
      adminEmail: 'quantri@syt-hn.gov.vn',
      adminFullName: 'Nguyễn Văn An',
      adminTempPassword: 'Matkhau@123',
    });
    req.flush({ success: true, data: DTO, error: null, traceId: 't' });
  });

  it('datTrangThai() gửi PUT .../active với đúng payload', () => {
    service.datTrangThai('t1', false).subscribe();
    const req = httpMock.expectOne('/core/system/tenants/t1/active');
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual({ isActive: false });
    req.flush({ success: true, data: null, error: null, traceId: 't' });
  });

  it('khoiPhucQuanTri() gửi POST .../recovery-reset-password', () => {
    service.khoiPhucQuanTri('t1', { userName: 'quantri.syt-hn', tempPassword: 'Matkhau@123' }).subscribe();
    const req = httpMock.expectOne('/core/system/tenants/t1/recovery-reset-password');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ userName: 'quantri.syt-hn', tempPassword: 'Matkhau@123' });
    req.flush({ success: true, data: null, error: null, traceId: 't' });
  });

  it('taoQuanTriMoi() gửi POST .../admins', () => {
    service
      .taoQuanTriMoi('t1', {
        userName: 'quantri2.syt-hn',
        email: 'quantri2@syt-hn.gov.vn',
        fullName: 'Trần Thị Bình',
        tempPassword: 'Matkhau@123',
      })
      .subscribe();
    const req = httpMock.expectOne('/core/system/tenants/t1/admins');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({
      userName: 'quantri2.syt-hn',
      email: 'quantri2@syt-hn.gov.vn',
      fullName: 'Trần Thị Bình',
      tempPassword: 'Matkhau@123',
    });
    req.flush({ success: true, data: null, error: null, traceId: 't' });
  });
});
