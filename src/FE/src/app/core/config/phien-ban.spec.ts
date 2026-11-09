import { maCommitNgan } from './phien-ban';

describe('maCommitNgan', () => {
  it('cắt 7 ký tự đầu của mã đầy đủ', () => {
    expect(maCommitNgan('ac807bdcdd34f64b6e5b0c3c5a1d6f0e2b7a9c11')).toBe('ac807bd');
  });

  it('không có mã (máy dev, ngoài CI) → null để Footer ẩn phần phiên bản', () => {
    expect(maCommitNgan(null)).toBeNull();
    expect(maCommitNgan('  ')).toBeNull();
  });

  it('bản dựng ở cây nguồn không mang mã → null', () => {
    expect(maCommitNgan()).toBeNull();
  });
});
