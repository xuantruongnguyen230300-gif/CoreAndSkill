import { FormControl, ValidatorFn, Validators } from '@angular/forms';

import { fieldErrorText } from './field-error-text';
import { TranslateService } from '@ngx-translate/core';

/**
 * `fieldErrorText` dựng khoá `loi.CORE.CLIENT.VALIDATION_<KHOÁ LỖI VIẾT HOA>` (fe-ui-conventions.md
 * §5.3). Không có khoá trong `vi.json` thì người dùng thấy CHUỖI KHOÁ THÔ dưới ô — nên mỗi validator
 * mà platform/ dùng phải có khoá dịch tương ứng.
 *
 * Danh sách dưới đây là các validator `Validators.*` platform/ đang dùng. Spec KHÔNG tự phát hiện
 * validator mới (trình duyệt không đọc được mã nguồn) — thêm validator mới thì thêm một dòng ở đây.
 */
interface CaValidator {
  ten: string;
  validator: ValidatorFn;
  giaTriSai: string;
}

const VALIDATOR_DANG_DUNG: readonly CaValidator[] = [
  { ten: 'required', validator: Validators.required, giaTriSai: '' },
  { ten: 'email', validator: Validators.email, giaTriSai: 'abc' },
  { ten: 'maxLength', validator: Validators.maxLength(3), giaTriSai: 'abcd' },
  { ten: 'pattern', validator: Validators.pattern(/^\d+$/), giaTriSai: 'abc' },
  // Validator tự viết của repo (nhập lại mật khẩu, gõ lại mã đơn vị) — khoá lỗi `mismatch`.
  { ten: 'mismatch', validator: () => ({ mismatch: true }), giaTriSai: 'x' },
];

describe('khoá dịch của validator phía client trong vi.json', () => {
  let viJson: Record<string, unknown>;

  beforeAll(async () => {
    const res = await fetch('/i18n/vi.json');
    viJson = (await res.json()) as Record<string, unknown>;
  });

  function tra(khoa: string): unknown {
    return khoa
      .split('.')
      .reduce<unknown>(
        (nut, doan) =>
          typeof nut === 'object' && nut !== null
            ? (nut as Record<string, unknown>)[doan]
            : undefined,
        viJson,
      );
  }

  for (const { ten, validator, giaTriSai } of VALIDATOR_DANG_DUNG) {
    it(`Validators.${ten} → câu dịch có thật, không rơi ra chuỗi khoá thô`, () => {
      const control = new FormControl(giaTriSai, validator);
      control.markAsTouched();
      let khoaDaTra = '';
      const translate = {
        instant: (khoa: string) => {
          khoaDaTra = khoa;
          const cau = tra(khoa);
          return typeof cau === 'string' ? cau : khoa;
        },
      } as unknown as TranslateService;

      const ra = fieldErrorText(control, true, translate);

      expect(khoaDaTra).toMatch(/^loi\.CORE\.CLIENT\.VALIDATION_[A-Z]+$/);
      expect(ra)
        .withContext(`thiếu khoá ${khoaDaTra} trong public/i18n/vi.json`)
        .not.toBe(khoaDaTra);
      expect(ra?.length ?? 0).toBeGreaterThan(0);
    });
  }
});
