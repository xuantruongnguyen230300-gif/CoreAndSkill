import type { Page } from '@playwright/test';

import { DUNG_SAI_PX, choTrangYen, phepDo } from './chung';

/**
 * A10 — ô mật khẩu chừa chỗ cho nút hiện/ẩn: chữ gõ vào không chạy xuống dưới nút.
 * "Nút hiện/ẩn" của một ô = `<button>` đang hiện nằm CHỒNG lên hộp của ô (nút ở ngoài hộp ô thì chữ không thể chạy
 * dưới nó, phép đo không có gì để báo). Điều kiện: mép phải của vùng chữ — mép phải ô trừ viền phải và đệm phải —
 * không vượt mép trái của nút. Đo hình học trên trang đã render, không đọc token: spec nói bằng kết quả
 * ("chuỗi mật khẩu dài không chạy xuống dưới nút"), công thức `calc()` là việc của CSS.
 */
export const a10OMatKhauDemPhai = phepDo(
  {
    ma: 'A10',
    ten: 'ô mật khẩu: đệm phải chừa đủ chỗ cho nút hiện/ẩn, chữ không chạy dưới nút',
    nguon: [
      'docs/Design/Components/AuthField.md:82 (§Kích thước — với password, đệm phải cộng tương tự để chuỗi mật khẩu dài không chạy xuống dưới nút hiện/ẩn)',
      'docs/Design/Components/Input.md:63 (§Biến thể password — đệm phải chừa chỗ nút, cùng khuôn AuthField)',
    ],
  },
  async (page: Page, vung: string = 'main') => {
    await choTrangYen(page);
    const d = await page.evaluate((vung) => {
      const hien = (e: Element) => (e as HTMLElement).checkVisibility({ checkVisibilityCSS: true });
      const ten = (e: Element) => `<${e.tagName.toLowerCase()}${e.id ? '#' + e.id : ''}>`;
      const ketQua: { o: string; chuaDuoc: number; nut: string; dem: number; canChua: number }[] =
        [];
      let soO = 0;
      for (const o of Array.from(
        document.querySelectorAll(`${vung} input`),
      ) as HTMLInputElement[]) {
        if (o.type === 'hidden' || !hien(o)) continue;
        const ro = o.getBoundingClientRect();
        const nutDe = Array.from(document.querySelectorAll(`${vung} button`)).filter((b) => {
          if (!hien(b)) return false;
          const rb = b.getBoundingClientRect();
          const giaoNgang = rb.left < ro.right && rb.right > ro.left;
          const giaoDoc = rb.top < ro.bottom && rb.bottom > ro.top;
          // nút "của ô": nằm hẳn trong hộp ô theo chiều ngang, không phải nút ngoài hàng
          return giaoNgang && giaoDoc && rb.left >= ro.left && rb.right <= ro.right + 0.5;
        });
        if (nutDe.length === 0) continue;
        soO += 1;
        const s = getComputedStyle(o);
        const mepChuPhai = ro.right - parseFloat(s.borderRightWidth) - parseFloat(s.paddingRight);
        for (const nut of nutDe) {
          const rb = nut.getBoundingClientRect();
          ketQua.push({
            o: ten(o),
            chuaDuoc: mepChuPhai,
            nut: ten(nut),
            dem: parseFloat(s.paddingRight),
            canChua: rb.left,
          });
        }
      }
      return { soO, ketQua };
    }, vung);
    if (d.soO === 0)
      return {
        viPham: [
          `không có ô nhập nào trong "${vung}" mang nút hiện/ẩn chồng lên ô — phép đo không có gì để đo`,
        ],
      };
    const viPham = d.ketQua
      .filter((k) => k.chuaDuoc > k.canChua + DUNG_SAI_PX)
      .map(
        (k) =>
          `ô ${k.o}: vùng chữ kéo tới ${k.chuaDuoc.toFixed(1)}px nhưng nút ${k.nut} bắt đầu ở ${k.canChua.toFixed(1)}px (đệm phải ${k.dem}px) — chữ dài chạy xuống dưới nút`,
      );
    return { viPham };
  },
);
