import type { Page } from '@playwright/test';

import { DUNG_SAI_PX, choTrangYen, docDo, phepDo } from './chung';

/**
 * Hai bề rộng thử — tham số của phép đo, không phải token (cùng cách ba khung nhìn của ADR-0117 QC3): 390 nằm dưới
 * `$bp-xs`, 700 nằm giữa `$bp-xs` và `$bp-md` (DESIGN.md §6.3 — điểm ngắt là biến Sass, không đọc được lúc chạy).
 * Đổi điểm ngắt ở DESIGN.md thì người đổi kiểm hai số này còn nằm đúng dải. Chiều cao đủ lớn để căn giữa dọc và căn
 * trên phân biệt được.
 */
export const KHUNG_DUOI_XS = { width: 390, height: 1000 } as const;
export const KHUNG_XS_DEN_MD = { width: 700, height: 1000 } as const;

interface DoKhung {
  readonly trai: number;
  readonly phai: number;
  readonly cao: number;
  readonly tren: number;
  readonly vw: number;
  readonly vien: readonly number[]; // trên, phải, dưới, trái
  readonly boGoc: readonly string[]; // bốn góc, theo computed style
  readonly dem: readonly number[]; // trên, phải, dưới, trái
  readonly mainTren: number;
  readonly mainCao: number;
}

const doKhungTrongTrang = (): DoKhung | null => {
  const main = document.querySelector('main');
  const khung = main?.firstElementChild as HTMLElement | null;
  if (!main || !khung) return null;
  const s = getComputedStyle(khung);
  const r = khung.getBoundingClientRect();
  const m = main.getBoundingClientRect();
  const px = (v: string) => parseFloat(v) || 0;
  return {
    trai: r.left,
    phai: r.right,
    cao: r.height,
    tren: r.top,
    vw: document.documentElement.clientWidth,
    vien: [s.borderTopWidth, s.borderRightWidth, s.borderBottomWidth, s.borderLeftWidth].map(px),
    boGoc: [
      s.borderTopLeftRadius,
      s.borderTopRightRadius,
      s.borderBottomRightRadius,
      s.borderBottomLeftRadius,
    ],
    dem: [s.paddingTop, s.paddingRight, s.paddingBottom, s.paddingLeft].map(px),
    mainTren: m.top,
    mainCao: m.height,
  };
};

const BON_CANH = ['trên', 'phải', 'dưới', 'trái'] as const;

async function doO(
  page: Page,
  kichThuoc: { width: number; height: number },
): Promise<DoKhung | null> {
  await page.setViewportSize(kichThuoc);
  await choTrangYen(page);
  return page.evaluate(doKhungTrongTrang);
}

/**
 * A9 — khung xác thực đổi theo bề rộng (AuthCard.md §Responsive): dưới `$bp-xs` khung trải hết bề rộng, bỏ viền và
 * bo góc, đệm `--sp-6`, căn trên (không căn giữa dọc); từ `$bp-xs` tới `$bp-md` đệm trong `--sp-8`.
 * Phép đo tự đổi khung nhìn rồi trả về khung cũ, nên chạy được ở project nào cũng cho cùng kết quả.
 */
export const a9AuthCardResponsive = phepDo(
  {
    ma: 'A9',
    ten: 'khung xác thực theo bề rộng: dưới $bp-xs trải hết, không viền/bo góc, đệm --sp-6, căn trên; $bp-xs…$bp-md đệm --sp-8',
    nguon: [
      'docs/Design/Components/AuthCard.md:133 (§Responsive — < $bp-xs: trải hết bề rộng, bỏ viền và bo góc, đệm --sp-6, căn trên)',
      'docs/Design/Components/AuthCard.md:132 (§Responsive — $bp-xs … $bp-md: đệm trong hạ xuống --sp-8)',
    ],
  },
  async (page: Page) => {
    const goc = page.viewportSize();
    const viPham: string[] = [];
    let ghiChu: string | undefined;
    try {
      const sp6 = await docDo(page, 'var(--sp-6)');
      const sp8 = await docDo(page, 'var(--sp-8)');

      const nho = await doO(page, KHUNG_DUOI_XS);
      if (!nho)
        return { viPham: ['không tìm thấy <main> có khung — trang không dựng khung xác thực'] };
      const dw = `${KHUNG_DUOI_XS.width}px`;
      if (Math.abs(nho.trai) > DUNG_SAI_PX || Math.abs(nho.vw - nho.phai) > DUNG_SAI_PX) {
        viPham.push(
          `${dw}: khung không trải hết bề rộng — mép ${nho.trai.toFixed(1)}…${nho.phai.toFixed(1)}px, khung nhìn rộng ${nho.vw}px`,
        );
      }
      nho.vien.forEach((v, i) => {
        if (v > DUNG_SAI_PX)
          viPham.push(`${dw}: khung còn viền ${BON_CANH[i]} dày ${v}px, spec đòi bỏ viền`);
      });
      nho.boGoc.forEach((b, i) => {
        if (parseFloat(b) > DUNG_SAI_PX)
          viPham.push(`${dw}: khung còn bo góc (góc thứ ${i + 1}: ${b}), spec đòi bỏ bo góc`);
      });
      nho.dem.forEach((d, i) => {
        if (Math.abs(d - sp6) > DUNG_SAI_PX)
          viPham.push(`${dw}: đệm ${BON_CANH[i]} ${d}px, spec đòi ${sp6}px (--sp-6)`);
      });
      const duCho = nho.mainCao - nho.cao > 2 * sp6 + DUNG_SAI_PX;
      if (!duCho)
        ghiChu = `${dw}: khung cao gần bằng vùng main — không phân biệt được căn trên với căn giữa, không đo "căn trên"`;
      else if (nho.tren - nho.mainTren > sp6 + DUNG_SAI_PX) {
        viPham.push(
          `${dw}: khung cách đỉnh vùng main ${(nho.tren - nho.mainTren).toFixed(1)}px — căn giữa dọc; spec đòi căn trên (tối đa ${sp6}px)`,
        );
      }

      const vua = await doO(page, KHUNG_XS_DEN_MD);
      if (!vua)
        return { viPham: ['không tìm thấy <main> có khung ở bề rộng giữa $bp-xs và $bp-md'] };
      vua.dem.forEach((d, i) => {
        if (Math.abs(d - sp8) > DUNG_SAI_PX) {
          viPham.push(
            `${KHUNG_XS_DEN_MD.width}px: đệm ${BON_CANH[i]} ${d}px, spec đòi ${sp8}px (--sp-8)`,
          );
        }
      });
    } finally {
      if (goc) await page.setViewportSize(goc);
    }
    return ghiChu ? { viPham, ghiChu } : { viPham };
  },
);
