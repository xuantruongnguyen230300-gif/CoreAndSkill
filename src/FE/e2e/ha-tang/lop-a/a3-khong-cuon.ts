import type { Page } from '@playwright/test';

import { DUNG_SAI_PX, choTrangYen, phepDo } from './chung';

/**
 * A3 — trang xác thực không có thanh cuộn dọc khi khung nhìn cao hơn nội dung, và `Footer` sát đáy.
 * Điều kiện "đủ chỗ" tính đúng công thức của spec: đệm trên + khung + đệm dưới của `main` + chiều cao `Footer`.
 * Khung nhìn thấp hơn tổng đó thì trang PHẢI cuộn — phép đo không có gì để kết luận và nói rõ qua `ghiChu`.
 */
export const a3KhongCuon = phepDo(
  {
    ma: 'A3',
    ten: 'trang xác thực không cuộn dọc khi đủ chỗ, Footer sát đáy',
    nguon: [
      'docs/Design/Components/AuthCard.md:122 (§Responsive — Đủ chỗ: Footer sát đáy khung nhìn, không có thanh cuộn)',
    ],
  },
  async (page: Page) => {
    await choTrangYen(page);
    const d = await page.evaluate(() => {
      const main = document.querySelector('main');
      const khung = main?.firstElementChild as HTMLElement | null;
      const footer = document.querySelector('footer');
      const goc = document.scrollingElement as HTMLElement;
      if (!main || !khung || !footer) return null;
      const cs = getComputedStyle(main);
      return {
        cao: window.innerHeight,
        cuonTong: goc.scrollHeight,
        khung: khung.getBoundingClientRect().height,
        dem: parseFloat(cs.paddingTop) + parseFloat(cs.paddingBottom),
        footer: footer.getBoundingClientRect().height,
        daySatFooter: footer.getBoundingClientRect().bottom,
      };
    });
    if (!d)
      return {
        viPham: ['không tìm thấy <main> có khung hoặc <footer> — trang không dựng khung xác thực'],
      };
    const can = d.dem + d.khung + d.footer;
    if (d.cao + DUNG_SAI_PX < can) {
      return {
        viPham: [],
        ghiChu: `khung nhìn ${d.cao}px thấp hơn nội dung ${can.toFixed(1)}px — trang phải cuộn, không đo`,
      };
    }
    const viPham: string[] = [];
    if (d.cuonTong > d.cao + DUNG_SAI_PX) {
      viPham.push(
        `trang cuộn dọc: chiều cao tài liệu ${d.cuonTong}px > khung nhìn ${d.cao}px, trong khi nội dung chỉ cần ${can.toFixed(1)}px`,
      );
    }
    if (Math.abs(d.daySatFooter - d.cao) > DUNG_SAI_PX) {
      viPham.push(
        `Footer không sát đáy: mép dưới ở ${d.daySatFooter.toFixed(1)}px, đáy khung nhìn ${d.cao}px`,
      );
    }
    return { viPham };
  },
);
