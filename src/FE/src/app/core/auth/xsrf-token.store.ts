import { Injectable, inject, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';

import { XsrfTokenService } from './xsrf-token.service';

/**
 * Token XSRF giữ Ở BỘ NHỚ — không bao giờ `localStorage`/`sessionStorage` (fe-api-client.md §2.1, ràng
 * buộc 2). Store chỉ giữ state; lời gọi HTTP nằm ở `XsrfTokenService` (03-state-management.md §4.1).
 */
@Injectable({ providedIn: 'root' })
export class XsrfTokenStore {
  private readonly service = inject(XsrfTokenService);
  private readonly _token = signal<string | null>(null);
  readonly token = this._token.asReadonly();

  /**
   * Gọi lúc khởi động app, sau đăng nhập, sau đăng xuất — và một lần khi gặp CSRF_REJECTED.
   * `boQuaToastLoi` chuyền nguyên cho `XsrfTokenService.layToken` (lý do cờ: ở đó).
   */
  lamMoi(tuyChon: { readonly boQuaToastLoi?: boolean } = {}): Observable<string> {
    return this.service
      .layToken(tuyChon.boQuaToastLoi ?? false)
      .pipe(tap((t) => this._token.set(t)));
  }
}
