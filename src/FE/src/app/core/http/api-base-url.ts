import { InjectionToken } from '@angular/core';

/**
 * Origin của API cộng tiền tố gốc và phiên bản: `https://<api>/api/v1` — cấp từ
 * `environment.apiBaseUrl` (nhúng lúc build, fe-api-client.md §2.1). `authInterceptor` ghép
 * giá trị này vào trước mọi đường dẫn TƯƠNG ĐỐI mà service viết.
 */
export const API_BASE_URL = new InjectionToken<string>('API_BASE_URL');
