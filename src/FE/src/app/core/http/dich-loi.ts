import { HttpErrorResponse } from '@angular/common/http';
import { TranslateService } from '@ngx-translate/core';

import { ApiFailure, ApiFailureError, MessageParams } from './api-result.model';
import { laLoiXuyenSuot } from './loi-xuyen-suot';

/**
 * Tra bảng dịch theo mã; đường lùi là câu BE gửi kèm — ba bậc của 08-i18n.md §5.4. Dùng chung bởi
 * `errorInterceptor` (toast) VÀ bởi các màn tự hiện lỗi ở khu lỗi riêng (đăng nhập, đổi mật khẩu,
 * hồ sơ) — một hàm, một chỗ, tránh hai bản dịch mã lỗi lệch nhau.
 */
export function dichLoi(
  translate: TranslateService,
  body: ApiFailure | null,
  thamSoThem: MessageParams = {},
): string {
  if (!body) {
    return translate.instant('loi.CORE.CLIENT.NO_CONNECTION');
  }
  const khoa = `loi.${body.error.code}`;
  const cau = translate.instant(khoa, { ...(body.error.messageParams ?? {}), ...thamSoThem });
  return cau === khoa ? body.error.message : cau;
}

/**
 * Câu cho chỗ hiện lỗi RIÊNG của màn gọi thẳng `dichLoi` — khu lỗi đăng nhập, toast màn tự bắn khi
 * request mang `BO_QUA_TOAST_LOI`: `null` với lớp lỗi xuyên suốt (`laLoiXuyenSuot`), vì interceptor
 * đã toast nó kèm `traceId`; mọi lỗi khác như `dichLoi`. Màn có form hoặc hộp dùng `applyFormFailure`
 * / `fieldErrorsText` ở `shared/forms/` — hai hàm đó cùng luật.
 */
export function dichLoiChoMan(translate: TranslateService, loi: ApiFailureError): string | null {
  return laLoiXuyenSuot(loi.status, loi.body) ? null : dichLoi(translate, loi.body);
}

/** Retry-After (giây) → tham số dịch cùng tên BE gửi kèm 429. Vắng hoặc sai dạng → rỗng. */
export function thamSoRetryAfter(err: HttpErrorResponse): MessageParams {
  const giay = Number.parseInt(err.headers.get('Retry-After') ?? '', 10);
  return Number.isInteger(giay) && giay > 0 ? { RetryAfterSeconds: String(giay) } : {};
}
