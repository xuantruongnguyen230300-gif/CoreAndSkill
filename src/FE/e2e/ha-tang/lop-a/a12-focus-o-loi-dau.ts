import type { Page } from '@playwright/test';

import { choTrangYen, phepDo } from './chung';

/**
 * A12 — bấm gửi khi còn ô lỗi thì focus về ô lỗi ĐẦU TIÊN của form. Gọi SAU khi trạng thái "kiểm tra dữ liệu" đã
 * dựng (đã bấm gửi, lỗi đã hiện). Ô lỗi = phần tử hiện mang `aria-invalid="true"` trong `vung`, theo thứ tự DOM.
 * Không có ô lỗi nào thì phép đo không có gì để đo và báo vi phạm — không xanh rỗng.
 */
export const a12FocusOLoiDau = phepDo(
  {
    ma: 'A12',
    ten: 'gửi form còn lỗi: focus về ô lỗi đầu tiên',
    nguon: [
      'docs/Design/Components/FormRow.md:140 (§Accessibility — Submit lỗi: đặt focus vào trường lỗi đầu tiên)',
      'docs/Design/Components/FormRow.md:100 (người dùng bấm submit: kèm cuộn tới trường lỗi đầu tiên và đặt focus vào đó)',
      'docs/Design/Screens/01-dang-nhap.md:185 (kiểm tra dữ liệu — bấm gửi khi còn ô thiếu: focus về ô lỗi đầu tiên)',
    ],
  },
  async (page: Page, vung: string = 'main form') => {
    await choTrangYen(page);
    const d = await page.evaluate((vung) => {
      const hien = (e: Element) => (e as HTMLElement).checkVisibility({ checkVisibilityCSS: true });
      const ten = (e: Element | null) =>
        e
          ? `<${e.tagName.toLowerCase()}${e.id ? '#' + e.id : ''}${e.getAttribute('name') ? `[name=${e.getAttribute('name')}]` : ''}>`
          : 'không phần tử nào';
      const loi = Array.from(document.querySelectorAll(`${vung} [aria-invalid="true"]`)).filter(
        hien,
      );
      return {
        soLoi: loi.length,
        dau: ten(loi[0] ?? null),
        trungDau: !!loi[0] && document.activeElement === loi[0],
        dangGiu: ten(document.activeElement),
      };
    }, vung);
    if (d.soLoi === 0)
      return {
        viPham: [
          `không có ô lỗi nào trong "${vung}" — chưa dựng được trạng thái kiểm tra dữ liệu để đo`,
        ],
      };
    return {
      viPham: d.trungDau
        ? []
        : [`focus đang ở ${d.dangGiu}, spec đòi ô lỗi đầu tiên ${d.dau} (trong ${d.soLoi} ô lỗi)`],
    };
  },
);
