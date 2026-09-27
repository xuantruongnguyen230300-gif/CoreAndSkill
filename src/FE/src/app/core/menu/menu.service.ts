import { HttpClient, HttpContext } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';

import { ApiResult, BO_QUA_TOAST_LOI } from '../http/api-result.model';
import { unwrapData } from '../http/unwrap';
import { MenuItemDto } from './menu.dto';
import { dungCayMenu } from './menu.mapper';
import { MenuNode } from './menu.model';

/**
 * Đường dẫn NGẮN — `authInterceptor` ghép base URL (luật F19).
 *
 * Service là nơi DUY NHẤT thấy `MenuItemDto`: bóc envelope rồi map sang cây `MenuNode` ngay tại đây
 * (fe-api-client.md §4.1, luật F10) — store và shell chỉ nhận model.
 *
 * Tắt toast cho lỗi của màn: menu hỏng thì `Sidebar` tự vào trạng thái `error` kèm Thử lại
 * (Design/Screens/00-khung-ung-dung.md, mục Trạng thái → lỗi). Cờ không tắt được lớp lỗi xuyên suốt:
 * 5xx và 403 `CORE.AUTH.*` interceptor vẫn toast kèm traceId (ADR-0094).
 */
@Injectable({ providedIn: 'root' })
export class MenuService {
  private readonly http = inject(HttpClient);
  private readonly ctx = new HttpContext().set(BO_QUA_TOAST_LOI, true);

  layMenu(): Observable<readonly MenuNode[]> {
    return this.http
      .get<ApiResult<readonly MenuItemDto[]>>('/core/meta/menu', { context: this.ctx })
      .pipe(map(unwrapData), map(dungCayMenu));
  }
}
