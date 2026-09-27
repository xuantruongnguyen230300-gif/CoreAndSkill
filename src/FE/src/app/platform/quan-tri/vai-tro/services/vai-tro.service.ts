import { HttpClient, HttpContext, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';

import { danhSachTrang } from '../../../../core/http/crud';
import { ApiResult, BO_QUA_TOAST_LOI } from '../../../../core/http/api-result.model';
import { PagedList, PageQuery } from '../../../../core/http/paged.model';
import { unwrapData } from '../../../../core/http/unwrap';
import { VaiTroDto } from '../models/vai-tro.dto';
import { VaiTro } from '../models/vai-tro.model';
import { mapVaiTro } from './vai-tro.mapper';

/**
 * contracts/roles.md. `danhSach` KHÔNG tắt toast — màn danh sách (ListStateStore) không giữ chi
 * tiết lỗi, nên toast mặc định của interceptor là nơi DUY NHẤT mang câu lỗi cụ thể
 * (fe-architecture.md §2.8 dòng "errorInterceptor đã hiển thị lỗi; ở đây chỉ đổi trạng thái").
 * Mọi lời gọi còn lại tắt toast — màn tự hiện lỗi theo card ở đúng nơi cần.
 */
@Injectable({ providedIn: 'root' })
export class VaiTroService {
  private readonly http = inject(HttpClient);
  private readonly duongDan = '/core/roles';
  private readonly ctx = new HttpContext().set(BO_QUA_TOAST_LOI, true);

  danhSach(q: PageQuery): Observable<PagedList<VaiTro>> {
    return danhSachTrang<VaiTroDto, VaiTro>(this.http, this.duongDan, q, mapVaiTro);
  }

  /**
   * Tra TÊN vai trò theo `roleId` trên URL (Design/Screens/10-nguoi-dung.md), và lấy `version` mới
   * cho hộp đổi tên sau `CORE.CONCURRENCY.CONFLICT` (roles.md §3, §5).
   */
  chiTiet(id: string): Observable<VaiTro> {
    return this.http
      .get<ApiResult<VaiTroDto>>(`${this.duongDan}/${id}`, { context: this.ctx })
      .pipe(map(unwrapData), map(mapVaiTro));
  }

  /** Ô chọn vai trò (Autocomplete) — lỗi hiện trong lớp nổi, không toast (Autocomplete.md §Trạng thái). */
  timKiem(searchText: string, pageSize = 20): Observable<PagedList<VaiTro>> {
    const params = new HttpParams()
      .set('page', 1)
      .set('pageSize', pageSize)
      .set('searchText', searchText);
    return this.http
      .get<ApiResult<PagedList<VaiTroDto>>>(this.duongDan, { params, context: this.ctx })
      .pipe(
        map(unwrapData),
        map((trang) => ({ ...trang, items: trang.items.map(mapVaiTro) })),
      );
  }

  tao(name: string): Observable<{ id: string }> {
    return this.http
      .post<ApiResult<{ id: string }>>(this.duongDan, { name }, { context: this.ctx })
      .pipe(map(unwrapData));
  }

  /** `version` từ GET gần nhất; `data` là vai trò đã đổi, mang `version` MỚI (roles.md §3). */
  doiTen(id: string, name: string, version: string): Observable<VaiTro> {
    return this.http
      .put<ApiResult<VaiTroDto>>(`${this.duongDan}/${id}`, { name, version }, { context: this.ctx })
      .pipe(map(unwrapData), map(mapVaiTro));
  }

  /** Lỗi (`CORE.ROLE.IN_USE`) tự hiện trong `ConfirmDialog` — Design/Screens/11-vai-tro.md. */
  xoa(id: string): Observable<void> {
    return this.http
      .delete<ApiResult<null>>(`${this.duongDan}/${id}`, { context: this.ctx })
      .pipe(map(() => undefined));
  }
}
