import { HttpClient, HttpContext } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';

import { danhSachTrang } from '../../../../core/http/crud';
import { ApiResult, BO_QUA_TOAST_LOI } from '../../../../core/http/api-result.model';
import { PagedList, PageQuery } from '../../../../core/http/paged.model';
import { unwrapData } from '../../../../core/http/unwrap';
import {
  CapNhatNguoiDungPayload,
  DatLaiMatKhauPayload,
  GanVaiTroPayload,
  NguoiDungDto,
  TaoNguoiDungPayload,
  VersionedPayload,
} from '../models/nguoi-dung.dto';
import { NguoiDung } from '../models/nguoi-dung.model';
import { mapNguoiDung } from './nguoi-dung.mapper';

/**
 * contracts/users.md. `danhSach` KHÔNG tắt toast — cùng lý do ở `VaiTroService.danhSach`. Mọi
 * lời gọi còn lại tắt toast — màn tự hiện lỗi theo card.
 */
@Injectable({ providedIn: 'root' })
export class NguoiDungService {
  private readonly http = inject(HttpClient);
  private readonly duongDan = '/core/users';
  private readonly ctx = new HttpContext().set(BO_QUA_TOAST_LOI, true);

  danhSach(q: PageQuery): Observable<PagedList<NguoiDung>> {
    return danhSachTrang<NguoiDungDto, NguoiDung>(this.http, this.duongDan, q, mapNguoiDung);
  }

  /** Không qua `theoId` dùng chung — hàm đó không nhận `HttpContext`, và chi tiết PHẢI tắt toast
   *  (màn tự hiện `record-not-found` hoặc lỗi thẻ — Design/Screens/10-nguoi-dung.md §Trạng thái). */
  chiTiet(id: string): Observable<NguoiDung> {
    return this.http
      .get<ApiResult<NguoiDungDto>>(`${this.duongDan}/${id}`, { context: this.ctx })
      .pipe(map(unwrapData), map(mapNguoiDung));
  }

  tao(payload: TaoNguoiDungPayload): Observable<{ id: string }> {
    return this.http
      .post<ApiResult<{ id: string }>>(this.duongDan, payload, { context: this.ctx })
      .pipe(map(unwrapData));
  }

  suaThongTin(id: string, payload: CapNhatNguoiDungPayload): Observable<void> {
    return this.http
      .put<ApiResult<null>>(`${this.duongDan}/${id}`, payload, { context: this.ctx })
      .pipe(map(() => undefined));
  }

  ganVaiTro(id: string, payload: GanVaiTroPayload): Observable<void> {
    return this.http
      .put<ApiResult<null>>(`${this.duongDan}/${id}/roles`, payload, { context: this.ctx })
      .pipe(map(() => undefined));
  }

  khoa(id: string, payload: VersionedPayload): Observable<void> {
    return this.http
      .post<ApiResult<null>>(`${this.duongDan}/${id}/lock`, payload, { context: this.ctx })
      .pipe(map(() => undefined));
  }

  moKhoa(id: string, payload: VersionedPayload): Observable<void> {
    return this.http
      .post<ApiResult<null>>(`${this.duongDan}/${id}/unlock`, payload, { context: this.ctx })
      .pipe(map(() => undefined));
  }

  datLaiMatKhau(id: string, payload: DatLaiMatKhauPayload): Observable<void> {
    return this.http
      .post<ApiResult<null>>(`${this.duongDan}/${id}/reset-password`, payload, {
        context: this.ctx,
      })
      .pipe(map(() => undefined));
  }
}
