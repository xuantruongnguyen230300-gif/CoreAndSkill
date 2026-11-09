import { HttpErrorResponse } from '@angular/common/http';

import {
  ApiFailure,
  ApiFailureError,
  docEnvelopeLoi,
  maLoiKhongEnvelope,
} from './api-result.model';

describe('docEnvelopeLoi', () => {
  it('đọc được envelope khi thân lỗi có success và traceId (tên field CÓ THẬT trên dây)', () => {
    const envelope: ApiFailure = {
      success: false,
      data: null,
      error: {
        code: 'CORE.USER.EMAIL_DUPLICATED',
        type: 'Conflict',
        message: 'Email đã tồn tại.',
        messageParams: { Email: 'a@b.c' },
        fieldErrors: null,
      },
      traceId: 'trace-abc',
    };
    const err = new HttpErrorResponse({ status: 409, error: envelope });

    expect(docEnvelopeLoi(err)).toEqual(envelope);
  });

  // Bẫy 4.3 của 02-http-envelope.md: mất mạng / proxy trả HTML → PHẢI trả null, không phải object rỗng.
  it('trả null khi thân lỗi không phải envelope — mất mạng hoặc HTML từ proxy', () => {
    const err = new HttpErrorResponse({ status: 0, error: null });
    expect(docEnvelopeLoi(err)).toBeNull();

    const errHtml = new HttpErrorResponse({ status: 502, error: '<html>Bad Gateway</html>' });
    expect(docEnvelopeLoi(errHtml)).toBeNull();
  });

  it('trả null khi thân là object nhưng thiếu field envelope thật (không đoán mò)', () => {
    const err = new HttpErrorResponse({ status: 500, error: { message: 'Internal error' } });
    expect(docEnvelopeLoi(err)).toBeNull();
  });
});

// Nguồn DUY NHẤT cho luật F45 (fe-api-client.md §2.2, be-cqrs-handler.md §7.4): `dichLoi` và
// `ApiFailureError` phải cùng dùng hàm này — không mỗi nơi tự chọn mã một kiểu.
describe('maLoiKhongEnvelope()', () => {
  it('status 0 (không nhận được phản hồi nào) → NO_CONNECTION', () => {
    expect(maLoiKhongEnvelope(0)).toBe('CORE.CLIENT.NO_CONNECTION');
  });

  it('mọi 5xx → SERVER_UNAVAILABLE (biên dưới 500, biên trên 599)', () => {
    expect(maLoiKhongEnvelope(500)).toBe('CORE.CLIENT.SERVER_UNAVAILABLE');
    expect(maLoiKhongEnvelope(502)).toBe('CORE.CLIENT.SERVER_UNAVAILABLE');
    expect(maLoiKhongEnvelope(599)).toBe('CORE.CLIENT.SERVER_UNAVAILABLE');
  });

  it('status khác 0 và ngoài 5xx → INVALID_RESPONSE (413 vượt giới hạn, 404 dạng HTML, 600 ngoài biên)', () => {
    expect(maLoiKhongEnvelope(413)).toBe('CORE.CLIENT.INVALID_RESPONSE');
    expect(maLoiKhongEnvelope(404)).toBe('CORE.CLIENT.INVALID_RESPONSE');
    expect(maLoiKhongEnvelope(600)).toBe('CORE.CLIENT.INVALID_RESPONSE');
    expect(maLoiKhongEnvelope(200)).toBe('CORE.CLIENT.INVALID_RESPONSE');
  });
});

describe('ApiFailureError — message không envelope theo cùng hàm maLoiKhongEnvelope', () => {
  it('body null: message theo status — 0, 503, 413 cho đúng ba mã khác nhau', () => {
    expect(new ApiFailureError(null, 0).message).toBe('CORE.CLIENT.NO_CONNECTION');
    expect(new ApiFailureError(null, 503).message).toBe('CORE.CLIENT.SERVER_UNAVAILABLE');
    expect(new ApiFailureError(null, 413).message).toBe('CORE.CLIENT.INVALID_RESPONSE');
  });

  it('body có envelope: message luôn là body.error.code, bất kể status', () => {
    const envelope: ApiFailure = {
      success: false,
      data: null,
      error: {
        code: 'CORE.USER.EMAIL_DUPLICATED',
        type: 'Conflict',
        message: 'Email đã tồn tại.',
        messageParams: null,
        fieldErrors: null,
      },
      traceId: 't',
    };
    expect(new ApiFailureError(envelope, 409).message).toBe('CORE.USER.EMAIL_DUPLICATED');
  });
});
