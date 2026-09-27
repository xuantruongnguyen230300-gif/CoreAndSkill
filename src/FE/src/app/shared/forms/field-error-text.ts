import { AbstractControl } from '@angular/forms';
import { TranslateService } from '@ngx-translate/core';

/**
 * Định nghĩa gốc: quy-uoc/fe-ui-conventions.md §6.5. Quyết định lỗi hiện LÚC NÀO và CÂU NÀO cho
 * một ô — page gọi qua một dòng uỷ quyền, không viết lại điều kiện hiện lỗi.
 */
export function fieldErrorText(
  control: AbstractControl,
  formSubmitted: boolean,
  translate: TranslateService,
): string | null {
  const errors = control.errors;
  if (errors === null || (!control.touched && !formSubmitted)) {
    return null;
  }

  // Lỗi server: mảng câu ĐÃ dịch, đúng thứ tự BE trả; hiện câu đầu.
  const server: unknown = errors['server'];
  if (Array.isArray(server) && typeof server[0] === 'string') {
    return server[0];
  }

  // Lỗi validator client: khoá đầu tiên theo thứ tự validator khai ở control.
  const validator = Object.keys(errors).find((khoa) => khoa !== 'server');
  if (validator === undefined) {
    return null;
  }
  const chiTiet: unknown = errors[validator];
  return translate.instant(
    `loi.CORE.CLIENT.VALIDATION_${validator.toUpperCase()}`,
    laThamSo(chiTiet) ? chiTiet : undefined,
  );
}

/** `required` gắn `true`, `maxlength` gắn object — chỉ object mới là tham số dịch. */
function laThamSo(giaTri: unknown): giaTri is Record<string, unknown> {
  return typeof giaTri === 'object' && giaTri !== null;
}
