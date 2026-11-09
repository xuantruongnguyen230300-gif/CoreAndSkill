import { readFileSync, readdirSync } from 'node:fs';
import { resolve } from 'node:path';
import type { Page } from '@playwright/test';

import { choTrangYen, phepDo } from './chung';

const THU_MUC_I18N = resolve(__dirname, '../../../public/i18n');

/** Phân đoạn gốc của mọi khoá dịch (`xacThuc`, `loi`, `chung`...) — đọc từ tệp dịch thật, không chép vào test. */
export function docGocKhoaDich(thuMuc: string = THU_MUC_I18N): readonly string[] {
  const goc = new Set<string>();
  for (const tep of readdirSync(thuMuc).filter((t) => t.endsWith('.json'))) {
    for (const k of Object.keys(JSON.parse(readFileSync(resolve(thuMuc, tep), 'utf8')) as object))
      goc.add(k);
  }
  if (goc.size === 0)
    throw new Error(`Không đọc được khoá dịch nào ở ${thuMuc} — phép đo khoá dịch sẽ xanh rỗng`);
  return [...goc];
}

/**
 * A2 — không `null`, `undefined`, `NaN`, `[object Object]` trong chữ hiển thị và trong `placeholder`,
 * `aria-label`, `title`, `alt`; không chuỗi mang hình khoá dịch hiện ra.
 * Hình khoá dịch: cả nút chữ (hoặc cả giá trị thuộc tính) là một chuỗi `goc.phan.doan` mà `goc` là một
 * khoá gốc có thật trong tệp dịch — thư viện dịch trả chính chuỗi khoá khi thiếu bản dịch.
 */
export const a2ChuHienThi = phepDo(
  {
    ma: 'A2',
    ten: 'không null/undefined/NaN/[object Object] và không khoá dịch trong chữ hiển thị',
    nguon: [
      'docs/Design/Components/AuthField.md:141 (§API — placeholder null nghĩa là không có placeholder)',
      'docs/Design/Components/Input.md:159 (§API — placeholder null, cùng khuôn)',
      'docs/Design/DESIGN.md:776 (§9 — ❌ không để khoá dịch hiện ra thành chữ)',
    ],
  },
  async (page: Page, goc: readonly string[] = docGocKhoaDich()) => {
    await choTrangYen(page);
    const viPham = await page.evaluate((goc) => {
      const XAU = /(?:^|[^\p{L}\p{N}_])(?:null|undefined|NaN)(?![\p{L}\p{N}_])|\[object Object\]/u;
      const HINH_KHOA = /^[A-Za-z][A-Za-z0-9_-]*(?:\.[A-Za-z0-9_-]+)+$/;
      const loi: string[] = [];
      const ten = (e: Element) =>
        `<${e.tagName.toLowerCase()}${e.id ? '#' + e.id : ''}${
          typeof e.className === 'string' && e.className
            ? '.' + e.className.trim().split(/\s+/)[0]
            : ''
        }>`;
      const xet = (noiDung: string, viTri: string) => {
        const t = noiDung.trim();
        if (!t) return;
        if (XAU.test(t)) loi.push(`${viTri}: chữ "${t}" chứa null/undefined/NaN/[object Object]`);
        if (HINH_KHOA.test(t) && goc.includes(t.split('.')[0]!))
          loi.push(`${viTri}: "${t}" là khoá dịch hiện ra thành chữ`);
      };
      const hien = (e: Element) => (e as HTMLElement).checkVisibility({ checkVisibilityCSS: true });
      xet(document.title, 'document.title');
      const dao = document.createTreeWalker(document.body, NodeFilter.SHOW_TEXT);
      for (let n = dao.nextNode(); n; n = dao.nextNode()) {
        const cha = n.parentElement;
        if (!cha || ['SCRIPT', 'STYLE', 'NOSCRIPT', 'TEMPLATE'].includes(cha.tagName) || !hien(cha))
          continue;
        xet(n.textContent ?? '', `chữ trong ${ten(cha)}`);
      }
      for (const e of Array.from(document.body.querySelectorAll('*'))) {
        if (!hien(e)) continue;
        for (const a of ['placeholder', 'aria-label', 'title', 'alt']) {
          const v = e.getAttribute(a);
          if (v !== null) xet(v, `thuộc tính ${a} của ${ten(e)}`);
        }
      }
      return loi;
    }, goc);
    return { viPham };
  },
);
