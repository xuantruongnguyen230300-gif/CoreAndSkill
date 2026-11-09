// Ca khói chung của MÀN DANH SÁCH sau khi chuyển sang khối liền (docs/Design/Templates/ListScreen.md §2–§4,
// Components/DataTable.md §"Khung" và §"Cột hành động", Components/Toolbar.md §"Cách đặt"). Các phép đo ở đây ĐO
// TRÊN TRÌNH DUYỆT thứ unit test không dựng được: chuỗi flex thật trong khung ứng dụng, `:hover` thật, thứ tự Tab thật.
// Không phép đo nào khẳng định theo chuỗi tiếng Việt (hướng khoá K21); bộ chọn bám class của component Core.
import { readFileSync } from 'node:fs';

import type { Page } from '@playwright/test';

import { expect } from '../ha-tang/fixture';

/** Bề rộng trần ô tìm — `--toolbar-search-w-max` (DESIGN.md §6.5). */
const TRAN_O_TIM_PX = 360;
/** Sàn vùng cuộn — `--data-table-scroll-h-min` (DESIGN.md §6.5). */
const SAN_VUNG_CUON_PX = 280;

/** Khối liền: Toolbar attached trong khung; PageHeader không nút; ô tìm có trần. */
export async function kiemKhoiLien(page: Page, url: string): Promise<void> {
  await page.goto(url);
  await expect(page.locator('h1')).toBeVisible();
  const khung = page.locator('app-data-table .data-table');
  await expect(khung).toHaveCount(1);
  await expect(khung.locator('.data-table__dau app-toolbar .toolbar--attached')).toHaveCount(1);
  await expect(page.locator('app-page-header button, app-page-header a')).toHaveCount(0);
  await expect(khung.locator('.toolbar__hanh-dong').first()).toBeVisible();

  // Một viền ngoài duy nhất; dải phân trang nằm TRONG khung.
  await expect(khung).toHaveCSS('border-top-width', '1px');
  expect(await khung.locator('app-pagination').count()).toBeLessThanOrEqual(1);

  // Ô tìm không vượt trần (đo ở khung rộng ≥ $bp-md).
  const o = page.locator('.toolbar__tim').first();
  const rong = (await o.boundingBox())!.width;
  expect(rong).toBeLessThanOrEqual(TRAN_O_TIM_PX + 1);
}

/** Chỉ thân bảng cuộn: trang/`main` không cuộn khi nội dung vừa khung; vùng cuộn dọc trong khung; th dính đỉnh. */
export async function kiemChiThanBangCuon(page: Page, url: string): Promise<void> {
  await page.goto(url);
  await expect(page.locator('app-data-table .data-table__khung')).toBeVisible();
  const khungNhin = page.locator('.data-table__khung');
  const so = await page.evaluate(() => {
    const main = document.querySelector('main')!;
    const khung = document.querySelector('.data-table__khung')!;
    return {
      mainCuon: main.scrollHeight - main.clientHeight,
      trangCuon: document.documentElement.scrollHeight - window.innerHeight,
      khungCuon: khung.scrollHeight - khung.clientHeight,
    };
  });
  expect(so.trangCuon, 'trang không cuộn ở mức body').toBeLessThanOrEqual(1);
  expect(so.mainCuon, 'main không có gì để cuộn khi khối vừa chiều cao').toBeLessThanOrEqual(1);

  // Cuộn trong khung (nếu bảng đủ dài) thì th giữ nguyên ở đỉnh vùng cuộn.
  if (so.khungCuon > 0) {
    const th = page.locator('thead th').first();
    const topTruoc = (await th.boundingBox())!.y;
    await khungNhin.evaluate((e) => (e.scrollTop = 200));
    const topSau = (await th.boundingBox())!.y;
    expect(Math.abs(topSau - topTruoc)).toBeLessThanOrEqual(1);
  }
}

