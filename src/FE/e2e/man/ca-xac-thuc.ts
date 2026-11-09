// Hàm dùng chung của spec nhóm màn xác thực (đợt 1, T3 của ADR-0117): dựng trạng thái của màn, và sinh
// MỘT test cho MỖI phép đo lớp A — tên test nêu mã phép đo và trạng thái, đỏ thì biết đo gì hỏng ở đâu.
// Thẻ (`@mac-dinh-a`...) quyết định project nào chạy test đó (playwright.config.ts, bảng QC3) — không dùng lệnh bỏ qua test (F57).
import type { Locator, Page } from '@playwright/test';

import type { ApiGia } from '../ha-tang/api-gia';
import { chupThamKhao } from '../ha-tang/anh-tham-khao';
import { quetAxe } from '../ha-tang/axe';
import { expect, test } from '../ha-tang/fixture';
import type { ThemeLuoi } from '../ha-tang/fixture';

// `@mot-khung`: phép đo tự đặt khung nhìn (A9) — chạy một lần ở project đủ `rong-sang`, không có ở ba project còn lại.
export type TheKhung =
  '@mac-dinh-a' | '@mac-dinh-axe' | '@khac-a' | '@khac-axe' | '@tuong-tac' | '@mot-khung';

/** Dựng một trạng thái của màn; trả hàm dọn (giải phóng request treo...) nếu có. */
export type MoTrangThai = (page: Page, apiGia: ApiGia) => Promise<(() => void) | void>;

export interface PhepDoCuaMan {
  /** Mã và tên hiện ở tên test. */
  readonly ten: string;
  readonly chay: (page: Page) => Promise<void>;
  /** Lỗi đã ghi ở bảng *Đã có → còn thiếu* của spec: tên dòng. Có thì test mang `test.fail()` — tự hết hạn khi sửa xong. */
  readonly loiDaBiet?: string;
}

export function nutGui(page: Page): Locator {
  return page.locator('main form button[type=submit]');
}

/** Chạy một trạng thái với dọn dẹp chắc chắn (giải phóng request treo kể cả khi phép đo đỏ). */
async function voiTrangThai(
  page: Page,
  apiGia: ApiGia,
  theme: ThemeLuoi,
  mo: MoTrangThai,
  fn: () => Promise<void>,
): Promise<void> {
  const don = await mo(page, apiGia);
  try {
    // Theme tối mà không áp thật thì mọi phép đo đọc token của theme sáng và xanh nhầm — kiểm trước khi đo.
    await expect(page.locator('html')).toHaveAttribute(
      'data-theme',
      theme === 'toi' ? 'dark' : 'light',
    );
    await fn();
  } finally {
    if (don) don();
  }
}

/** Một test cho mỗi phép đo lớp A của một trạng thái. Test đầu tiên đính ảnh tham khảo của trạng thái. */
export function lopA(
  man: string,
  trangThai: string,
  the: TheKhung,
  mo: MoTrangThai,
  cacPhep: readonly PhepDoCuaMan[],
): void {
  test.describe(`${man} · ${trangThai} · lớp A`, () => {
    cacPhep.forEach((p, i) => {
      test(`${p.ten} — ${trangThai}`, { tag: [the] }, async ({ page, apiGia, theme }, testInfo) => {
        if (p.loiDaBiet) test.fail(true, `lỗi đã biết — ${p.loiDaBiet}`);
        await voiTrangThai(page, apiGia, theme, mo, async () => {
          if (i === 0) await chupThamKhao(page, testInfo, `${man}--${trangThai}`);
          await p.chay(page);
        });
      });
    });
  });
}

/** Một test axe WCAG A/AA cho một trạng thái (ADR-0117 QC5). */
export function axeTrangThai(
  man: string,
  trangThai: string,
  the: TheKhung,
  mo: MoTrangThai,
  loiDaBiet?: string,
): void {
  test(
    `${man} · ${trangThai} · axe WCAG A/AA không vi phạm`,
    { tag: [the] },
    async ({ page, apiGia, theme }, testInfo) => {
      // Lỗi đã biết (ADR-0117 QC6): test mang `test.fail()` kèm tên dòng spec — sửa xong mà quên gỡ thì test xanh ngoài dự kiến, Playwright báo đỏ.
      if (loiDaBiet) test.fail(true, `lỗi đã biết — ${loiDaBiet}`);
      await voiTrangThai(page, apiGia, theme, mo, async () => {
        await quetAxe(page, man, testInfo.project.name);
      });
    },
  );
}

export { expect };
