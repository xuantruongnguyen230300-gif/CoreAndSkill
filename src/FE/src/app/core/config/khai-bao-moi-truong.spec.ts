import { khaiBaoMoiTruong } from './khai-bao-moi-truong';

describe('khaiBaoMoiTruong', () => {
  it('trả nguyên giá trị khi mã đơn vị không rỗng', () => {
    const env = khaiBaoMoiTruong({
      production: true,
      apiBaseUrl: 'https://api.x.invalid/api/v1',
      tenantCode: 'ACME',
    });
    expect(env).toEqual({
      production: true,
      apiBaseUrl: 'https://api.x.invalid/api/v1',
      tenantCode: 'ACME',
    });
  });

  it('chuỗi rỗng viết thẳng là lỗi BIÊN DỊCH (@ts-expect-error: dòng dưới mà biên dịch được thì spec này tự đỏ)', () => {
    khaiBaoMoiTruong({
      production: true,
      apiBaseUrl: 'https://api.x.invalid/api/v1',
      // @ts-expect-error — tenantCode rỗng bị kiểu từ chối
      tenantCode: '',
    });
    expect(true).toBe(true);
  });
});