/** Màn thấp hơn tổng các dải + sàn: `main` cuộn, vùng cuộn của bảng giữ đúng sàn, không co tiếp. */
export async function kiemSanVungCuon(page: Page, url: string): Promise<void> {
  await page.setViewportSize({ width: 1280, height: 420 });
  await page.goto(url);
  await expect(page.locator('app-data-table .data-table__khung')).toBeVisible();
  const cao = (await page.locator('.data-table__than').boundingBox())!.height;
  expect(cao).toBeGreaterThanOrEqual(SAN_VUNG_CUON_PX - 1);
  const mainCuon = await page.evaluate(() => {
    const m = document.querySelector('main')!;
    return m.scrollHeight - m.clientHeight;
  });
  expect(mainCuon, 'đường lui duy nhất: main cuộn').toBeGreaterThan(0);
}

/**
 * Cột hành động (DataTable.md §"Cột hành động"): ghim phải; nghỉ ẩn bằng `opacity` nhưng vẫn trong thứ tự Tab;
 * rê chuột / focus thì hiện; nút KHÔNG `display:none`/`visibility:hidden`.
 */
export async function kiemCotHanhDong(page: Page, url: string): Promise<void> {
  await page.goto(url);
  const th = page.locator('thead th.data-table__o-hanh-dong');
  await expect(th).toHaveCount(1);
  await expect(th.locator('.sr-only')).not.toHaveText('');

  const hang = page.locator('tbody tr').first();
  const o = hang.locator('td.data-table__o-hanh-dong');
  const vung = o.locator('.data-table__hanh-dong');
  const nut = vung.locator('button').first();
  await expect(o).toHaveCount(1);
  await expect(o).toHaveCSS('position', 'sticky');
  await expect(o).toHaveCSS('right', '0px');
  expect(await hang.locator('td').last().getAttribute('class')).toContain(
    'data-table__o-hanh-dong',
  );
  await expect(nut).toHaveCSS('display', /^(?!none)/);
  await expect(nut).toHaveCSS('visibility', 'visible');

  // Nghỉ: opacity 0 (đưa chuột ra xa trước đã).
  await page.mouse.move(0, 0);
  await expect(vung).toHaveCSS('opacity', '0');

  // Rê chuột vào dòng → hiện; chuột ra → ẩn lại.
  await hang.hover();
  await expect(vung).toHaveCSS('opacity', '1');
  await page.mouse.move(0, 0);
  await expect(vung).toHaveCSS('opacity', '0');

  // Bàn phím: Tab tới được nút trong ô, và khi nút nhận focus thì ô hiện — không bao giờ focus vào nút vô hình.
  await page.locator('.data-table__khung').focus();
  let toi = false;
  for (let i = 0; i < 20 && !toi; i++) {
    await page.keyboard.press('Tab');
    toi = await vung
      .locator(':focus')
      .count()
      .then((n) => n > 0);
  }
  expect(toi, 'Tab phải tới được nút trong cột hành động').toBe(true);
  await expect(vung).toHaveCSS('opacity', '1');

  // Focus ở phần tử KHÁC trong dòng (liên kết ô đầu) cũng làm nút của dòng hiện (DataTable.md §"Cột hành động").
  await page.mouse.move(0, 0);
  const lienODau = hang.locator('td').first().locator('a, button').first();
  if ((await lienODau.count()) > 0) {
    await lienODau.focus();
    await expect(vung).toHaveCSS('opacity', '1');
  }
}

/**
 * Vùng báo kết quả (DataTable.md §"Vùng báo kết quả"): đúng một vùng `role="status"` trong khung bảng, không còn
 * `aria-live` nào khác; mở màn với bộ lọc ra 0 bản ghi (`urlKhongCoKetQua`) thì vùng mang câu "không có kết quả".
 * Câu đọc từ `public/i18n/vi.json` (hướng khoá K21: không gõ chuỗi tiếng Việt vào phép đo).
 */
export async function kiemVungBaoKetQuaKhiKhongCo(
  page: Page,
  urlKhongCoKetQua: string,
): Promise<void> {
  const vi = JSON.parse(readFileSync('public/i18n/vi.json', 'utf-8')) as {
    chung: { bang: { khongCoKetQua: string } };
  };
  await page.goto(urlKhongCoKetQua);
  await expect(page.locator('h1')).toBeVisible();
  const vung = page.locator('app-data-table [role="status"]');
  await expect(vung).toHaveCount(1);
  await expect(page.locator('app-data-table [aria-live]')).toHaveCount(0);
  await expect(vung).toHaveText(vi.chung.bang.khongCoKetQua);
}
