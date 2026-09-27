import { HttpClient, HttpContext } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';

import { danhSachTrang } from '../../../../core/http/crud';
import { ApiResult, BO_QUA_TOAST_LOI } from '../../../../core/http/api-result.model';
import { PagedList, PageQuery } from '../../../../core/http/paged.model';
import { unwrapData } from '../../../../core/http/unwrap';
import {
  DonViDto,
  KhoiPhucQuanTriPayload,
  TaoDonViPayload,
  TaoQuanTriMoiPayload,
} from '../models/don-vi.dto';
import { DonVi } from '../models/don-vi.model';
import { mapDonVi } from './don-vi.mapper';

/**
 * contracts/tenants.md. Mọi endpoint mang `[RequireSystemOperator]` — không phải permission
 * (Design/Screens/20-don-vi.md). `danhSach` KHÔNG tắt toast — cùng lý do ở `VaiTroService.danhSach`
 * (nguoi-dung.service.ts): màn danh sách (`ListStateStore`) không giữ chi tiết lỗi, nên toast mặc
 * định của interceptor là nơi DUY NHẤT mang câu lỗi cụ thể. Mọi lời gọi còn lại tắt toast — màn tự
 * hiện lỗi theo card ở đúng nơi cần (NoticeBanner trong hộp, hoặc field error).
 */
@Injectable({ providedIn: 'root' })
export class DonViService {
  private readonly http = inject(HttpClient);
  private readonly duongDan = '/core/system/tenants';
  private readonly ctx = new HttpContext().set(BO_QUA_TOAST_LOI, true);

  danhSach(q: PageQuery): Observable<PagedList<DonVi>> {
    return danhSachTrang<DonViDto, DonVi>(this.http, this.duongDan, q, mapDonVi);
  }

  /** contracts/tenants.md §2 — response KHÔNG mang mật khẩu; `data` là đơn vị vừa tạo. */
  tao(payload: TaoDonViPayload): Observable<DonVi> {
    return this.http
      .post<ApiResult<DonViDto>>(this.duongDan, payload, { context: this.ctx })
      .pipe(map(unwrapData), map(mapDonVi));
  }

  /** contracts/tenants.md §3 — `isActive: true` bật lại, `false` ngưng hoạt động. */
  datTrangThai(id: string, isActive: boolean): Observable<void> {
    return this.http
      .put<ApiResult<null>>(`${this.duongDan}/${id}/active`, { isActive }, { context: this.ctx })
      .pipe(map(() => undefined));
  }

  /** contracts/tenants.md §4 — response KHÔNG mang dữ liệu nào về tài khoản đích (cố ý). */
  khoiPhucQuanTri(id: string, payload: KhoiPhucQuanTriPayload): Observable<void> {
    return this.http
      .post<ApiResult<null>>(`${this.duongDan}/${id}/recovery-reset-password`, payload, {
        context: this.ctx,
      })
      .pipe(map(() => undefined));
  }

  /** contracts/tenants.md §6 — response KHÔNG mang dữ liệu nào về tài khoản vừa tạo. */
  taoQuanTriMoi(id: string, payload: TaoQuanTriMoiPayload): Observable<void> {
    return this.http
      .post<ApiResult<null>>(`${this.duongDan}/${id}/admins`, payload, { context: this.ctx })
      .pipe(map(() => undefined));
  }
}
