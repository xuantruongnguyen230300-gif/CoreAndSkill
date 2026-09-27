import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';

import { PhanQuyenService } from './phan-quyen.service';
import { MaTranPhanQuyenDto } from '../models/phan-quyen.dto';
import { BO_QUA_TOAST_LOI } from '../../../../core/http/api-result.model';

const DTO: MaTranPhanQuyenDto = {
  roles: [{ id: 'r1', name: 'Quản trị hệ thống', isSystem: true }],
  rows: [
    {
      permissionId: 'p1',
      code: 'core.user.read',
      resourceKey: 'core.user',
      resourceNameKey: 'resource.core.user',
      nameKey: 'permission.core.user.read',
      grantedRoleIds: ['r1'],
    },
  ],
  version: 'sha256:abc',
};

describe('PhanQuyenService', () => {
  let service: PhanQuyenService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });
    service = TestBed.inject(PhanQuyenService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('layMaTran() bóc envelope, tắt toast mặc định', () => {
    let ketQua: unknown;
    service.layMaTran().subscribe((d) => (ketQua = d));

    const req = httpMock.expectOne('/core/permissions/matrix');
    expect(req.request.context.get(BO_QUA_TOAST_LOI)).toBe(true);
    req.flush({ success: true, data: DTO, error: null, traceId: 't' });

    expect(ketQua).toEqual({ roles: DTO.roles, rows: DTO.rows, version: 'sha256:abc' });
  });

  it('luuMaTran() gửi PUT, trả về version MỚI', () => {
    let versionMoi: string | undefined;
    service
      .luuMaTran({ version: 'sha256:abc', entries: [{ permissionId: 'p1', roleIds: ['r1'] }] })
      .subscribe((v) => (versionMoi = v));

    const req = httpMock.expectOne('/core/permissions/matrix');
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual({
      version: 'sha256:abc',
      entries: [{ permissionId: 'p1', roleIds: ['r1'] }],
    });
    req.flush({ success: true, data: { version: 'sha256:def' }, error: null, traceId: 't' });

    expect(versionMoi).toBe('sha256:def');
  });
});
