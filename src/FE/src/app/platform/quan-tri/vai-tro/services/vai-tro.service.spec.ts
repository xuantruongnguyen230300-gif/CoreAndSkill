import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';

import { VaiTroService } from './vai-tro.service';
import { VaiTroDto } from '../models/vai-tro.dto';
import { BO_QUA_TOAST_LOI } from '../../../../core/http/api-result.model';

const DTO: VaiTroDto = {
  id: 'r1',
  name: 'Quản trị hệ thống',
  isSystem: true,
  userCount: 3,
  createdAt: '2026-09-10T03:12:44.000Z',
  version: '4b1f0c2e-7a3d-4e59-9c86-2d0e5f7a1b34',
};

describe('VaiTroService', () => {
  let service: VaiTroService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });
    service = TestBed.inject(VaiTroService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('danhSach() bóc envelope, map DTO → model, đổi createdAt sang Date', () => {
    let ketQua: unknown;
    service.danhSach({ page: 1, pageSize: 20 }).subscribe((d) => (ketQua = d));

    const req = httpMock.expectOne((r) => r.url === '/core/roles');
    req.flush({
      success: true,
      data: { items: [DTO], page: 1, pageSize: 20, totalCount: 1 },
      error: null,
      traceId: 't',
    });

    expect(ketQua).toEqual({
      items: [
        {
          id: 'r1',
          name: 'Quản trị hệ thống',
          isSystem: true,
          userCount: 3,
          createdAt: new Date(DTO.createdAt as string),
          version: '4b1f0c2e-7a3d-4e59-9c86-2d0e5f7a1b34',
        },
      ],
      page: 1,
      pageSize: 20,
      totalCount: 1,
    });
  });

  it('timKiem() gửi searchText và tắt toast mặc định', () => {
    service.timKiem('kt').subscribe();

    const req = httpMock.expectOne(
      (r) => r.url === '/core/roles' && r.params.get('searchText') === 'kt',
    );
    expect(req.request.context.get(BO_QUA_TOAST_LOI)).toBe(true);
    req.flush({
      success: true,
      data: { items: [], page: 1, pageSize: 20, totalCount: 0 },
      error: null,
      traceId: 't',
    });
  });

  it('tao() gửi POST với đúng payload', () => {
    service.tao('Kế toán trưởng').subscribe();
    const req = httpMock.expectOne('/core/roles');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ name: 'Kế toán trưởng' });
    req.flush({ success: true, data: { id: 'r2' }, error: null, traceId: 't' });
  });

  it('chiTiet() giữ nguyên chuỗi version (roles.md §5) và tắt toast', () => {
    let ketQua: { version: string } | undefined;
    service.chiTiet('r1').subscribe((d) => (ketQua = d));
    const req = httpMock.expectOne('/core/roles/r1');
    expect(req.request.method).toBe('GET');
    expect(req.request.context.get(BO_QUA_TOAST_LOI)).toBe(true);
    req.flush({ success: true, data: DTO, error: null, traceId: 't' });
    expect(ketQua?.version).toBe(DTO.version);
  });

  it('doiTen() gửi PUT { name, version } và trả vai trò đã đổi mang version MỚI (roles.md §3)', () => {
    let ketQua: unknown;
    service.doiTen('r1', 'Tên mới', 'v-cu').subscribe((d) => (ketQua = d));
    const req = httpMock.expectOne('/core/roles/r1');
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual({ name: 'Tên mới', version: 'v-cu' });
    expect(req.request.context.get(BO_QUA_TOAST_LOI)).toBe(true);
    req.flush({
      success: true,
      data: { ...DTO, name: 'Tên mới', createdAt: null, version: 'v-moi' },
      error: null,
      traceId: 't',
    });
    expect(ketQua).toEqual({
      id: 'r1',
      name: 'Tên mới',
      isSystem: true,
      userCount: 3,
      createdAt: null,
      version: 'v-moi',
    });
  });

  it('xoa() gửi DELETE', () => {
    service.xoa('r1').subscribe();
    const req = httpMock.expectOne('/core/roles/r1');
    expect(req.request.method).toBe('DELETE');
    req.flush({ success: true, data: null, error: null, traceId: 't' });
  });
});
