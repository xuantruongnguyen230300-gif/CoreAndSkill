import { HttpClient, HttpContext } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';

import { ApiResult, BO_QUA_TOAST_LOI } from '../http/api-result.model';
import { unwrapData } from '../http/unwrap';

/**
 * Phần gọi HTTP của token XSRF — tách khỏi `XsrfTokenStore` để store chỉ giữ state (store không gọi
 * `HttpClient` trực tiếp: wiki-core/fe/03-state-management.md §4.1 quy tắc 2). Không giữ token, không
 * biết ai dùng nó; nơi giữ là `XsrfTokenStore` (fe-api-client.md §2.1).
 */
@Injectable({ providedIn: 'root' })
export class XsrfTokenService {
  private readonly http = inject(HttpClient);

  /**
   * `GET /core/antiforgery/token`, trả về chuỗi token đã bóc envelope.
   *
   * `boQuaToastLoi`: cờ `BO_QUA_TOAST_LOI` của request này. GET đi qua chính interceptor nên lỗi của nó
   * được dịch ở lượt của GET — `errorInterceptor` truyền cờ của REQUEST GỐC ở nhánh CSRF; thiếu cờ thì
   * màn tự hiện lỗi (banner) mà toast vẫn bắn, một lỗi hiện hai lần (fe-ui-conventions.md §6.2 "Toast và
   * banner không đi cùng nhau"). Cờ không tắt lớp lỗi xuyên suốt (fe-api-client.md §2.2).
   */
  layToken(boQuaToastLoi: boolean): Observable<string> {
    const context = new HttpContext().set(BO_QUA_TOAST_LOI, boQuaToastLoi);
    return this.http.get<ApiResult<{ token: string }>>('/core/antiforgery/token', { context }).pipe(
      map(unwrapData),
      map((d) => d.token),
    );
  }
}
