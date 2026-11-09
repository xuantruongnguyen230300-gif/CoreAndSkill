// Hàm dùng chung của spec các màn SAU ĐĂNG NHẬP (đợt 2, T3 lượt b của ADR-0117): dựng trạng thái của màn trong
// khung ứng dụng và sinh test lớp A + axe cho từng trạng thái, theo bảng QC3 — mặc định ở ba khung nhìn, trạng
// thái khác ở `rong`·`sang` và `hep`·`sang`, axe theo cột *axe*.
//
// Phép đo lớp A bật cho màn trong khung: A1 (một <h1>), A2 (chữ hiển thị), A4 (họ font). Các phép đo KHÔNG bật:
//   A3, A6        luật của khung xác thực (AuthCard.md), không áp cho trang trong khung.
//   A5, A7        ô nhập — chỉ bật ở màn có form (xem spec màn); ô tìm của Toolbar không có trạng thái lỗi để đo.
//   A8            không màn nào ngoài 01-dang-nhap có bảng *Cố ý không có* ở `docs/Design/Screens/`.
// Không có phép đo nào khẳng định theo chuỗi tiếng Việt (hướng khoá K21).
import type { Page } from '@playwright/test';

import type { ApiGia, CaGia } from '../ha-tang/api-gia';
import { a1MotH1, a2ChuHienThi, a4HoFont } from '../ha-tang/lop-a';
import { expect } from '../ha-tang/fixture';
import { axeTrangThai, lopA } from './ca-xac-thuc';
import type { MoTrangThai, PhepDoCuaMan } from './ca-xac-thuc';

/** Ba phép đo chung của màn trong khung. */
export const phepDoManTrongKhung: readonly PhepDoCuaMan[] = [
  { ten: 'A1 một h1', chay: (page) => a1MotH1.kiem(page) },
  { ten: 'A2 không null/NaN/khoá dịch', chay: (page) => a2ChuHienThi.kiem(page) },
  { ten: 'A4 họ font --font-sans', chay: (page) => a4HoFont.kiem(page) },
];

export interface TrangThaiMan {
  /** Tên trạng thái — theo mục *Trạng thái* của spec màn. */
  readonly ten: string;
  readonly mo: MoTrangThai;
  /** `true` ⇒ trạng thái `mặc định` (ba khung nhìn + `rong`·`toi`); mặc định `false`. */
  readonly macDinh?: true;
  /** Phép đo riêng của trạng thái, chạy cùng các phép đo chung. */
  readonly phepDoThem?: readonly PhepDoCuaMan[];
  /** `true` ⇒ bỏ A1: spec của trạng thái này nói tiêu đề là skeleton, nên chưa có `<h1>` (PageHeader.md §Trạng thái `loading`). */
  readonly khongCoH1?: true;
  /** Lỗi axe đã biết ở trạng thái này: tên dòng spec bị vi phạm. Có thì test axe mang `test.fail()`. */
  readonly loiAxeDaBiet?: string;
}

/** Mở một URL với các ca đã khai rồi chờ `<h1>` hiện. */
export function moManVaoH1(url: string, ca: readonly CaGia[]): MoTrangThai {
  return async (page, apiGia) => {
    apiGia.khaiBao(...ca);
    await page.goto(url);
    await expect(page.locator('h1')).toBeVisible();
    await daDungDungTrangThai(page, apiGia, url, ca);
  };
}

/**
 * Trạng thái phải là trạng thái nó mang tên, không phải màn mặc định hay trang khác có <h1>: (1) URL không bị chuyển
 * hướng khỏi đường dẫn của màn; (2) mọi ca lỗi có chủ ý đã khai thật sự được màn gọi tới.
 */
async function daDungDungTrangThai(
  page: Page,
  apiGia: ApiGia,
  url: string,
  ca: readonly CaGia[],
): Promise<void> {
  await page.waitForLoadState('networkidle');
  expect(new URL(page.url()).pathname, 'màn bị chuyển hướng khỏi đường dẫn đã mở').toBe(
    new URL(url, page.url()).pathname,
  );
  for (const c of ca.filter((x) => x.loiCoChuY)) {
    expect(
      apiGia.daPhucVuCa(c),
      `ca lỗi có chủ ý ${c.phuongThuc} ${c.duongDan} chưa được màn gọi — trạng thái lỗi không dựng được`,
    ).toBe(true);
  }
}

/**
 * Trạng thái ĐANG TẢI: ca `caTre` bị giữ cho tới khi trạng thái được dọn (hàm trả về). `chiBao` là bộ chọn của dấu
 * hiệu "đang tải" cần chờ (mặc định vùng `aria-busy`); `coH1` — màn vẽ tiêu đề ngay lúc tải (danh sách, hồ sơ) hay
 * không (trang chi tiết: tiêu đề là skeleton).
 */
export function moManDangTai(
  url: string,
  ca: readonly CaGia[],
  caTre: CaGia,
  tuyChon: { readonly chiBao?: string; readonly coH1?: boolean } = {},
): MoTrangThai {
  const { chiBao = '[aria-busy="true"]', coH1 = true } = tuyChon;
  return async (page, apiGia) => {
    let giaiPhong!: () => void;
    const cho = new Promise<void>((xong) => (giaiPhong = xong));
    apiGia.khaiBao(...ca, { ...caTre, cho });
    await page.goto(url);
    if (coH1) await expect(page.locator('h1')).toBeVisible();
    // `toBeAttached`, không `toBeVisible`: skeleton của tiêu đề `PageHeader` đo được rộng 0px (xem báo cáo T3 lượt b), có mặt mà không hiện.
    await expect(page.locator(chiBao).first()).toBeAttached();
    return giaiPhong;
  };
}

/** Sinh lớp A + axe cho mọi trạng thái của một màn. */
export function phuManTrongKhung(man: string, cacTrangThai: readonly TrangThaiMan[]): void {
  for (const t of cacTrangThai) {
    const phep = [
      ...phepDoManTrongKhung.filter((p) => !(t.khongCoH1 && p.ten.startsWith('A1 '))),
      ...(t.phepDoThem ?? []),
    ];
    lopA(man, t.ten, t.macDinh ? '@mac-dinh-a' : '@khac-a', t.mo, phep);
    axeTrangThai(man, t.ten, t.macDinh ? '@mac-dinh-axe' : '@khac-axe', t.mo, t.loiAxeDaBiet);
  }
}

/** Chờ một phần tử hiện — dùng trong hàm dựng trạng thái. */
export async function choHien(page: Page, boChon: string): Promise<void> {
  await expect(page.locator(boChon).first()).toBeVisible();
}
