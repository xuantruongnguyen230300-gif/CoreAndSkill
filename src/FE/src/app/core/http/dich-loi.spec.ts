import { TranslateService } from '@ngx-translate/core';

import { ApiFailure, ApiFailureError } from './api-result.model';
import {
  cauLoiKhongPhaiApi,
  dichLoi,
  dichLoiBatKy,
  dichLoiChoMan,
  thamSoRetryAfter,
} from './dich-loi';

function envelope(
  code: string,
  message = `msg:${code}`,
  messageParams: Readonly<Record<string, string>> | null = null,
): ApiFailure {
  return {
    success: false,
    data: null,
    error: { code, type: 'X', message, messageParams, fieldErrors: null },
    traceId: 't',
  };
}

/** Echo cả khoá lẫn tham số — phân biệt được nhánh nào của `dichLoi` đã chạy và tham số nào tới nơi. */
function trackingTranslate(): { instant: jasmine.Spy } & TranslateService {
  const instant = jasmine
    .createSpy('instant')
    .and.callFake((khoa: string, tham?: Record<string, string>) =>
      tham && Object.keys(tham).length > 0 ? `[${khoa}:${JSON.stringify(tham)}]` : `[${khoa}]`,
    );
  return { instant } as unknown as { instant: jasmine.Spy } & TranslateService;
}

// Luật F45 (fe-api-client.md §2.2, be-cqrs-handler.md §7.4): `body` null → mã chọn theo status, ĐÚNG
// một hàm (`maLoiKhongEnvelope`, api-result.model.spec.ts đã khoá riêng); ở đây khoá hành vi của
// `dichLoi` khi ghép mã đó vào khoá dịch và tham số.
describe('dichLoi() — thân KHÔNG mang envelope, mã chọn theo status', () => {
  it('status 0 (không nhận được phản hồi nào) → khoá NO_CONNECTION, KHÔNG kèm maHttp', () => {
    const translate = trackingTranslate();
    expect(dichLoi(translate, null, 0)).toBe('[loi.CORE.CLIENT.NO_CONNECTION]');
    expect(translate.instant).toHaveBeenCalledOnceWith('loi.CORE.CLIENT.NO_CONNECTION', {});
  });

  it('một 5xx bất kỳ (503) → khoá SERVER_UNAVAILABLE, KHÔNG kèm maHttp', () => {
    const translate = trackingTranslate();
    expect(dichLoi(translate, null, 503)).toBe('[loi.CORE.CLIENT.SERVER_UNAVAILABLE]');
    expect(translate.instant).toHaveBeenCalledOnceWith('loi.CORE.CLIENT.SERVER_UNAVAILABLE', {});
  });

  it('413 (thân vượt giới hạn, không envelope) → khoá INVALID_RESPONSE, kèm maHttp="413"', () => {
    const translate = trackingTranslate();
    expect(dichLoi(translate, null, 413)).toBe(
      '[loi.CORE.CLIENT.INVALID_RESPONSE:{"maHttp":"413"}]',
    );
    expect(translate.instant).toHaveBeenCalledOnceWith('loi.CORE.CLIENT.INVALID_RESPONSE', {
      maHttp: '413',
    });
  });

  it('200 (không phải lỗi, nhưng lọt qua nhánh không envelope) → vẫn INVALID_RESPONSE theo status thật', () => {
    const translate = trackingTranslate();
    expect(dichLoi(translate, null, 200)).toBe(
      '[loi.CORE.CLIENT.INVALID_RESPONSE:{"maHttp":"200"}]',
    );
  });

  it('404 dạng HTML (đường dẫn không tới được app) → INVALID_RESPONSE, KHÔNG phải câu mất kết nối', () => {
    const translate = trackingTranslate();
    expect(dichLoi(translate, null, 404)).toBe(
      '[loi.CORE.CLIENT.INVALID_RESPONSE:{"maHttp":"404"}]',
    );
  });

  it('tham số thêm của người gọi được giữ cùng maHttp', () => {
    const translate = trackingTranslate();
    dichLoi(translate, null, 413, { RetryAfterSeconds: '7' });
    expect(translate.instant).toHaveBeenCalledOnceWith('loi.CORE.CLIENT.INVALID_RESPONSE', {
      maHttp: '413',
      RetryAfterSeconds: '7',
    });
  });
});

describe('dichLoi() — thân CÓ envelope: không đổi hành vi theo status', () => {
  it('dịch theo body.error.code, status không ảnh hưởng tới khoá dịch', () => {
    const translate = trackingTranslate();
    const body = envelope('CORE.USER.EMAIL_DUPLICATED', 'msg', { Email: 'a@b.c' });
    expect(dichLoi(translate, body, 409)).toBe(
      '[loi.CORE.USER.EMAIL_DUPLICATED:{"Email":"a@b.c"}]',
    );
  });

  it('thiếu bản dịch (instant trả về đúng khoá) → rơi về câu BE gửi kèm', () => {
    const translate = { instant: (khoa: string) => khoa } as unknown as TranslateService;
    const body = envelope('CORE.KHONG_CO_KHOA', 'câu BE gửi kèm');
    expect(dichLoi(translate, body, 422)).toBe('câu BE gửi kèm');
  });

  it('thiếu khoá và message rỗng hoặc chỉ khoảng trắng → câu dự phòng chung, không trả chuỗi rỗng', () => {
    const translate = {
      instant: (khoa: string) =>
        khoa === 'loi.CORE.CLIENT.UNKNOWN_ERROR_CODE' ? 'dự phòng' : khoa,
    } as unknown as TranslateService;
    for (const message of ['', '   ', '\n\t']) {
      expect(dichLoi(translate, envelope('CORE.KHONG_CO_KHOA', message), 422)).toBe('dự phòng');
    }
  });

  it('thamSoThem ghép cùng messageParams của envelope, thamSoThem thắng khi trùng khoá', () => {
    const translate = trackingTranslate();
    const body = envelope('CORE.RATE_LIMIT.EXCEEDED', 'msg', { RetryAfterSeconds: '5' });
    expect(dichLoi(translate, body, 429, { RetryAfterSeconds: '30' })).toBe(
      '[loi.CORE.RATE_LIMIT.EXCEEDED:{"RetryAfterSeconds":"30"}]',
    );
  });
});

