import { defineConfig, devices } from '@playwright/test';

// Cấu hình cho các ca đỏ của canary — chỉ được `canary-api-gia.spec.ts` gọi. Không `webServer`:
// canary dựng trang giả bằng `page.route`, không cần dev server. `baseURL` chỉ cho origin của trang giả.
export default defineConfig({
  testDir: '.',
  testMatch: '**/*.canary.ts',
  retries: 0,
  workers: 1,
  reporter: [['json']],
  use: { ...devices['Desktop Chrome'], baseURL: 'http://localhost:4300', locale: 'vi-VN' },
});
