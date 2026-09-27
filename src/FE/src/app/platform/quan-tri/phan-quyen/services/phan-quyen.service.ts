import { HttpClient, HttpContext } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';

import { ApiResult, BO_QUA_TOAST_LOI } from '../../../../core/http/api-result.model';
import { unwrapData } from '../../../../core/http/unwrap';
import {
  CapNhatMaTranPayload,
  CapNhatMaTranResultDto,
  MaTranPhanQuyenDto,
} from '../models/phan-quyen.dto';
import { MaTranPhanQuyen } from '../models/phan-quyen.model';
import { mapMaTranPhanQuyen } from './phan-quyen.mapper';

/**
 * contracts/permissions.md §5, §6. Cả hai lời gọi tắt toast mặc định — màn tự hiện lỗi: đọc thì
 * vào trạng thái `error` của `Table` (Design/Screens/12), ghi thì vào `NoticeBanner` dưới
 * `PageHeader` theo đúng mã (409 `VERSION_MISMATCH` có nút "Tải lại ma trận" riêng).
 */
@Injectable({ providedIn: 'root' })
export class PhanQuyenService {
  private readonly http = inject(HttpClient);
  private readonly duongDan = '/core/permissions/matrix';
  private readonly ctx = new HttpContext().set(BO_QUA_TOAST_LOI, true);

  layMaTran(): Observable<MaTranPhanQuyen> {
    return this.http
      .get<ApiResult<MaTranPhanQuyenDto>>(this.duongDan, { context: this.ctx })
      .pipe(map(unwrapData), map(mapMaTranPhanQuyen));
  }

  luuMaTran(payload: CapNhatMaTranPayload): Observable<string> {
    return this.http
      .put<ApiResult<CapNhatMaTranResultDto>>(this.duongDan, payload, { context: this.ctx })
      .pipe(
        map(unwrapData),
        map((d) => d.version),
      );
  }
}
