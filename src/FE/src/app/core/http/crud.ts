import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, map } from 'rxjs';

import { ApiResult } from './api-result.model';
import { unwrapData } from './unwrap';
import { PagedList, PageQuery } from './paged.model';

/**
 * Định nghĩa gốc: quy-uoc/fe-api-client.md §5.2. Hàm THUẦN, không class, không kế thừa — service
 * của feature GỌI, không kế thừa base class (core/ không cấp base class, 01-core-components.md §9).
 */
export function danhSachTrang<TDto, TModel>(
  http: HttpClient,
  duongDan: string,
  query: PageQuery,
  map1: (dto: TDto) => TModel,
): Observable<PagedList<TModel>> {
  return http
    .get<ApiResult<PagedList<TDto>>>(duongDan, { params: toHttpParams(query) })
    .pipe(map(unwrapData), map((trang) => ({ ...trang, items: trang.items.map(map1) })));
}

export function theoId<TDto, TModel>(
  http: HttpClient,
  duongDan: string,
  id: string,
  map1: (dto: TDto) => TModel,
): Observable<TModel> {
  return http.get<ApiResult<TDto>>(`${duongDan}/${id}`).pipe(map(unwrapData), map(map1));
}

/** Trải truy vấn thành query string: tên dây giữ nguyên, bộ lọc thành tham số rời, bỏ ô trống. */
function toHttpParams(query: PageQuery): HttpParams {
  const { filters, ...chung } = query;
  let params = new HttpParams();
  for (const [ten, giaTri] of Object.entries({ ...filters, ...chung })) {
    if (giaTri !== undefined && giaTri !== '') {
      params = params.set(ten, String(giaTri));
    }
  }
  return params;
}
