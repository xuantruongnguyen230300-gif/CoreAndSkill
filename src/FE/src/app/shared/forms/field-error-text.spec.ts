import { FormControl, Validators } from '@angular/forms';
import { TranslateService } from '@ngx-translate/core';

import { fieldErrorText } from './field-error-text';

describe('fieldErrorText()', () => {
  let translate: jasmine.SpyObj<TranslateService>;

  beforeEach(() => {
    translate = jasmine.createSpyObj<TranslateService>('TranslateService', ['instant']);
    translate.instant.and.callFake((k: unknown) => `[${k as string}]`);
  });

  it('control không lỗi → null', () => {
    const c = new FormControl('ok', Validators.required);
    expect(fieldErrorText(c, false, translate)).toBeNull();
  });

  it('control lỗi nhưng CHƯA touched và form CHƯA gửi → null (không mắng người dùng đang gõ lần đầu)', () => {
    const c = new FormControl('', Validators.required);
    expect(fieldErrorText(c, false, translate)).toBeNull();
  });

  it('control lỗi, đã touched → dịch mã CORE.CLIENT.VALIDATION_<TÊN VALIDATOR>', () => {
    const c = new FormControl('', Validators.required);
    c.markAsTouched();
    expect(fieldErrorText(c, false, translate)).toBe('[loi.CORE.CLIENT.VALIDATION_REQUIRED]');
  });

  it('control lỗi, form đã gửi (chưa touched) → vẫn hiện', () => {
    const c = new FormControl('', Validators.required);
    expect(fieldErrorText(c, true, translate)).toBe('[loi.CORE.CLIENT.VALIDATION_REQUIRED]');
  });

  it('có lỗi server VÀ lỗi client cùng lúc → ưu tiên câu của server', () => {
    const c = new FormControl('x');
    c.setErrors({ required: true, server: ['Email đã tồn tại.'] });
    c.markAsTouched();
    expect(fieldErrorText(c, false, translate)).toBe('Email đã tồn tại.');
  });
});
