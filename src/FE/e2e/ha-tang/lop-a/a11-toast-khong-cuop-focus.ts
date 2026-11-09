import { errors } from '@playwright/test';
import type { Page } from '@playwright/test';

import { choTrangYen, phepDo } from './chung';

/** Vùng chứa toast của ứng dụng — `<app-toast>` ở gốc (`app.html`). */
const VUNG_TOAST = 'app-toast';
/** Một toast đang hiện: phần tử PrimeNG `.p-toast-message` nằm trong vùng chứa. */
const MOT_TOAST = `${VUNG_TOAST} .p-toast-message`;

/**
 * A11 — toast lỗi không cướp focus: sau khi một toast hiện ra, phần tử đang giữ focus không nằm trong vùng toast
 * (kể cả nút đóng). Người gọi truyền hàm gây ra lỗi (ví dụ gửi form mà API trả 500 `CORE.SYSTEM.UNEXPECTED`) —
 * phép đo chờ toast hiện, chờ trang yên (kể cả hoạt ảnh vào và các khung hình mà app dùng để trả focus), rồi đọc
 * `document.activeElement`. Không có toast nào hiện thì phép đo không có gì để kết luận và báo vi phạm.
 */
export const a11ToastKhongCuopFocus = phepDo(
  {
    ma: 'A11',
    ten: 'toast lỗi không cướp focus (nút đóng không giữ focus sau khi toast hiện)',
    nguon: [
      'docs/Design/Components/Toast.md:145 (§Accessibility — Bàn phím: Toast không tự cướp focus; người dùng đang gõ dở bị nhảy focus là mất chỗ)',
    ],
  },
  async (page: Page, gayLoi: () => Promise<void>) => {
    await gayLoi();
    try {
      await page.locator(MOT_TOAST).first().waitFor({ state: 'visible', timeout: 5000 });
    } catch (e) {
      if (!(e instanceof errors.TimeoutError)) throw e;
      return {
        viPham: [`không thấy toast nào (${MOT_TOAST}) sau khi gây lỗi — phép đo không có gì để đo`],
      };
    }
    await choTrangYen(page);
    // Vài khung hình sau khi toast chèn vào: chỗ ứng dụng (hay một lỗi cũ) chuyển focus vào nút đóng.
    await page.evaluate(
      () =>
        new Promise<void>((xong) =>
          requestAnimationFrame(() =>
            requestAnimationFrame(() =>
              requestAnimationFrame(() => requestAnimationFrame(() => xong())),
            ),
          ),
        ),
    );
    const giu = await page.evaluate((vung) => {
      const e = document.activeElement as HTMLElement | null;
      if (!e || !e.closest(vung)) return null;
      return `<${e.tagName.toLowerCase()}${e.id ? '#' + e.id : ''}${typeof e.className === 'string' && e.className ? '.' + e.className.trim().split(/\s+/)[0] : ''}>`;
    }, VUNG_TOAST);
    return {
      viPham: giu
        ? [`sau khi toast hiện, focus nằm ở ${giu} trong vùng toast — toast đã cướp focus`]
        : [],
    };
  },
);
