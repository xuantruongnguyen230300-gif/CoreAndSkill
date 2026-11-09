// Ca chung cho LỚP NỔI NEO (Menu, Autocomplete) dưới giảm chuyển động. Chạy ở project `rong-sang`, nơi
// `playwright.config.ts` đặt `reducedMotion: 'reduce'`. Hồi quy cần khoá: lớp nổi để tại chỗ trong khung cuộn của
// bảng/hộp thoại bị cú cuộn của chính nó đóng ngay (~15ms) — khi có hoạt ảnh, bộ nghe cuộn của PrimeNG gắn sau
// hoạt ảnh nên cú cuộn lọt qua; khi giảm chuyển động thì gắn ngay. Bộ chọn bám vai trò ARIA, không chuỗi tiếng Việt.
import type { Locator, Page } from '@playwright/test';

import { expect } from '../ha-tang/fixture';

/** Thời gian chờ cho lớp nổi "lộ" việc tự đóng: đóng xảy ra trong vài chục ms sau khi hiện. */
const CHO_LO_TU_DONG_DONG_MS = 400;

/** Chờ lớp nổi đứng yên: hai khung hình rồi một quãng đủ cho bộ nghe cuộn/nhấp ngoài kịp chạy. */
export async function choLopNoiDungYen(page: Page): Promise<void> {
  await page.evaluate(
    (ms) =>
      new Promise<void>((xong) =>
        requestAnimationFrame(() => requestAnimationFrame(() => setTimeout(xong, ms))),
      ),
    CHO_LO_TU_DONG_DONG_MS,
  );
}

/**
 * Mở menu của một nút `aria-haspopup="menu"` và khẳng định: menu còn mở sau khi đứng yên, có `role="menu"`, nằm trong
 * khung nhìn và sát nút neo (không văng ra ngoài toạ độ của ô cha định vị), nút mang `aria-expanded="true"`.
 * Trả về locator của menu để ca gọi chọn mục.
 */
export async function moMenuVaKiemConMo(page: Page, nut: Locator): Promise<Locator> {
  await nut.click();
  const menu = page.getByRole('menu');
  await expect(menu).toBeVisible();
  await choLopNoiDungYen(page);
  await expect(menu).toBeVisible();
  await expect(menu).toHaveCount(1);
  await expect(nut).toHaveAttribute('aria-expanded', 'true');

  // Khung `.p-menu` gồm cả tiêu đề (nếu có) nằm trên `role=menu`; neo đo theo khung, không theo danh sách bên trong.
  const m = (await page.locator('.p-menu').boundingBox())!;
  const n = (await nut.boundingBox())!;
  const khung = page.viewportSize()!;
  expect(m.x, 'menu không tràn trái khung nhìn').toBeGreaterThanOrEqual(0);
  expect(m.x + m.width, 'menu không tràn phải khung nhìn').toBeLessThanOrEqual(khung.width);
  expect(m.y + m.height, 'menu không tràn đáy khung nhìn').toBeLessThanOrEqual(khung.height);
  expect(m.x <= n.x + n.width && m.x + m.width >= n.x, 'menu chồng trục ngang với nút neo').toBe(
    true,
  );
  expect(Math.abs(m.y - (n.y + n.height)), 'menu sát cạnh dưới nút neo').toBeLessThan(24);
  return menu;
}
