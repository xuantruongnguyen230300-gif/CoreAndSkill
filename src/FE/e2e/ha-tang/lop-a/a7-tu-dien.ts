import type { Page } from '@playwright/test';

import { choTrangYen, docToken, phepDo } from './chung';
import { docDiemAnh } from './png';

/**
 * A7 — ô được trình duyệt tự điền giữ nền và chữ của trạng thái nghỉ (và của `disabled` khi đã khoá).
 *
 * CÁCH TÁI HIỆN (đo 2026-10-06, Playwright 1.63.0 + Chromium kèm theo): Chromium không giao diện không tự
 * điền, nhưng `CSS.forcePseudoState` của DevTools Protocol nhận `autofill` và khiến stylesheet của TRÌNH
 * DUYỆT (nền xanh nhạt `!important`) cùng stylesheet của app đều thấy ô khớp `:autofill`. Phép đo lấy mẫu
 * một điểm ảnh THẬT trong ô (không phải computed style: nền tự điền do UA khai `!important` nên
 * `background-color` luôn là xanh nhạt; thứ người dùng thấy là lớp bóng `inset` phủ lên) và đọc
 * `-webkit-text-fill-color` — màu chữ thật.
 * GIỚI HẠN: kiểm CSS của app, không kiểm hành vi tự điền của trình duyệt thật — ca đó vẫn kiểm tay.
 */
export const a7TuDien = phepDo(
  {
    ma: 'A7',
    ten: 'ô tự điền giữ nền --color-surface và chữ --color-text (disabled: nền/chữ disabled)',
    nguon: [
      'docs/Design/DESIGN.md:248 (§2.6 Ô nhập chữ — dòng Trình duyệt tự điền: nền --color-surface, chữ --color-text; disabled vẫn thắng)',
      'docs/Design/Components/AuthField.md:88 (§Trạng thái default — ô vừa tự điền giữ nguyên nền và chữ)',
    ],
  },
  async (page: Page, vung: string = 'main form') => {
    await choTrangYen(page);
    const o = page.locator(
      `${vung} input:not([type=hidden]):not([type=checkbox]):not([type=radio])`,
    );
    const n = await o.count();
    if (n === 0)
      return { viPham: [`không có ô nhập nào trong "${vung}" — phép đo không có gì để đo`] };
    const mong = {
      nen: await docToken(page, 'color', 'var(--color-surface)'),
      chu: await docToken(page, 'color', 'var(--color-text)'),
      nenKhoa: await docToken(page, 'color', 'var(--color-surface-3)'),
      chuKhoa: await docToken(page, 'color', 'var(--color-text-disabled)'),
    };
    const cdp = await page.context().newCDPSession(page);
    await cdp.send('DOM.enable');
    await cdp.send('CSS.enable');
    const viPham: string[] = [];
    try {
      for (let i = 0; i < n; i++) {
        const id = await o.nth(i).evaluate((e) => e.id || e.getAttribute('name') || e.tagName);
        for (const khoa of [false, true]) {
          await o.nth(i).fill('abc');
          await o.nth(i).evaluate((e, k) => {
            (e as HTMLInputElement).disabled = k;
            (e as HTMLInputElement).blur();
          }, khoa);
          await page.mouse.move(0, 0);
          const { root } = await cdp.send('DOM.getDocument', { depth: 0 });
          const { nodeId } = await cdp.send('DOM.querySelector', {
            nodeId: root.nodeId,
            selector: `input[id="${id}"]`,
          });
          await cdp.send('CSS.forcePseudoState', { nodeId, forcedPseudoClasses: ['autofill'] });
          const khop = await o.nth(i).evaluate((e) => e.matches(':autofill'));
          if (!khop) {
            viPham.push(
              `ô #${id}: ép :autofill qua CDP không có tác dụng — hạ tầng đo hỏng, không phải lỗi của ô`,
            );
            continue;
          }
          const h = await o.nth(i).boundingBox();
          if (!h) throw new Error(`ô #${id} không có hộp bao`);
          const x = Math.floor(h.x + h.width * 0.6);
          const y = Math.floor(h.y) + 6; // dưới viền (≤ 3px dày), trên dòng chữ — vùng nền thuần
          const diem = docDiemAnh(await page.screenshot({ clip: { x, y, width: 1, height: 1 } }));
          const chu = await o
            .nth(i)
            .evaluate((e) => getComputedStyle(e).getPropertyValue('-webkit-text-fill-color'));
          const trangThai = khoa ? 'tự điền + disabled' : 'tự điền';
          const mongNen = khoa ? mong.nenKhoa : mong.nen;
          const mongChu = khoa ? mong.chuKhoa : mong.chu;
          if (diem !== mongNen)
            viPham.push(`ô #${id} · ${trangThai}: nền thật ${diem}, spec đòi ${mongNen}`);
          if (chu !== mongChu)
            viPham.push(`ô #${id} · ${trangThai}: chữ ${chu}, spec đòi ${mongChu}`);
          await cdp.send('CSS.forcePseudoState', { nodeId, forcedPseudoClasses: [] });
          if (khoa) await o.nth(i).evaluate((e) => ((e as HTMLInputElement).disabled = false));
        }
      }
    } finally {
      await cdp.detach();
    }
    return { viPham };
  },
);
