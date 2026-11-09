import { HttpErrorResponse } from '@angular/common/http';
import { ErrorHandler, Injectable, inject } from '@angular/core';

import { ApiFailureError } from '../http/api-result.model';
import { ClientErrorReporter } from './client-error-reporter';

/**
 * Lỗi HTTP đã được `errorInterceptor` xử lý (toast, form, hết phiên) — đếm thêm ở đây là đếm hai lần
 * và biến mỗi 400 hợp lệ thành một "lỗi FE" (10-observability.md §2, §7). `ApiFailureError` là lỗi
 * HTTP đã được interceptor dịch ra, cùng loại với `HttpErrorResponse`.
 */
function laLoiHttpDaXuLy(loi: unknown): boolean {
  return loi instanceof HttpErrorResponse || loi instanceof ApiFailureError;
}

/**
 * `ErrorHandler` của app (10-observability.md §3). Nhận mọi lỗi mà Angular bắt, và — nhờ
 * `provideBrowserGlobalErrorListeners()` trong `provideCoreObservability()` — cả sự kiện `error` và
 * `unhandledrejection` của cửa sổ.
 *
 * KHÔNG BAO GIỜ ném: một `ErrorHandler` ném lỗi là cách ngắn nhất để biến một lỗi thành vòng lặp.
 */
@Injectable()
export class GlobalErrorHandler implements ErrorHandler {
  private readonly reporter = inject(ClientErrorReporter);

  handleError(loi: unknown): void {
    try {
      if (!laLoiHttpDaXuLy(loi)) {
        this.reporter.capture(loi);
      }
      // Vẫn in ra: môi trường phát triển không được mất thông tin (10-observability.md §3, §8).
      console.error(loi);
    } catch {
      // Bọc kín (§4.1 ràng buộc 3): không có gì an toàn để làm ở đây, và ném tiếp là vòng lặp.
    }
  }
}
