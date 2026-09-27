import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';

import { NguoiDungService } from './nguoi-dung.service';
import { NguoiDungDto } from '../models/nguoi-dung.dto';
import { BO_QUA_TOAST_LOI } from '../../../../core/http/api-result.model';

const DTO: NguoiDungDto = {
  id: 'u1',
  userName: 'an.nv',
  email: 'an.nv@vd.vn',
  fullName: 'Nguyễn Văn An',
  roles: [{ id: 'r1', name: 'Quản trị hệ thống', isSystem: true }],
  isLocked: false,
  lockoutEnd: null,
  lockedByAdmin: false,
  mustChangePassword: false,
  createdAt: '2026-09-01T03:12:45.120Z',
  version: 'v1',
};

describe('NguoiDungService', () => {
  let service: NguoiDungService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });
    service = TestBed.inject(NguoiDungService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('danhSach() bóc envelope, map DTO → model', () => {
    let ketQua: unknown;
    service.danhSach({ page: 1, pageSize: 20 }).subscribe((d) => (ketQua = d));

    const req = httpMock.expectOne((r) => r.url === '/core/users');
    req.flush({
      success: true,
      data: { items: [DTO], page: 1, pageSize: 20, totalCount: 1 },
      error: null,
      traceId: 't',
    });

    expect(ketQua).toEqual({
      items: [
        {
          id: 'u1',
          userName: 'an.nv',
          email: 'an.nv@vd.vn',
          fullName: 'Nguyễn Văn An',
          roles: [{ id: 'r1', name: 'Quản trị hệ thống', isSystem: true }],
          isLocked: false,
          lockoutEnd: null,
          lockedByAdmin: false,
          mustChangePassword: false,
          createdAt: new Date(DTO.createdAt as string),
          version: 'v1',
        },
      ],
      page: 1,
      pageSize: 20,
      totalCount: 1,
    });
  });

  it('chiTiet() tắt toast mặc định', () => {
    service.chiTiet('u1').subscribe();
    const req = httpMock.expectOne('/core/users/u1');
    expect(req.request.context.get(BO_QUA_TOAST_LOI)).toBe(true);
    req.flush({ success: true, data: DTO, error: null, traceId: 't' });
  });

  it('tao() gửi POST với đúng payload', () => {
    service
      .tao({
        userName: 'binh.tv',
        email: 'binh.tv@vd.vn',
        fullName: 'Trần Văn Bình',
        tempPassword: 'Abc12345',
        roleIds: [],
      })
      .subscribe();
    const req = httpMock.expectOne('/core/users');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({
      userName: 'binh.tv',
      email: 'binh.tv@vd.vn',
      fullName: 'Trần Văn Bình',
      tempPassword: 'Abc12345',
      roleIds: [],
    });
    req.flush({ success: true, data: { id: 'u2' }, error: null, traceId: 't' });
  });

  it('suaThongTin() gửi PUT không mang userName', () => {
    service
      .suaThongTin('u1', { email: 'moi@vd.vn', fullName: 'Tên mới', version: 'v1' })
      .subscribe();
    const req = httpMock.expectOne('/core/users/u1');
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual({ email: 'moi@vd.vn', fullName: 'Tên mới', version: 'v1' });
    req.flush({ success: true, data: null, error: null, traceId: 't' });
  });

  it('ganVaiTro() gửi PUT tới /roles, thay thế toàn bộ, kèm version của tài khoản đích', () => {
    service.ganVaiTro('u1', { roleIds: ['r1', 'r2'], version: 'v1' }).subscribe();
    const req = httpMock.expectOne('/core/users/u1/roles');
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual({ roleIds: ['r1', 'r2'], version: 'v1' });
    req.flush({ success: true, data: null, error: null, traceId: 't' });
  });

  it('khoa() gửi POST kèm version của bản ghi đích', () => {
    service.khoa('u1', { version: 'v1' }).subscribe();
    const req = httpMock.expectOne('/core/users/u1/lock');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ version: 'v1' });
    req.flush({ success: true, data: null, error: null, traceId: 't' });
  });

  it('moKhoa() gửi POST kèm version', () => {
    service.moKhoa('u1', { version: 'v1' }).subscribe();
    const req = httpMock.expectOne('/core/users/u1/unlock');
    expect(req.request.method).toBe('POST');
    req.flush({ success: true, data: null, error: null, traceId: 't' });
  });

  it('datLaiMatKhau() gửi POST với tempPassword và version', () => {
    service.datLaiMatKhau('u1', { tempPassword: 'Xyz98765', version: 'v1' }).subscribe();
    const req = httpMock.expectOne('/core/users/u1/reset-password');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ tempPassword: 'Xyz98765', version: 'v1' });
    req.flush({ success: true, data: null, error: null, traceId: 't' });
  });
});
