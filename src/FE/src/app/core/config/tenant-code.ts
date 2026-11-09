import { InjectionToken } from '@angular/core';

/**
 * Mã đơn vị nghiệp vụ của bản cài — cấp từ `environment.tenantCode` (nhúng lúc build,
 * fe-architecture.md §1.1, ADR-0116). Trang đăng nhập ở chế độ thường gửi giá trị này làm
 * `tenantCode`; mã đơn vị hệ thống KHÔNG nằm ở đây (người vận hành gõ tay).
 */
export const TENANT_CODE = new InjectionToken<string>('TENANT_CODE');
