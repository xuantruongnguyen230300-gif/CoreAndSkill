import { TRUY_VAN_DUOI_BP_SM, TRUY_VAN_DUOI_BP_XS } from './diem-ngat';

describe('diem-ngat — chuỗi truy vấn phản chiếu $bp-* (fe-ui-conventions.md §7.1)', () => {
  const cac = { TRUY_VAN_DUOI_BP_XS, TRUY_VAN_DUOI_BP_SM };

  for (const [ten, truyVan] of Object.entries(cac)) {
    it(`${ten} là truy vấn trình duyệt hiểu được, dạng max-width trừ 0.02px`, () => {
      expect(matchMedia(truyVan).media).not.toBe('not all');
      expect(truyVan).toMatch(/^\(max-width: \d+\.98px\)$/);
    });
  }

  it('hai hằng khác nhau — mỗi hằng một điểm ngắt', () => {
    expect(TRUY_VAN_DUOI_BP_XS).not.toBe(TRUY_VAN_DUOI_BP_SM);
  });
});
