import { HttpClient, HttpContext } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';

import { ApiResult, BO_QUA_TOAST_LOI } from '../../../core/http/api-result.model';
import { unwrapData } from '../../../core/http/unwrap';
import { CapNhatHoSoPayload, HoSoDto } from '../models/ho-so.dto';
import { HoSo } from '../models/ho-so.model';
import { mapHoSo } from './ho-so.mapper';

/** contracts/profile.md §1–§3. Cả ba request tắt toast mặc định — màn tự hiện lỗi. */
@Injectable({ providedIn: 'root' })
export class HoSoService {
  private readonly http = inject(HttpClient);
  private readonly duongDan = '/core/profile';
  private readonly ctx = new HttpContext().set(BO_QUA_TOAST_LOI, true);

  layHoSo(): Observable<HoSo> {
    return this.http
      .get<ApiResult<HoSoDto>>(this.duongDan, { context: this.ctx })
      .pipe(map(unwrapData), map(mapHoSo));
  }

  capNhatHoSo(payload: CapNhatHoSoPayload): Observable<HoSo> {
    return this.http
      .put<ApiResult<HoSoDto>>(this.duongDan, payload, { context: this.ctx })
      .pipe(map(unwrapData), map(mapHoSo));
  }

  tuBoCoDacQuyen(): Observable<void> {
    return this.http
      .post<ApiResult<null>>(`${this.duongDan}/renounce-permission-bypass`, {}, { context: this.ctx })
      .pipe(map(unwrapData), map(() => undefined));
  }
}
