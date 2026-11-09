import { HttpBackend, HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, Injector, inject } from '@angular/core';
import { timeout } from 'rxjs';

import { XsrfTokenStore } from '../auth/xsrf-token.store';
import { API_BASE_URL } from '../http/api-base-url';
import { ClientErrorOutput } from './client-error-output';
import { ClientErrorReport } from './client-error-report';

/** Đường dẫn sau `API_BASE_URL` — docs/contracts/client-errors.md §1. */
const DUONG_DAN_BAO_LOI = '/core/client-errors';

/** Gọi rồi quên: không đợi lâu hơn thế một báo cáo mà người dùng không đợi kết quả. */
const TIMEOUT_GUI_BAO_CAO_MS = 5_000;

/**
 * BE đòi `Retry-After` kèm 429 (client-errors.md §1). Header không đọc được (CORS không phơi nó) hay
 * không phải số thì nghỉ đúng một cửa sổ của BE; số quá lớn bị chặn trần để một giá trị lạ không
 * tắt báo lỗi cả buổi.
 */
const NGHI_MAC_DINH_S = 60;
const NGHI_TOI_DA_S = 600;

/**
 * Đầu ra gửi báo cáo về BE của chính mình — `POST /api/v1/core/client-errors`
 * (10-observability.md §4, quyết định v1 ngày 2026-09-10). Cắm bằng
 * `provideClientErrorOutput(HttpClientErrorOutput)`.
 *
 * Endpoint là best-effort (client-errors.md, Ghi chú) nên lớp này làm đúng bốn điều:
 * 1. **Đi vòng chuỗi interceptor**, qua `HttpBackend` như bộ nạp tệp dịch. Đây là chỗ chặn vòng lặp:
 *    lỗi của việc gửi không đi qua `errorInterceptor` (không toast, không xử lý 401/hết phiên, không
 *    lấy token CSRF rồi gửi lại) và không bao giờ quay về `ErrorHandler`. Vì đi vòng nên lớp này tự
 *    làm phần việc của `authInterceptor`: ghép base URL, `withCredentials`, header `X-XSRF-TOKEN`
 *    (luật F59).
 * 2. **Chưa có token CSRF thì bỏ báo cáo**, không xin token — CSRF vẫn áp như mọi `POST` và FE không
 *    được xin token chỉ để báo lỗi.
 * 3. **Không thử lại.** Thất bại nào cũng nuốt, bằng `error` handler tường minh — nên RxJS không
 *    chuyển nó thành lỗi chưa bắt (thứ sẽ quay lại `ErrorHandler`).
 * 4. **429 thì dừng gửi** tới hết `Retry-After` (luồng N8 §4): thử lại ngay biến hạn mức thành vòng lặp.
 */
@Injectable()
export class HttpClientErrorOutput implements ClientErrorOutput {
  private readonly http = new HttpClient(inject(HttpBackend));
  private readonly baseUrl = inject(API_BASE_URL);
  // Lấy lúc gửi, không lúc dựng: đầu ra này được dựng từ trong `errorInterceptor` (qua reporter) và
  // `XsrfTokenStore` lại cần `HttpClient` — lấy muộn thì không có chuỗi phụ thuộc nào để vòng lại.
  private readonly injector = inject(Injector);

  private nghiDen = 0;

  send(baoCao: ClientErrorReport): void {
    if (Date.now() < this.nghiDen) {
      return;
    }
    const token = this.injector.get(XsrfTokenStore).token();
    if (token === null) {
      return;
    }
    this.http
      .post(`${this.baseUrl}${DUONG_DAN_BAO_LOI}`, thanGui(baoCao), {
        withCredentials: true,
        headers: { 'X-XSRF-TOKEN': token },
      })
      .pipe(timeout(TIMEOUT_GUI_BAO_CAO_MS))
      .subscribe({ error: (loi: unknown) => this.nghiNeuBiSiet(loi) });
  }

  private nghiNeuBiSiet(loi: unknown): void {
    if (!(loi instanceof HttpErrorResponse) || loi.status !== 429) {
      return;
    }
    const giay = Number.parseInt(loi.headers.get('Retry-After') ?? '', 10);
    const nghiS = Number.isInteger(giay) && giay > 0 ? giay : NGHI_MAC_DINH_S;
    this.nghiDen = Date.now() + Math.min(nghiS, NGHI_TOI_DA_S) * 1000;
  }
}

/**
 * Thân request: đúng sáu field của hợp đồng, không hơn. Dựng tường minh từng field để một field
 * thêm vào `ClientErrorReport` sau này không tự động đi lên server mà chưa qua hợp đồng.
 */
function thanGui(baoCao: ClientErrorReport): Record<string, string> {
  return {
    kind: baoCao.kind,
    message: baoCao.message,
    ...(baoCao.stack === undefined ? {} : { stack: baoCao.stack }),
    duongDan: baoCao.duongDan,
    ...(baoCao.traceId === undefined ? {} : { traceId: baoCao.traceId }),
    phienBanApp: baoCao.phienBanApp,
  };
}
