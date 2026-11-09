import { HttpErrorResponse } from '@angular/common/http';
import { TranslateService } from '@ngx-translate/core';

import { ApiFailure, ApiFailureError, MessageParams, maLoiKhongEnvelope } from './api-result.model';
import { laLoiXuyenSuot } from './loi-xuyen-suot';

/**
 * Câu dự phòng chung (bậc 3, 08-i18n.md §5.4): mã lạ và BE không gửi câu nào. Là mã gốc phía FE
 * `CORE.CLIENT.UNKNOWN_ERROR_CODE` (be-cqrs-handler.md §7.4), không mượn mã `CORE.SYSTEM.*` của BE.
 */
const KHOA_DU_PHONG = 'loi.CORE.CLIENT.UNKNOWN_ERROR_CODE';

/**
 * Tra bảng dịch theo mã; đường lùi là câu BE gửi kèm — ba bậc của 08-i18n.md §5.4. Dùng chung bởi
 * `errorInterceptor` (toast) VÀ bởi các màn tự hiện lỗi ở khu lỗi riêng (đăng nhập, đổi mật khẩu,
 * hồ sơ) — một hàm, một chỗ, tránh hai bản dịch mã lỗi lệch nhau.
 *
 * `status` BẮT BUỘC, không có giá trị mặc định: một lời gọi quên truyền phải lỗi biên dịch, không
 * được âm thầm rơi về `0` rồi hiện câu mất kết nối cho một phản hồi máy chủ THẬT (luật F45).
 * `body` là `null` (phản hồi không mang envelope) thì mã chọn theo `maLoiKhongEnvelope(status)`
 * (be-cqrs-handler.md §7.4); câu của `CORE.CLIENT.INVALID_RESPONSE` nhận thêm tham số `maHttp`.
 */
export function dichLoi(
  translate: TranslateService,
  body: ApiFailure | null,
  status: number,
  thamSoThem: MessageParams = {},
): string {
  if (!body) {
    const ma = maLoiKhongEnvelope(status);
    const thamSo =
      ma === 'CORE.CLIENT.INVALID_RESPONSE'
        ? { maHttp: String(status), ...thamSoThem }
        : thamSoThem;
    return translate.instant(`loi.${ma}`, thamSo);
  }
  const khoa = `loi.${body.error.code}`;
  const cau = translate.instant(khoa, { ...(body.error.messageParams ?? {}), ...thamSoThem });
  if (cau !== khoa) {
    return cau;
  }
  // Bậc 2 chỉ khi BE gửi câu thật; rỗng/chỉ khoảng trắng rơi xuống bậc 3 (08-i18n.md §5.4).
  const cauBe = body.error.message;
  return typeof cauBe === 'string' && cauBe.trim() !== ''
    ? cauBe
    : translate.instant(KHOA_DU_PHONG);
}

/**
 * Câu cho chỗ hiện lỗi RIÊNG của màn gọi thẳng `dichLoi` — khu lỗi đăng nhập, toast màn tự bắn khi
 * request mang `BO_QUA_TOAST_LOI`: `null` với lớp lỗi xuyên suốt (`laLoiXuyenSuot`), vì interceptor
 * đã toast nó kèm `traceId`; mọi lỗi khác như `dichLoi`. Màn có form hoặc hộp dùng `applyFormFailure`
 * / `fieldErrorsText` ở `shared/forms/` — hai hàm đó cùng luật.
 */
export function dichLoiChoMan(translate: TranslateService, loi: ApiFailureError): string | null {
  return laLoiXuyenSuot(loi.status, loi.body) ? null : dichLoi(translate, loi.body, loi.status);
}

/**
 * Câu cho lỗi KHÔNG phải `ApiFailureError` (timeout, lỗi JS — không có phản hồi HTTP nào): luôn là câu
 * của mã `maLoiKhongEnvelope(0)`. Chỗ DUY NHẤT ngoài `dichLoi` tra khoá `loi.CORE.CLIENT.*` (luật F45).
 */
export function cauLoiKhongPhaiApi(translate: TranslateService): string {
  return translate.instant(`loi.${maLoiKhongEnvelope(0)}`);
}

/**
 * Câu cho một lỗi bất kỳ của `subscribe`: `ApiFailureError` thì đi đường dịch theo envelope/status
 * (`dichApi`, mặc định `dichLoiChoMan`; màn có khu lỗi theo `fieldErrorsText` truyền hàm đó vào),
 * mọi lỗi khác thì `cauLoiKhongPhaiApi`. `null` khi `dichApi` trả `null` (lớp lỗi xuyên suốt).
 */
export function dichLoiBatKy(
  translate: TranslateService,
  loi: unknown,
  dichApi: (loi: ApiFailureError) => string | null = (l) => dichLoiChoMan(translate, l),
): string | null {
  return loi instanceof ApiFailureError ? dichApi(loi) : cauLoiKhongPhaiApi(translate);
}

/** Retry-After (giây) → tham số dịch cùng tên BE gửi kèm 429. Vắng hoặc sai dạng → rỗng. */
export function thamSoRetryAfter(err: HttpErrorResponse): MessageParams {
  const giay = Number.parseInt(err.headers.get('Retry-After') ?? '', 10);
  return Number.isInteger(giay) && giay > 0 ? { RetryAfterSeconds: String(giay) } : {};
}
