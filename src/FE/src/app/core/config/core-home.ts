import {
  EnvironmentProviders,
  InjectionToken,
  Type,
  makeEnvironmentProviders,
} from '@angular/core';

/**
 * Seam trang chủ — fe-architecture.md §2.5, ADR-0057. Dự án cấp HÀM NẠP LƯỜI component trang chủ
 * của mình (cùng hình dạng với `loadComponent`), không cấp `Type`: `Type` buộc cấu hình app import
 * tĩnh component đó, kéo bảng tổng hợp cùng thư viện biểu đồ vào bundle khởi động (ngân sách F14,
 * luật lazy-load F20). Route trang chủ của Core đọc token ngay trong `loadComponent`
 * (`platform/trang-chu/trang-chu.routes.ts`); không khai ⇒ trang chào của Core.
 *
 * KHÔNG có giá trị mặc định ở token (cùng lý do với `CORE_BRANDING`), nhưng route đọc bằng
 * `{ optional: true }`: trang chào của Core không mang tên sản phẩm nào sai — ly-do §2.5.
 */
export type CoreHomeLoader = () => Promise<Type<unknown>>;

export const CORE_HOME = new InjectionToken<CoreHomeLoader>('CORE_HOME');

export function provideCoreHome(load: CoreHomeLoader): EnvironmentProviders {
  return makeEnvironmentProviders([{ provide: CORE_HOME, useValue: load }]);
}
