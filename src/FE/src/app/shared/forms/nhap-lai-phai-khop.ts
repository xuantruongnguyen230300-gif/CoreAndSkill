import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

/**
 * Validator CẤP GROUP: ô `tenNhapLai` phải khớp ô `tenGoc` — "nhập lại mật khẩu mới"
 * (quy-uoc/fe-ui-conventions.md §6.3: so sánh giữa các field trong cùng form, kiểm cả ở client).
 * Gắn vào group, không vào ô: phép so đọc HAI ô, và chỉ group chạy lại ở mọi lần đổi giá trị của
 * cả hai — người dùng gõ ô nào trước cũng được tính.
 *
 * Lỗi KHÔNG nằm trên group mà trên CHÍNH ô nhập lại, khoá `mismatch`: mọi chỗ hiện lỗi đọc control —
 * `fieldErrorText` dựng câu `loi.CORE.CLIENT.VALIDATION_MISMATCH` cho dòng lỗi dưới ô, viền lỗi và
 * `aria-invalid` của ô suy từ `invalid && touched`. Group luôn nhận `null`.
 *
 * - Ô nhập lại rỗng → không `mismatch`; `required` của ô nói thay.
 * - Lỗi khác đang có trên ô nhập lại (vd `server`) được giữ; `mismatch` đứng sau chúng.
 * - Tên ô không có trong group → ném lỗi ngay lúc dựng form (group chạy validator trong constructor),
 *   không để phép kiểm chết im lặng.
 *
 * KHÔNG dùng cho ô "gõ lại mã đơn vị": ô đó so với dữ liệu của hàng, không phân biệt hoa thường — luật khác.
 */
export function nhapLaiPhaiKhop(tenGoc: string, tenNhapLai: string): ValidatorFn {
  return (group: AbstractControl): null => {
    const goc = group.get(tenGoc);
    const nhapLai = group.get(tenNhapLai);
    if (goc === null || nhapLai === null) {
      throw new Error(
        `[nhapLaiPhaiKhop] Group không có control "${goc === null ? tenGoc : tenNhapLai}"`,
      );
    }

    const conLai: ValidationErrors = { ...nhapLai.errors };
    delete conLai['mismatch'];
    if (nhapLai.value !== '' && nhapLai.value !== goc.value) {
      nhapLai.setErrors({ ...conLai, mismatch: true });
    } else {
      nhapLai.setErrors(Object.keys(conLai).length > 0 ? conLai : null);
    }
    return null;
  };
}
