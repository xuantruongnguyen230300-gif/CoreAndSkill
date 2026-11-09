import { defineConfig, devices } from '@playwright/test';

import type { TuyChonLuoi } from './e2e/ha-tang/fixture';

// ADR-0117 QC1, QC3. Chromium do Playwright cài theo đúng bản đã ghim (quyết định 4): cùng một lệnh
// trên máy dev và CI, không phụ thuộc Chrome hệ thống.
//
// Bốn project = ba khung nhìn × hai theme theo QC3: `rong` 1280×800 (Sidebar cố định, $bp-lg), `vua` 1024×768
// ($bp-md…$bp-lg), `hep` 390×844 giả lập cảm ứng (dưới $bp-xs; cảm ứng để `(hover: hover)` sai như máy thật);
// `sang` ở cả ba, `toi` chỉ ở `rong`. `rong-sang` là project đủ — chạy mọi spec. Ba project còn lại chỉ chạy
// các spec màn dưới e2e/man/ (đợt 1 xác thực, đợt 2 sau đăng nhập) và chỉ những test mang thẻ của khoảng QC3 ứng với nó:
//   @mac-dinh-a    lớp A ở trạng thái mặc định     → cả bốn project
//   @mac-dinh-axe  axe ở trạng thái mặc định        → rong-sang, rong-toi, hep-sang
//   @khac-a        lớp A ở trạng thái khác          → rong-sang, hep-sang
//   @khac-axe      axe ở trạng thái khác            → rong-sang
//   @tuong-tac     trạng thái tương tác (A5, A7)    → rong-sang, rong-toi
// Lọc bằng thẻ khai ở đây — không dùng lệnh bỏ qua test (F57 cấm).
const CONG = 4300;
const SPEC_CO_LOP_A = ['**/man/*.spec.ts'];

export default defineConfig<TuyChonLuoi>({
  testDir: './e2e',
  testMatch: '**/*.spec.ts',
  forbidOnly: !!process.env['CI'],
  retries: 0,
  workers: 1,
  reporter: [['list'], ['html', { open: 'never' }]],
  use: {
    baseURL: `http://localhost:${CONG}`,
    locale: 'vi-VN',
    timezoneId: 'Asia/Ho_Chi_Minh',
    reducedMotion: 'reduce',
  },
  projects: [
    {
      name: 'rong-sang',
      use: { ...devices['Desktop Chrome'], viewport: { width: 1280, height: 800 }, theme: 'sang' },
    },
    {
      name: 'vua-sang',
      testMatch: SPEC_CO_LOP_A,
      grep: /@mac-dinh-a\b/,
      use: { ...devices['Desktop Chrome'], viewport: { width: 1024, height: 768 }, theme: 'sang' },
    },
    {
      name: 'hep-sang',
      testMatch: SPEC_CO_LOP_A,
      grep: /@(mac-dinh-a\b|mac-dinh-axe|khac-a\b)/,
      use: {
        ...devices['Desktop Chrome'],
        viewport: { width: 390, height: 844 },
        hasTouch: true,
        isMobile: true,
        theme: 'sang',
      },
    },
    {
      name: 'rong-toi',
      testMatch: SPEC_CO_LOP_A,
      grep: /@(mac-dinh-a\b|mac-dinh-axe|tuong-tac)/,
      use: { ...devices['Desktop Chrome'], viewport: { width: 1280, height: 800 }, theme: 'toi' },
    },
  ],
  webServer: {
    command: 'npx ng serve --configuration e2e',
    url: `http://localhost:${CONG}`,
    reuseExistingServer: !process.env['CI'],
    timeout: 240_000,
  },
});
