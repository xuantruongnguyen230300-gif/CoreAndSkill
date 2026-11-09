// Trang giả cùng origin với app, dựng bằng `page.route` — canary không cần dev server chạy.
// Origin của trang là origin thật của app nên CORS tới API giả là CORS thật (khác nguồn, có credentials).
import type { Page } from '@playwright/test';

import { docApiBaseUrl } from '../api-gia';

export const API = docApiBaseUrl();

export async function moTrangCanary(page: Page): Promise<void> {
  // Đăng ký SAU catch-all của fixture `apiGia` nên được xét trước.
  await page.route('**/__canary', (route) =>
    route.fulfill({ contentType: 'text/html', body: '<!doctype html><title>canary</title>' }),
  );
  await page.goto('/__canary');
}

export interface KetQuaFetch {
  readonly status?: number;
  readonly body?: unknown;
  readonly loi?: string;
}

/** fetch từ trang, đúng như app làm: khác nguồn, kèm cookie. Không ném khi request bị huỷ — trả `loi`. */
export async function goi(
  page: Page,
  url: string,
  init: { method?: string; headers?: Record<string, string>; body?: string } = {},
): Promise<KetQuaFetch> {
  return page.evaluate(
    async ({ url, init }) => {
      try {
        const r = await fetch(url, { credentials: 'include', ...init });
        return { status: r.status, body: await r.json() };
      } catch (e) {
        return { loi: String(e) };
      }
    },
    { url, init },
  );
}

export const JSON_XSRF = { 'content-type': 'application/json', 'X-XSRF-TOKEN': 'xsrf-gia' };
