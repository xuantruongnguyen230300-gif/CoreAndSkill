import { FormControl, FormGroup, Validators } from '@angular/forms';
import { TranslateService } from '@ngx-translate/core';

import { fieldErrorText } from './field-error-text';
import { nhapLaiPhaiKhop } from './nhap-lai-phai-khop';

function dungForm(): FormGroup<{ moi: FormControl<string>; nhapLai: FormControl<string> }> {
  return new FormGroup(
    {
      moi: new FormControl('', { nonNullable: true, validators: Validators.required }),
      nhapLai: new FormControl('', { nonNullable: true, validators: Validators.required }),
    },
    { validators: nhapLaiPhaiKhop('moi', 'nhapLai') },
  );
}

describe('nhapLaiPhaiKhop() — validator cấp group, lỗi trên ô nhập lại', () => {
  it('nhập lại khác ô gốc → `mismatch` trên Ô NHẬP LẠI; group không mang lỗi riêng nhưng không hợp lệ', () => {
    const form = dungForm();
    form.setValue({ moi: 'Moi-12345', nhapLai: 'Moi-54321' });

    expect(form.controls.nhapLai.errors).toEqual({ mismatch: true });
    expect(form.controls.moi.errors).toBeNull();
    expect(form.errors).toBeNull();
    expect(form.invalid).toBeTrue();
  });

  it('khớp → không lỗi, form hợp lệ', () => {
    const form = dungForm();
    form.setValue({ moi: 'Moi-12345', nhapLai: 'Moi-12345' });

    expect(form.controls.nhapLai.errors).toBeNull();
    expect(form.valid).toBeTrue();
  });

  it('ô nhập lại rỗng → không `mismatch`, chỉ còn `required` của ô', () => {
    const form = dungForm();
    form.setValue({ moi: 'Moi-12345', nhapLai: '' });

    expect(form.controls.nhapLai.errors).toEqual({ required: true });
  });

  it('đổi Ô GỐC → tính lại: khớp thì gỡ `mismatch`, lệch lại thì gắn lại', () => {
    const form = dungForm();
    form.setValue({ moi: 'Moi-12345', nhapLai: 'Moi-54321' });

    form.controls.moi.setValue('Moi-54321');
    expect(form.controls.nhapLai.errors).toBeNull();

    form.controls.moi.setValue('Khac-000');
    expect(form.controls.nhapLai.errors).toEqual({ mismatch: true });
  });

  it('gõ ô nhập lại khi ô gốc CHƯA từng đổi giá trị → vẫn tính ngay', () => {
    const form = dungForm();

    form.controls.nhapLai.setValue('Moi-54321');

    expect(form.controls.nhapLai.errors).toEqual({ mismatch: true });
  });

  it('giữ lỗi khác đang có trên ô nhập lại (vd `server`) khi ô gốc đổi; `mismatch` đứng sau', () => {
    const form = dungForm();
    form.setValue({ moi: 'Moi-12345', nhapLai: 'Moi-12345' });
    form.controls.nhapLai.setErrors({ server: ['câu của máy chủ'] });

    form.controls.moi.setValue('Khac-000');
    expect(form.controls.nhapLai.errors).toEqual({ server: ['câu của máy chủ'], mismatch: true });
    expect(Object.keys(form.controls.nhapLai.errors ?? {})).toEqual(['server', 'mismatch']);

    form.controls.moi.setValue('Moi-12345');
    expect(form.controls.nhapLai.errors).toEqual({ server: ['câu của máy chủ'] });
  });

  it('reset form → hết `mismatch`', () => {
    const form = dungForm();
    form.setValue({ moi: 'Moi-12345', nhapLai: 'Moi-54321' });

    form.reset();

    expect(form.controls.nhapLai.hasError('mismatch')).toBeFalse();
  });

  it('khoá `mismatch` → `fieldErrorText` ra câu của `loi.CORE.CLIENT.VALIDATION_MISMATCH`', () => {
    const translate = { instant: (khoa: string) => `[${khoa}]` } as unknown as TranslateService;
    const form = dungForm();
    form.setValue({ moi: 'Moi-12345', nhapLai: 'Moi-54321' });

    expect(fieldErrorText(form.controls.nhapLai, true, translate)).toBe(
      '[loi.CORE.CLIENT.VALIDATION_MISMATCH]',
    );
  });

  it('tên ô không có trong group → ném lỗi ngay lúc dựng form, nêu tên ô', () => {
    expect(
      () =>
        new FormGroup(
          { moi: new FormControl(''), nhapLai: new FormControl('') },
          { validators: nhapLaiPhaiKhop('moi', 'goLai') },
        ),
    ).toThrowError(/"goLai"/);
  });
});
