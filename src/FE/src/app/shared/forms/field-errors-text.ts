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
  return cau ?? dichLoi(translate, loi.body, loi.status);
}

/**
 * Câu của từng mã, đúng thứ tự nhận, mỗi mã một dòng (`\n` — chỗ hiện giữ ngắt dòng); `null` khi
 * không có mã nào. Nơi DUY NHẤT nối câu của mã con cho khu lỗi chung — `applyFormFailure` dùng lại.
 */
export function cauTungMa(
  translate: TranslateService,
  loi: readonly ApiFieldError[],
): string | null {
  return loi.length > 0 ? loi.map((l) => cauMaCon(translate, l)).join('\n') : null;
}

/** Khoá câu dự phòng khi mã trong `fieldErrors` chưa có khoá dịch (08-i18n.md §5.4 bậc 3). */
const KHOA_CAU_DU_PHONG = 'loi.CORE.VALIDATION.INVALID';

/**
 * Câu của MỘT mã trong `fieldErrors`. Mã chưa có khoá dịch thì `instant` trả lại nguyên chuỗi khoá —
 * không bao giờ để chuỗi đó hiện cho người dùng: thay bằng câu dự phòng chung. Nơi DUY NHẤT dịch mã con.
 */
export function cauMaCon(translate: TranslateService, l: ApiFieldError): string {
  const khoa = `loi.${l.code}`;
  const cau = translate.instant(khoa, l.messageParams ?? {}) as string;
  if (cau !== khoa) return cau;
  // Bộ chuỗi chưa nạp (không có cả câu dự phòng) thì giữ nguyên kết quả — không bịa câu.
  const duPhong = translate.instant(KHOA_CAU_DU_PHONG) as string;
  return duPhong === KHOA_CAU_DU_PHONG ? cau : duPhong;
}
