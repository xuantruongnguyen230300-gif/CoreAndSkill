import { HttpClient, HttpContext } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';

import { ApiResult, BO_QUA_TOAST_LOI } from '../http/api-result.model';
import { unwrapData } from '../http/unwrap';
import { DoiMatKhauPayload } from './doi-mat-khau.dto';

/**
 * Hai endpoint đổi mật khẩu (contracts/auth.md §6, §7) — dùng chung bởi màn đổi mật khẩu bắt buộc
 * (`platform/xac-thuc`) và màn hồ sơ cá nhân (`platform/ho-so`). Đặt ở `core/auth/` vì cả hai
 * đều là mối quan tâm xác thực dùng chung giữa hai feature Core, không thuộc riêng feature nào.
 *
 * Cả hai request đều tắt toast mặc định — trang tự hiện lỗi vào đúng ô hoặc khu lỗi của mình.
 */
@Injectable({ providedIn: 'root' })
export class DoiMatKhauService {
  private readonly http = inject(HttpClient);
  private readonly ctx = new HttpContext().set(BO_QUA_TOAST_LOI, true);

  /** `POST /core/auth/change-password-required` — đường thoát duy nhất khỏi cờ buộc đổi (auth.md §7). */
  doiBatBuoc(payload: DoiMatKhauPayload): Observable<void> {
    return this.http
      .post<ApiResult<null>>('/core/auth/change-password-required', payload, { context: this.ctx })
      .pipe(
        map(unwrapData),
        map(() => undefined),
      );
  }

  /** `POST /core/auth/change-password` — đổi tự nguyện, dùng khi KHÔNG bị buộc (auth.md §6). */
  doiTuNguyen(payload: DoiMatKhauPayload): Observable<void> {
    return this.http
      .post<ApiResult<null>>('/core/auth/change-password', payload, { context: this.ctx })
      .pipe(
        map(unwrapData),
        map(() => undefined),
      );
  }
}
