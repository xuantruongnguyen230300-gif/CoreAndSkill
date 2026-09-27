import { HttpErrorResponse } from '@angular/common/http';

import { ApiFailure, docEnvelopeLoi } from './api-result.model';

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
