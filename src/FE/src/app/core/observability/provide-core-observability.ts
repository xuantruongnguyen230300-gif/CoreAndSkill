import {
  EnvironmentProviders,
  ErrorHandler,
  makeEnvironmentProviders,
  provideBrowserGlobalErrorListeners,
  provideEnvironmentInitializer,
} from '@angular/core';

import { GlobalErrorHandler } from './global-error-handler';
import { nghePhanTuTaiHong } from './resource-error-listener';

/**
 * Nối trọn bộ thu lỗi runtime (10-observability.md §3) — composition root chỉ gọi hàm này:
 * 1. `ErrorHandler` = `GlobalErrorHandler`.
 * 2. `provideBrowserGlobalErrorListeners()` đưa sự kiện `error` và `unhandledrejection` của cửa sổ
 *    vào `ErrorHandler` — hai nguồn đầu của bảng §3 (promise bị từ chối, lỗi ngoài vùng Angular).
 *    Không nghe lại hai sự kiện đó ở đây: nghe hai lần là báo hai lần.
 * 3. Bộ nghe capture cho tài nguyên tải hỏng — nguồn thứ ba, thứ mà hai bộ nghe trên không thấy.
 *
 * Đầu ra mặc định là `console`; đổi bằng `provideClientErrorOutput(…)` (client-error-output.ts).
 */
export function provideCoreObservability(): EnvironmentProviders {
  return makeEnvironmentProviders([
    { provide: ErrorHandler, useClass: GlobalErrorHandler },
    provideBrowserGlobalErrorListeners(),
    provideEnvironmentInitializer(nghePhanTuTaiHong),
  ]);
}
