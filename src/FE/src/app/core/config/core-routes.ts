import { EnvironmentProviders, InjectionToken, makeEnvironmentProviders } from '@angular/core';

/**
 * Seam — `core/` giữ luật (guard, interceptor), app cấp đường dẫn thật.
 * Guard chưa dựng ở F0 (thuộc F2); token khai ở đây vì `SessionExpiryHandler`
 * (F0, core/interceptors) đã cần điều hướng về màn đăng nhập khi hết phiên.
 */
export interface CoreRoutes {
  readonly dangNhap: string;
  readonly doiMatKhauBatBuoc: string;
  readonly khongCoQuyen: string;
  readonly sauDangNhap: string;
}

export const CORE_ROUTES = new InjectionToken<CoreRoutes>('CORE_ROUTES');

export function provideCoreRoutes(routes: CoreRoutes): EnvironmentProviders {
  return makeEnvironmentProviders([{ provide: CORE_ROUTES, useValue: routes }]);
}
