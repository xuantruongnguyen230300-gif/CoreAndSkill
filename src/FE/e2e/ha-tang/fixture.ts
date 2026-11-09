// Fixture dùng chung của lưới giao diện (ADR-0117): spec import `test` và `expect` từ đây, không từ
// `@playwright/test`. Fixture `apiGia` tự chạy ở mọi test — API đóng mặc định không phải thứ người
// viết spec phải nhớ bật; cuối test, mọi vi phạm H1/H2 thành đỏ.
// Fixture tự chạy thứ hai: theme (đặt vào `localStorage` TRƯỚC khi trang tải, đúng cơ chế script nội tuyến của
// `index.html`). Đồng hồ cố định (ADR-0117 QC1, `page.clock`) CHƯA bật — đo 2026-10-06: `page.clock.setFixedTime`
// cài `ClockController` bọc mọi callback timer, nên ngoại lệ ném trong `setTimeout` thành `console.error` thay vì
// `pageerror` (canary `DO ngoại lệ trang` ở canary-api-gia.spec.ts đỏ). Phép đo lớp A của đợt 1 không phụ thuộc ngày giờ.
import { expect, test as goc } from '@playwright/test';

import { ApiGia } from './api-gia';

export type ThemeLuoi = 'sang' | 'toi';

/** Tuỳ chọn của lưới, đặt ở `use` của từng project trong `playwright.config.ts`. */
export interface TuyChonLuoi {
  readonly theme: ThemeLuoi;
}

export const test = goc.extend<{ apiGia: ApiGia; datTheme: void } & TuyChonLuoi>({
  theme: ['sang', { option: true }],
  datTheme: [
    async ({ page, theme }, use) => {
      await page.addInitScript(
        (t: string) => {
          try {
            localStorage.setItem('theme', t);
          } catch {
            // Trang about:blank không có localStorage — không phải lỗi.
          }
        },
        theme === 'toi' ? 'dark' : 'light',
      );
      await use();
    },
    { auto: true },
  ],
  apiGia: [
    async ({ page, baseURL }, use) => {
      if (!baseURL) throw new Error('playwright.config.ts thiếu use.baseURL');
      const api = await ApiGia.dung(page, baseURL);
      await use(api);
      api.kiemTraCuoiTest();
    },
    { auto: true },
  ],
});

export { expect };
