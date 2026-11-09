import {
  EnvironmentProviders,
  InjectionToken,
  Type,
  makeEnvironmentProviders,
} from '@angular/core';

import { ClientErrorReport } from './client-error-report';

/**
 * Đầu ra của `ClientErrorReporter` — nơi một báo cáo ĐÃ dựng, ĐÃ che, ĐÃ qua giới hạn tần suất đi tới
 * (10-observability.md §4). Interface tầm thường là có chủ đích: đổi đích sau này là đổi một provider,
 * không phải một đợt sửa rải khắp nơi.
 *
 * Hợp đồng cho mọi hiện thực:
 * - `send` KHÔNG ném và KHÔNG làm lỗi của việc gửi sinh thêm báo cáo (§4.1 ràng buộc 3). Reporter
 *   vẫn bọc kín một lớp nữa, nhưng đầu ra không dựa vào lớp đó.
 * - `traceIdTruoc`: các `traceId` gần đây ngoài cái đã nằm trong báo cáo, cũ nhất trước. Đây là SUY
 *   LUẬN XÁC SUẤT (§5.2), không phải quan hệ nhân quả — đầu ra nào hiển thị chúng phải nói rõ vậy.
 */
export interface ClientErrorOutput {
  send(baoCao: ClientErrorReport, traceIdTruoc: readonly string[]): void;
}

/**
 * Đầu ra mặc định là `console` (10-observability.md §10, §4): môi trường phát triển không bắn báo
 * cáo về server. Token có `factory` mặc định — khác các seam ở fe-architecture.md §2.5, vì ở đây
 * "không khai gì" có một nghĩa đúng và an toàn.
 */
export const CLIENT_ERROR_OUTPUT = new InjectionToken<ClientErrorOutput>('CLIENT_ERROR_OUTPUT', {
  providedIn: 'root',
  factory: () => new ConsoleClientErrorOutput(),
});

/**
 * In ra đúng bản báo cáo SẼ được gửi (đã che, đã cắt) — để người phát triển thấy nó sạch thế nào.
 * Lỗi gốc vẫn được `GlobalErrorHandler` in riêng bằng `console.error`; đầu ra này không thay việc đó.
 */
export class ConsoleClientErrorOutput implements ClientErrorOutput {
  send(baoCao: ClientErrorReport, traceIdTruoc: readonly string[]): void {
    const tieuDe = '[ClientErrorReporter] báo cáo lỗi (đầu ra console — không gửi đi đâu):';
    if (traceIdTruoc.length === 0) {
      console.warn(tieuDe, baoCao);
      return;
    }
    console.warn(
      tieuDe,
      baoCao,
      `traceId trước đó: ${traceIdTruoc.join(', ')} — chỉ là suy luận từ các request hoàn tất ` +
        'gần nhất, không chắc là nguyên nhân.',
    );
  }
}

/** Cắm đầu ra khác thay cho `console` — khai ở composition root. */
export function provideClientErrorOutput(output: Type<ClientErrorOutput>): EnvironmentProviders {
  return makeEnvironmentProviders([{ provide: CLIENT_ERROR_OUTPUT, useClass: output }]);
}
