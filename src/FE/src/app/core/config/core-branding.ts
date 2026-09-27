import { EnvironmentProviders, InjectionToken, makeEnvironmentProviders } from '@angular/core';

/**
 * Seam — `core/` không được biết tên sản phẩm của dự án (fe-architecture.md §2.5).
 * `name` dùng làm hậu tố `<title>` (CoreTitleStrategy) và làm vùng thương hiệu của `AuthCard`
 * (Design/Screens/01-dang-nhap.md — "Thương hiệu — dữ liệu seam CORE_BRANDING; mặc định của
 * Core là tên sản phẩm dạng chữ, không có ảnh logo"). `shortName` dành cho ô vuông thương hiệu
 * thu gọn (Sidebar) — F2 chưa có sidebar thu gọn nên chưa tiêu thụ, nhưng token đã cấp sẵn.
 */
export interface CoreBranding {
  readonly name: string;
  readonly shortName: string;
}

export const CORE_BRANDING = new InjectionToken<CoreBranding>('CORE_BRANDING');

export function provideCoreBranding(branding: CoreBranding): EnvironmentProviders {
  return makeEnvironmentProviders([{ provide: CORE_BRANDING, useValue: branding }]);
}
