import { TranslateService } from '@ngx-translate/core';

import { ApiFailureError, ApiFieldError } from '../../core/http/api-result.model';
import { cauMaCon, cauTungMa, fieldErrorsText } from './field-errors-text';

function body(
  code: string,
  fieldErrors: Record<string, readonly ApiFieldError[]> | null,
  message = `msg:${code}`,
  status = 400,
): ApiFailureError {
  return new ApiFailureError(
    {
      success: false,
      data: null,
      error: { code, type: 'Validation', message, messageParams: null, fieldErrors },
      traceId: 't',
    },
    status,
  );
}

describe('fieldErrorsText()', () => {
  const translate = {
    instant: (khoa: string, tham?: Record<string, string>) =>
      tham && Object.keys(tham).length > 0 ? `[${khoa}:${JSON.stringify(tham)}]` : `[${khoa}]`,
  } as unknown as TranslateService;

  it('có fieldErrors → dịch từng mã kèm messageParams, đúng thứ tự BE trả, mỗi mã một dòng', () => {
    const cau = fieldErrorsText(
      translate,
      body('CORE.VALIDATION.FAILED', {
        RoleIds: [
          { code: 'CORE.VALIDATION.MAX_ITEMS', messageParams: { MaxItems: '50' } },
          { code: 'CORE.VALIDATION.REQUIRED', messageParams: null },
        ],
      }),
    );
    expect(cau).toBe(
      '[loi.CORE.VALIDATION.MAX_ITEMS:{"MaxItems":"50"}]\n[loi.CORE.VALIDATION.REQUIRED]',
    );
  });

  it('fieldErrors null hoặc rỗng → câu của CHÍNH mã (không im lặng)', () => {
    expect(fieldErrorsText(translate, body('CORE.VALIDATION.FAILED', null))).toBe(
      '[loi.CORE.VALIDATION.FAILED]',
    );
    expect(fieldErrorsText(translate, body('CORE.VALIDATION.FAILED', {}))).toBe(
      '[loi.CORE.VALIDATION.FAILED]',
    );
    expect(fieldErrorsText(translate, body('CORE.X', { A: [] }))).toBe('[loi.CORE.X]');
  });

  it('mã gốc CORE.VALIDATION.FAILED kèm một khoá lạ "" → câu của mã con, không phải của mã gốc', () => {
    expect(
      fieldErrorsText(
        translate,
        body('CORE.VALIDATION.FAILED', {
          '': [{ code: 'CORE.USER.DUPLICATE_ROLE_ENTRY', messageParams: null }],
        }),
      ),
    ).toBe('[loi.CORE.USER.DUPLICATE_ROLE_ENTRY]');
  });

  it('mã của nhiều khoá → mỗi mã một dòng, theo thứ tự khoá rồi thứ tự mã BE gửi', () => {
    expect(
      fieldErrorsText(
        translate,
        body('CORE.VALIDATION.FAILED', {
          RoleIds: [{ code: 'CORE.VALIDATION.REQUIRED', messageParams: null }],
          Version: [{ code: 'CORE.VALIDATION.NOT_EMPTY', messageParams: null }],
        }),
      ),
    ).toBe('[loi.CORE.VALIDATION.REQUIRED]\n[loi.CORE.VALIDATION.NOT_EMPTY]');
  });

  it('body null (mất kết nối) → câu dự phòng NO_CONNECTION', () => {
    expect(fieldErrorsText(translate, new ApiFailureError(null))).toBe(
      '[loi.CORE.CLIENT.NO_CONNECTION]',
    );
  });

  // Lớp lỗi xuyên suốt: interceptor đã toast kèm traceId — khu lỗi của hộp KHÔNG hiện lần hai.
  it('500 CORE.SYSTEM.UNEXPECTED → null', () => {
    expect(fieldErrorsText(translate, body('CORE.SYSTEM.UNEXPECTED', null, 'msg', 500))).toBeNull();
  });

  it('403 CORE.AUTH.FORBIDDEN → null; 403 mã nghiệp vụ → câu của mã như cũ', () => {
    expect(fieldErrorsText(translate, body('CORE.AUTH.FORBIDDEN', null, 'msg', 403))).toBeNull();
    expect(fieldErrorsText(translate, body('CORE.ROLE.IN_USE', null, 'msg', 403))).toBe(
      '[loi.CORE.ROLE.IN_USE]',
    );
  });

  it('502 không envelope → null, KHÔNG phải câu mất kết nối', () => {
    expect(fieldErrorsText(translate, new ApiFailureError(null, 502))).toBeNull();
  });
});

describe('cauTungMa() / fieldErrorsText() — mã chưa có khoá dịch', () => {
  function dich(): TranslateService {
    const translate = jasmine.createSpyObj<TranslateService>('TranslateService', ['instant']);
    translate.instant.and.callFake((k: unknown) =>
      k === 'loi.CORE.AUTH.CO_KHOA'
        ? 'Câu có khoá'
        : k === 'loi.CORE.VALIDATION.INVALID'
          ? 'Giá trị không hợp lệ.'
          : (k as string),
    );
    return translate;
  }

  it('cauTungMa: mã có bản dịch → câu; mã thiếu → câu dự phòng, mỗi mã một dòng', () => {
    expect(
      cauTungMa(dich(), [
        { code: 'CORE.AUTH.CO_KHOA', messageParams: null },
        { code: 'CORE.AUTH.MA_LA', messageParams: null },
      ]),
    ).toBe('Câu có khoá\nGiá trị không hợp lệ.');
  });

  it('fieldErrorsText: mã con thiếu bản dịch không bao giờ ra chuỗi dạng loi.X.Y', () => {
    const cau = fieldErrorsText(
      dich(),
      body('CORE.VALIDATION.FAILED', { A: [{ code: 'CORE.AUTH.MA_LA', messageParams: null }] }),
    );
    expect(cau).toBe('Giá trị không hợp lệ.');
    expect(cau).not.toContain('loi.');
  });
});

describe('cauMaCon() — mã chưa có khoá dịch', () => {
  it('mã lạ → câu dự phòng, KHÔNG hiện chuỗi khoá; mã có khoá → câu của mã', () => {
    const translate = jasmine.createSpyObj<TranslateService>('TranslateService', ['instant']);
    translate.instant.and.callFake((k: unknown) =>
      k === 'loi.CORE.AUTH.CO_KHOA'
        ? 'Câu có khoá'
        : k === 'loi.CORE.VALIDATION.INVALID'
          ? 'Giá trị không hợp lệ.'
          : (k as string),
    );

    expect(cauMaCon(translate, { code: 'CORE.AUTH.MA_LA', messageParams: null })).toBe(
      'Giá trị không hợp lệ.',
    );
    expect(cauMaCon(translate, { code: 'CORE.AUTH.CO_KHOA', messageParams: null })).toBe(
      'Câu có khoá',
    );
  });
});
