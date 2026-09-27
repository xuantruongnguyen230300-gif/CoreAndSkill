import { ApiFailureError, ApiResult } from './api-result.model';
import { unwrapData } from './unwrap';

describe('unwrapData', () => {
  it('trả data khi envelope thành công', () => {
    const res: ApiResult<{ id: string }> = {
      success: true,
      data: { id: 'abc' },
      error: null,
      traceId: 't-1',
    };

    expect(unwrapData(res)).toEqual({ id: 'abc' });
  });

  it('ném ApiFailureError mang theo envelope khi envelope thất bại — không trả undefined/mặc định', () => {
    const res: ApiResult<{ id: string }> = {
      success: false,
      data: null,
      error: {
        code: 'CORE.USER.NOT_FOUND',
        type: 'NotFound',
        message: 'Không tìm thấy người dùng.',
        messageParams: null,
        fieldErrors: null,
      },
      traceId: 't-2',
    };

    expect(() => unwrapData(res)).toThrowMatching((err: unknown) => {
      return (
        err instanceof ApiFailureError && err.message === 'CORE.USER.NOT_FOUND' && err.body === res
      );
    });
  });
});
