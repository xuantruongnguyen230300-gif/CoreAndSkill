import { TranslateService } from '@ngx-translate/core';

import { ApiFailure, ApiFailureError } from './api-result.model';
import { dichLoiChoMan } from './dich-loi';
import { laLoiXuyenSuot } from './loi-xuyen-suot';

function envelope(code: string): ApiFailure {
  return {
    success: false,
    data: null,
    error: { code, type: 'X', message: `msg:${code}`, messageParams: null, fieldErrors: null },
    traceId: 't',
  };
}

describe('laLoiXuyenSuot() — mọi 5xx, cộng 403 CORE.AUTH.* trừ PASSWORD_CHANGE_REQUIRED', () => {
  const THUOC: readonly [number, string | null][] = [
    [500, 'CORE.SYSTEM.UNEXPECTED'],
    [500, null],
    [502, null],
    [503, 'CORE.BAT_KY'],
    [599, null],
    [403, 'CORE.AUTH.FORBIDDEN'],
    [403, 'CORE.AUTH.CSRF_REJECTED'],
    [403, 'CORE.AUTH.ORIGIN_REJECTED'],
  ];
  const KHONG_THUOC: readonly [number, string | null][] = [
    [0, null],
    [400, 'CORE.VALIDATION.FAILED'],
    [401, 'CORE.AUTH.NOT_AUTHENTICATED'],
    [403, 'CORE.AUTH.PASSWORD_CHANGE_REQUIRED'],
    [403, 'CORE.ROLE.IN_USE'],
    [403, null],
    [404, 'CORE.ROUTE.NOT_FOUND'],
    [409, 'CORE.CONCURRENCY.CONFLICT'],
    [422, 'CORE.AUTH.CHANGE_PASSWORD_FAILED'],
    [429, 'CORE.RATE_LIMIT.EXCEEDED'],
    [600, null],
  ];

  for (const [status, code] of THUOC) {
    it(`${status} ${code ?? '(không envelope)'} → thuộc lớp`, () => {
      expect(laLoiXuyenSuot(status, code === null ? null : envelope(code))).toBeTrue();
    });
  }

  for (const [status, code] of KHONG_THUOC) {
    it(`${status} ${code ?? '(không envelope)'} → KHÔNG thuộc lớp`, () => {
      expect(laLoiXuyenSuot(status, code === null ? null : envelope(code))).toBeFalse();
    });
  }
});

describe('dichLoiChoMan() — câu cho chỗ hiện lỗi riêng của màn', () => {
  const translate = { instant: (khoa: string) => khoa } as unknown as TranslateService;

  it('lớp xuyên suốt → null', () => {
    expect(dichLoiChoMan(translate, new ApiFailureError(envelope('CORE.SYSTEM.UNEXPECTED'), 500)))
      .withContext('500')
      .toBeNull();
    expect(dichLoiChoMan(translate, new ApiFailureError(envelope('CORE.AUTH.FORBIDDEN'), 403)))
      .withContext('403 CORE.AUTH.FORBIDDEN')
      .toBeNull();
  });

  it('lỗi khác → như `dichLoi`: câu BE khi thiếu khoá dịch, câu mất kết nối khi không envelope', () => {
    expect(
      dichLoiChoMan(translate, new ApiFailureError(envelope('CORE.AUTH.INVALID_CREDENTIALS'), 401)),
    ).toBe('msg:CORE.AUTH.INVALID_CREDENTIALS');
    expect(dichLoiChoMan(translate, new ApiFailureError(null))).toBe(
      'loi.CORE.CLIENT.NO_CONNECTION',
    );
  });
});
