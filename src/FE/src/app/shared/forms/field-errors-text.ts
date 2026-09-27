import { TranslateService } from '@ngx-translate/core';

import { ApiFailureError, ApiFieldError } from '../../core/http/api-result.model';
import { dichLoi } from '../../core/http/dich-loi';
import { laLoiXuyenSuot } from '../../core/http/loi-xuyen-suot';

/**
 * Câu cho khu lỗi chung của một hộp KHÔNG có form (không control nào để `applyFormFailure` gắn
 * `fieldErrors` vào — ví dụ hộp "Gán vai trò", nơi payload là một tập id chứ không phải ô nhập).
 * Cùng luật với `applyFormFailure` (fe-ui-conventions.md §6.2): không ô nào hiện được mã nào, nên
 * câu của MỌI mã trong `fieldErrors` — đúng thứ tự BE gửi, kèm `messageParams`, mỗi mã một dòng;
 * `fieldErrors` không mang mã nào thì đường lùi là câu của mã gốc. Lớp lỗi xuyên suốt
 * (`laLoiXuyenSuot`) → `null`: interceptor đã toast nó kèm `traceId`.
 *
 * Không dùng cho hộp có form: ở đó lỗi phải xuống từng ô, và `applyFormFailure` là hàm duy nhất.
 */
export function fieldErrorsText(translate: TranslateService, loi: ApiFailureError): string | null {
  if (laLoiXuyenSuot(loi.status, loi.body)) {
    return null;
  }
  const cau = cauTungMa(translate, Object.values(loi.body?.error.fieldErrors ?? {}).flat());
  return cau ?? dichLoi(translate, loi.body);
}

/**
 * Câu của từng mã, đúng thứ tự nhận, mỗi mã một dòng (`\n` — chỗ hiện giữ ngắt dòng); `null` khi
 * không có mã nào. Nơi DUY NHẤT nối câu của mã con cho khu lỗi chung — `applyFormFailure` dùng lại.
 */
export function cauTungMa(
  translate: TranslateService,
  loi: readonly ApiFieldError[],
): string | null {
  return loi.length > 0
    ? loi.map((l) => translate.instant(`loi.${l.code}`, l.messageParams ?? {}) as string).join('\n')
    : null;
}
