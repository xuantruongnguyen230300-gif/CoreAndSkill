import { HttpClient, HttpContext } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { Observable, map, tap } from 'rxjs';

import { ApiResult, BO_QUA_TOAST_LOI } from '../http/api-result.model';
import { unwrapData } from '../http/unwrap';

/** Token XSRF giữ Ở BỘ NHỚ — không bao giờ `localStorage`/`sessionStorage` (fe-api-client.md §2.1, ràng buộc 2). */
@Injectable({ providedIn: 'root' })
export class XsrfTokenStore {
  private readonly http = inject(HttpClient);
  private readonly _token = signal<string | null>(null);
  readonly token = this._token.asReadonly();

  /**
   * Gọi lúc khởi động app, sau đăng nhập, sau đăng xuất — và một lần khi gặp CSRF_REJECTED.
   *
   * `boQuaToastLoi`: `errorInterceptor` truyền cờ `BO_QUA_TOAST_LOI` của REQUEST GỐC ở nhánh CSRF.
   * GET này đi qua chính interceptor, nên lỗi của nó được dịch ở lượt của GET — thiếu cờ thì màn tự
   * hiện lỗi (banner) mà toast vẫn bắn, một lỗi hiện hai lần (fe-ui-conventions.md §6.2 "Toast và
   * banner không đi cùng nhau"). Cờ không tắt lớp lỗi xuyên suốt (fe-api-client.md §2.2).
   */
  lamMoi(tuyChon: { readonly boQuaToastLoi?: boolean } = {}): Observable<string> {
    const context = new HttpContext().set(BO_QUA_TOAST_LOI, tuyChon.boQuaToastLoi ?? false);
    return this.http.get<ApiResult<{ token: string }>>('/core/antiforgery/token', { context }).pipe(
      map(unwrapData),
      map((d) => d.token),
      tap((t) => this._token.set(t)),
    );
  }
}
