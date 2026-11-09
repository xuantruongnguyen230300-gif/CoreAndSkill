// Hàm dùng chung của các ca khói (F2, ADR-0117): mở một URL, chờ trang yên, kiểm đúng một <h1> đang hiện.
// Không phải phép đo lớp A (đó là T3) — chỉ chứng minh "vào được" và dữ liệu giả đủ để màn dựng xong.
// H1 (không lỗi console / ngoại lệ trang) và H2 (không request ngoài ca giả) do fixture `apiGia` canh cuối test.
import type { Page } from '@playwright/test';

import { expect } from '../ha-tang/fixture';

export async function vaoVaKiemMotH1(page: Page, url: string): Promise<void> {
  await page.goto(url);
  const h1 = page.locator('h1');
  await expect(h1).toHaveCount(1);
  await expect(h1).toBeVisible();
  // Chờ các request của màn xong: request chưa khai báo (nếu có) hiện ra trước khi test kết thúc (H2).
  await page.waitForLoadState('networkidle');
}
