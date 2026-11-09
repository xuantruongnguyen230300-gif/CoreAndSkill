import type { Page } from '@playwright/test';

import { choTrangYen, phepDo } from './chung';

/**
 * A4 — mọi phần tử hiển thị chữ dùng họ ĐẦU của `--font-sans`, kể cả `button`, `input`, `select`, `textarea`
 * (bốn phần tử không kế thừa họ font của trang), và font đó đã nạp. Nhánh `--font-mono` chưa bật: chưa có
 * phần tử nào dùng `var(--font-mono)` để đo (06-testing-strategy.md §6.1).
 * "Có chữ" = có nút văn bản riêng không rỗng; `input`/`select`/`textarea` luôn tính (sẽ hiện giá trị người gõ);
 * `button` chỉ tính khi có chữ — nút chỉ có icon không hiển thị chữ nào.
 */
export const a4HoFont = phepDo(
  {
    ma: 'A4',
    ten: 'mọi phần tử có chữ dùng họ đầu của --font-sans và font đã nạp',
    nguon: [
      'docs/Design/DESIGN.md:367 (§3.1 — Phần tử nào dùng họ nào: kể cả button, input, select, textarea)',
    ],
  },
  async (page: Page) => {
    await choTrangYen(page);
    const viPham = await page.evaluate(() => {
      const hoDau = (v: string) => (v.split(',')[0] ?? '').trim().replace(/^["']|["']$/g, '');
      const mong = hoDau(
        getComputedStyle(document.documentElement).getPropertyValue('--font-sans'),
      );
      const loi: string[] = [];
      if (!mong) return ['không đọc được --font-sans trên :root — token họ font không tồn tại'];
      const ten = (e: Element) =>
        `<${e.tagName.toLowerCase()}${e.id ? '#' + e.id : ''}${
          typeof e.className === 'string' && e.className
            ? '.' + e.className.trim().split(/\s+/)[0]
            : ''
        }>`;
      const dung = new Map<string, string>(); // `${nét}` → mẫu chữ
      for (const e of Array.from(document.body.querySelectorAll('*'))) {
        if (!(e as HTMLElement).checkVisibility({ checkVisibilityCSS: true })) continue;
        if (['SCRIPT', 'STYLE', 'NOSCRIPT', 'TEMPLATE'].includes(e.tagName)) continue;
        const coChuRieng = Array.from(e.childNodes).some(
          (n) => n.nodeType === 3 && (n.textContent ?? '').trim() !== '',
        );
        const loaiNhap =
          ['INPUT', 'SELECT', 'TEXTAREA'].includes(e.tagName) &&
          (e as HTMLInputElement).type !== 'hidden';
        const nutCoChu = e.tagName === 'BUTTON' && (e.textContent ?? '').trim() !== '';
        if (!coChuRieng && !loaiNhap && !nutCoChu) continue;
        const s = getComputedStyle(e);
        const ho = hoDau(s.fontFamily);
        if (ho !== mong)
          loi.push(`${ten(e)} dùng họ font "${ho}", spec đòi "${mong}" (--font-sans)`);
        else dung.set(s.fontWeight, (e.textContent ?? '').trim() || 'a');
      }
      return loi.concat(
        Array.from(dung).flatMap(([net, mau]) => {
          const daKhai = Array.from(document.fonts).some((f) => hoDau(f.family) === mong);
          if (!daKhai) return [`họ "${mong}" không có @font-face nào trong document.fonts`];
          return document.fonts.check(`${net} 16px "${mong}"`, mau)
            ? []
            : [`font "${mong}" nét ${net} chưa nạp xong cho chữ "${mau.slice(0, 20)}"`];
        }),
      );
    });
    return { viPham: [...new Set(viPham)] };
  },
);
