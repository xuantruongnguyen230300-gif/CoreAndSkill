import type { Page } from '@playwright/test';

import { choTrangYen, phepDo } from './chung';

export interface TuyChonA1 {
  /** Màn xác thực: `<h1>` phải nằm trong `<main>` (AuthCard.md §Accessibility). */
  readonly trongMain?: boolean;
}

/**
 * A1 — màn dựng nội dung: đúng một `<h1>`, đang hiện, sau khi điều hướng tới URL của màn.
 * Bắt lỗi "URL `/` ra trang trắng": nhánh route không component thì trang không có `<h1>` nào.
 */
export const a1MotH1 = phepDo(
  {
    ma: 'A1',
    ten: 'đúng một <h1> đang hiện',
    nguon: [
      'docs/Design/Components/PageHeader.md:111 (§Accessibility — tiêu đề là <h1> duy nhất của trang)',
      'docs/Design/Components/AuthCard.md:145 (§Accessibility — tiêu đề là <h1> duy nhất, trong <main>)',
    ],
  },
  async (page: Page, tuyChon: TuyChonA1 = {}) => {
    await choTrangYen(page);
    const viPham: string[] = [];
    const ds = await page.locator('h1').evaluateAll((es) =>
      es.map((e) => ({
        hien: (e as HTMLElement).checkVisibility({ checkVisibilityCSS: true }),
        trongMain: e.closest('main') !== null,
      })),
    );
    if (ds.length !== 1) viPham.push(`trang có ${ds.length} thẻ <h1>, spec đòi đúng 1`);
    else {
      if (!ds[0]!.hien) viPham.push('<h1> có trong DOM nhưng không hiện');
      if (tuyChon.trongMain && !ds[0]!.trongMain) viPham.push('<h1> không nằm trong <main>');
    }
    return { viPham };
  },
);
