import { FormControl, FormGroup } from '@angular/forms';
import { TranslateService } from '@ngx-translate/core';

import { applyFieldErrors } from './apply-field-errors';

describe('applyFieldErrors()', () => {
  let translate: jasmine.SpyObj<TranslateService>;
  let form: FormGroup;

  beforeEach(() => {
    translate = jasmine.createSpyObj<TranslateService>('TranslateService', ['instant']);
    translate.instant.and.callFake((k: unknown) => `[${k as string}]`);
    form = new FormGroup({ currentPassword: new FormControl(''), newPassword: new FormControl('') });
  });

  it('gắn lỗi vào ĐÚNG control sau khi chuyển PascalCase → camelCase', () => {
    const khongKhop = applyFieldErrors(
      form,
      { CurrentPassword: [{ code: 'CORE.AUTH.PASSWORD_MISMATCH', messageParams: null }] },
      translate,
    );

    expect(khongKhop).toEqual([]);
    expect(form.controls['currentPassword'].errors?.['server']).toEqual(['[loi.CORE.AUTH.PASSWORD_MISMATCH]']);
    expect(form.controls['currentPassword'].touched).toBeTrue();
  });

  it('khoá BE KHÔNG khớp control nào → trả về khoá đó, KHÔNG nuốt im lặng', () => {
    const khongKhop = applyFieldErrors(form, { $record: [{ code: 'X', messageParams: null }] }, translate);
    expect(khongKhop).toEqual(['$record']);
  });

  it('giữ lỗi client sẵn có, không ghi đè — chỉ thêm khoá server', () => {
    form.controls['newPassword'].setErrors({ required: true });
    applyFieldErrors(form, { NewPassword: [{ code: 'CORE.AUTH.PASSWORD_TOO_SHORT', messageParams: { MinLength: '8' } }] }, translate);

    expect(form.controls['newPassword'].errors?.['required']).toBeTrue();
    expect(form.controls['newPassword'].errors?.['server']).toEqual(['[loi.CORE.AUTH.PASSWORD_TOO_SHORT]']);
  });

  it('ô nhận NHIỀU mã cùng lúc → giữ ĐÚNG thứ tự BE trả', () => {
    applyFieldErrors(
      form,
      {
        NewPassword: [
          { code: 'CORE.AUTH.PASSWORD_TOO_SHORT', messageParams: { MinLength: '8' } },
          { code: 'CORE.AUTH.PASSWORD_REQUIRES_DIGIT', messageParams: null },
        ],
      },
      translate,
    );

    expect(form.controls['newPassword'].errors?.['server']).toEqual([
      '[loi.CORE.AUTH.PASSWORD_TOO_SHORT]',
      '[loi.CORE.AUTH.PASSWORD_REQUIRES_DIGIT]',
    ]);
  });
  it('khoá KHỚP control nhưng danh sách RỖNG → KHÔNG đặt lỗi lên control (không có câu nào để hiện), trả khoá về như không khớp', () => {
    const khongKhop = applyFieldErrors(form, { NewPassword: [] }, translate);

    expect(khongKhop).toEqual(['NewPassword']);
    expect(form.controls['newPassword'].errors).toBeNull();
    expect(form.controls['newPassword'].valid).toBeTrue();
    expect(form.controls['newPassword'].touched).toBeFalse();
  });

  it('khoá rỗng không xoá lỗi client sẵn có của control và không thêm khoá server', () => {
    form.controls['newPassword'].setErrors({ required: true });

    applyFieldErrors(form, { NewPassword: [] }, translate);

    expect(form.controls['newPassword'].errors).toEqual({ required: true });
  });

  it('lẫn khoá rỗng và khoá có câu → khoá có câu vẫn gắn vào ô, khoá rỗng trả về', () => {
    const khongKhop = applyFieldErrors(
      form,
      {
        NewPassword: [],
        CurrentPassword: [{ code: 'CORE.AUTH.PASSWORD_MISMATCH', messageParams: null }],
      },
      translate,
    );

    expect(khongKhop).toEqual(['NewPassword']);
    expect(form.controls['currentPassword'].errors?.['server']).toEqual([
      '[loi.CORE.AUTH.PASSWORD_MISMATCH]',
    ]);
    expect(form.controls['newPassword'].errors).toBeNull();
  });
});