describe('dichLoiChoMan() — lớp lỗi xuyên suốt trả null, còn lại như dichLoi', () => {
  it('5xx không envelope (lớp xuyên suốt, interceptor đã toast) → null', () => {
    expect(dichLoiChoMan(trackingTranslate(), new ApiFailureError(null, 503))).toBeNull();
  });

  it('403 mang mã CORE.AUTH.* (lớp xuyên suốt) → null', () => {
    const loi = new ApiFailureError(envelope('CORE.AUTH.FORBIDDEN'), 403);
    expect(dichLoiChoMan(trackingTranslate(), loi)).toBeNull();
  });

  it('lỗi nghiệp vụ thường (400 có envelope) → câu dịch theo mã', () => {
    const loi = new ApiFailureError(envelope('X.Y'), 400);
    expect(dichLoiChoMan(trackingTranslate(), loi)).toBe('[loi.X.Y]');
  });

  it('413 không envelope → câu INVALID_RESPONSE kèm maHttp, không phải null', () => {
    expect(dichLoiChoMan(trackingTranslate(), new ApiFailureError(null, 413))).toBe(
      '[loi.CORE.CLIENT.INVALID_RESPONSE:{"maHttp":"413"}]',
    );
  });
});

describe('thamSoRetryAfter() — không đổi bởi F45', () => {
  it('header hợp lệ → RetryAfterSeconds đúng số giây', () => {
    const err = { headers: { get: () => '42' } } as unknown as Parameters<
      typeof thamSoRetryAfter
    >[0];
    expect(thamSoRetryAfter(err)).toEqual({ RetryAfterSeconds: '42' });
  });

  it('header vắng hoặc sai dạng → rỗng', () => {
    const vang = { headers: { get: () => null } } as unknown as Parameters<
      typeof thamSoRetryAfter
    >[0];
    expect(thamSoRetryAfter(vang)).toEqual({});
  });
});

describe('cauLoiKhongPhaiApi() / dichLoiBatKy() — nhánh lỗi không phải phản hồi HTTP', () => {
  it('cauLoiKhongPhaiApi → khoá NO_CONNECTION, không kèm tham số', () => {
    const translate = trackingTranslate();
    expect(cauLoiKhongPhaiApi(translate)).toBe('[loi.CORE.CLIENT.NO_CONNECTION]');
    expect(translate.instant).toHaveBeenCalledOnceWith('loi.CORE.CLIENT.NO_CONNECTION');
  });

  it('lỗi khác ApiFailureError (Error, chuỗi, null) → NO_CONNECTION', () => {
    for (const loi of [new Error('timeout'), 'x', null, undefined]) {
      const translate = trackingTranslate();
      expect(dichLoiBatKy(translate, loi)).toBe('[loi.CORE.CLIENT.NO_CONNECTION]');
    }
  });

  it('ApiFailureError có envelope → dịch theo mã envelope, KHÔNG NO_CONNECTION', () => {
    const translate = trackingTranslate();
    const loi = new ApiFailureError(envelope('CORE.AUTH.INVALID_CREDENTIALS'), 401);
    expect(dichLoiBatKy(translate, loi)).toBe('[loi.CORE.AUTH.INVALID_CREDENTIALS]');
  });

  it('ApiFailureError không envelope (413) → câu theo status kèm maHttp, KHÔNG NO_CONNECTION', () => {
    const translate = trackingTranslate();
    expect(dichLoiBatKy(translate, new ApiFailureError(null, 413))).toBe(
      '[loi.CORE.CLIENT.INVALID_RESPONSE:{"maHttp":"413"}]',
    );
  });

  it('ApiFailureError lớp xuyên suốt (503 không envelope) → null với dịch mặc định (interceptor đã toast)', () => {
    expect(dichLoiBatKy(trackingTranslate(), new ApiFailureError(null, 503))).toBeNull();
  });

  it('truyền dichApi → ApiFailureError đi qua hàm đó, lỗi khác vẫn NO_CONNECTION', () => {
    const translate = trackingTranslate();
    const dichApi = jasmine.createSpy('dichApi').and.returnValue('cau-rieng');
    const loi = new ApiFailureError(envelope('X.Y'), 400);
    expect(dichLoiBatKy(translate, loi, dichApi)).toBe('cau-rieng');
    expect(dichApi).toHaveBeenCalledOnceWith(loi);
    expect(dichLoiBatKy(translate, new Error('e'), dichApi)).toBe(
      '[loi.CORE.CLIENT.NO_CONNECTION]',
    );
    expect(dichApi).toHaveBeenCalledTimes(1);
  });
});
