import { isDevMode } from '@angular/core';
import { FormGroup } from '@angular/forms';
import { TranslateService } from '@ngx-translate/core';

import { ApiFieldError } from '../../core/http/api-result.model';

/**
 * Định nghĩa gốc: quy-uoc/fe-ui-conventions.md §6.2, hiện thực đầy đủ ở wiki-core/fe/09-forms-validation.md §4.1.
 * NƠI DUY NHẤT gắn `fieldErrors` vào form — page gọi hàm này, không viết lại vòng lặp riêng.
 *
 * Trả về danh sách khoá BE KHÔNG dùng được — không khớp control nào, HOẶC khớp nhưng mang danh sách
 * rỗng (không có câu nào để hiện; đặt `server: []` lên control sẽ làm nó invalid mà không hiện gì).
 * Đây là điểm phát hiện lệch hợp đồng (§4.3); page gộp chúng vào thông báo lỗi chung, KHÔNG được
 * nuốt im lặng.
 */
export function applyFieldErrors(
  form: FormGroup,
  fieldErrors: Readonly<Record<string, readonly ApiFieldError[]>> | null | undefined,
  translate: TranslateService,
): string[] {
  if (!fieldErrors) return [];
  const khongKhop: string[] = [];

  for (const [khoaBE, loi] of Object.entries(fieldErrors)) {
    const control = form.get(tenControlTuKhoaBE(khoaBE));
    // Khoá lạ, hoặc khoá khớp nhưng không có câu nào: không đặt gì lên control.
    if (!control || loi.length === 0) {
      khongKhop.push(khoaBE);
      continue;
    }
    const cau = loi.map((l) => translate.instant(`loi.${l.code}`, l.messageParams ?? {}) as string);
    control.setErrors({ ...(control.errors ?? {}), server: cau });
    control.markAsTouched();
  }
  // §4.3 — khoá không khớp KHÔNG được biến mất im lặng: trang gộp vào thông báo chung (giá trị trả
  // về), và ở đây ghi log môi trường phát triển để lập trình viên thấy ngay lúc đang code.
  if (khongKhop.length > 0 && isDevMode()) {
    console.warn(
      '[applyFieldErrors] Khoá fieldErrors không khớp control nào, hoặc mang danh sách rỗng:',
      khongKhop,
    );
  }
  return khongKhop;
}

/** Khoá BE là PascalCase (be-api-controller.md §2.3); tên control là camelCase — MỘT bước chuyển, ở đây. */
function tenControlTuKhoaBE(khoaBE: string): string {
  return khoaBE
    .split('.')
    .map((doan) => (doan.length > 0 ? doan.charAt(0).toLowerCase() + doan.slice(1) : doan))
    .join('.');
}
