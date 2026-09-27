import { AbstractControl, FormGroup } from '@angular/forms';
import { TranslateService } from '@ngx-translate/core';

import { ApiFailure, ApiFailureError } from '../../core/http/api-result.model';
import { dichLoi } from '../../core/http/dich-loi';
import { laLoiXuyenSuot } from '../../core/http/loi-xuyen-suot';
import { applyFieldErrors } from './apply-field-errors';
import { cauTungMa } from './field-errors-text';

/**
 * Bảng MÃ GỐC → TÊN CONTROL cho `applyFormFailure`: mã ở gốc envelope mà card KHÔNG khai
 * `fieldErrors` nhưng screen spec đặt dưới một ô (mã trùng — "FE tự gắn mã vào ô"). Một mã → một ô.
 * Tên control ràng theo kiểu của form: gõ sai tên là lỗi biên dịch, không phải lỗi im lặng.
 */
export type BangMaGocVaoO<F extends FormGroup = FormGroup> = Readonly<
  Record<string, Extract<keyof F['controls'], string>>
>;

/**
 * Định nghĩa gốc: quy-uoc/fe-ui-conventions.md §6.2. Gắn lỗi của một phản hồi thất bại vào form và
 * trả về CÂU cho khu lỗi chung của hộp/màn (`null` = không cần khu chung). Bọc `applyFieldErrors`
 * để một luật duy nhất, không lặp ở từng hộp:
 *
 * - Mọi mã trong `fieldErrors` đã hiện dưới một ô → `null`.
 * - Còn mã CHƯA hiện ở ô nào (khoá không khớp control) → câu của từng mã đó, đúng thứ tự BE gửi,
 *   không lặp mã đã hiện dưới ô, mỗi mã một dòng.
 * - `fieldErrors` không mang mã nào (vắng, `{}`, mọi danh sách rỗng) → đường lùi: câu của mã gốc.
 *
 * Mã gốc không nói thay mã con, với MỌI mã gốc: câu của `CORE.VALIDATION.FAILED` ("kiểm tra lại các
 * trường được đánh dấu") sai khi không ô nào được đánh dấu; câu của một mã gộp như
 * `CORE.TENANT.ADMIN_CREATE_FAILED` giấu mất lý do cụ thể nằm ở mã con.
 *
 * `maGocVaoO` (tuỳ chọn): mã gốc có trong bảng VÀ control tồn tại → câu của mã gốc (qua `dichLoi`:
 * kèm `messageParams`, thiếu khoá dịch thì lùi về câu BE gửi kèm) đứng đầu khoá `server` của ô đó,
 * và mã gốc không bao giờ lên khu chung — ở đường lùi, khu chung là `null`. Mã con vẫn theo ba
 * nhánh trên. Mã không có trong bảng, hoặc bảng trỏ vào control không có trong form → như không bảng.
 *
 * Lớp lỗi xuyên suốt (`laLoiXuyenSuot`: 5xx, 403 `CORE.AUTH.*` trừ `PASSWORD_CHANGE_REQUIRED`) →
 * `null` và form không bị đụng: interceptor đã toast nó kèm `traceId`, khu lỗi không hiện lần hai.
 */
export function applyFormFailure<F extends FormGroup>(
  form: F,
  loi: ApiFailureError,
  translate: TranslateService,
  maGocVaoO: BangMaGocVaoO<F> = {},
): string | null {
  if (laLoiXuyenSuot(loi.status, loi.body)) {
    return null;
  }
  const body = loi.body;
  const fieldErrors = body?.error.fieldErrors ?? {};
  const oMaGoc = timOMaGoc(form, body, maGocVaoO);
  const serverTruoc: unknown = oMaGoc?.errors?.['server'];

  const khongKhop = applyFieldErrors(form, fieldErrors, translate);

  if (oMaGoc) {
    // `applyFieldErrors` THAY khoá `server` của ô nào nó gắn mã → mảng khác mảng lúc trước nghĩa là
    // mã con của chính phản hồi này cho cùng ô: giữ, sau câu của mã gốc. Lỗi server của lần trước
    // thì bị thay, không cộng dồn.
    const serverSau: unknown = oMaGoc.errors?.['server'];
    const maConCungO = serverSau !== serverTruoc && Array.isArray(serverSau) ? serverSau : [];
    oMaGoc.setErrors({ ...oMaGoc.errors, server: [dichLoi(translate, body), ...maConCungO] });
    oMaGoc.markAsTouched();
  }

  if (Object.values(fieldErrors).every((loi) => loi.length === 0)) {
    return oMaGoc ? null : dichLoi(translate, body);
  }
  // `khongKhop` giữ thứ tự khoá BE gửi; khoá khớp mà mang danh sách rỗng góp 0 mã.
  return cauTungMa(
    translate,
    khongKhop.flatMap((khoa) => fieldErrors[khoa] ?? []),
  );
}

/** Ô nhận câu của mã gốc; `null` khi không phản hồi, mã ngoài bảng, hay form không có control đó. */
function timOMaGoc(
  form: FormGroup,
  body: ApiFailure | null,
  bang: Readonly<Record<string, string>>,
): AbstractControl | null {
  const ma = body?.error.code;
  return ma !== undefined && Object.hasOwn(bang, ma) ? form.get(bang[ma]) : null;
}
