import { readFileSync } from 'node:fs';

import type { Page } from '@playwright/test';

import { expect, test } from '../ha-tang/fixture';
import type { MoTrangThai } from './ca-xac-thuc';
import { kichBanQuanTriDonVi } from '../du-lieu-gia/kich-ban';
import {
  chiTietNguoiDungBiKhoa,
  chiTietNguoiDungKhongTimThay,
  chiTietNguoiDungLoiTai,
  chiTietNguoiDungMau,
  chiTietNguoiDungTenDai,
  danhSachNguoiDungCoDuLieu,
  danhSachNguoiDungLoiTai,
  danhSachNguoiDungRong,
  danhSachNguoiDungTimKhongRa,
  ID_NGUOI_DUNG_BI_KHOA,
  ID_NGUOI_DUNG_MAU,
  ID_NGUOI_DUNG_TEN_DAI,
} from '../du-lieu-gia/nguoi-dung';
import { danhSachVaiTroCoDuLieu } from '../du-lieu-gia/vai-tro';
import { vaoVaKiemMotH1 } from './ca-khoi';
import { choLopNoiDungYen, moMenuVaKiemConMo } from './ca-lop-noi-neo';
import { moManDangTai, moManVaoH1, phuManTrongKhung } from './ca-trong-khung';
import {
  kiemChiThanBangCuon,
  kiemCotHanhDong,
  kiemKhoiLien,
  kiemSanVungCuon,
  kiemVungBaoKetQuaKhiKhongCo,
} from './ca-khoi-danh-sach';

// Ca khói F2 cho docs/Design/Screens/10-nguoi-dung.md (danh sách và chi tiết).
test.describe('người dùng — danh sách — ca khói', () => {
  test('có dữ liệu: vào được, đúng một h1', async ({ page, apiGia }) => {
    apiGia.khaiBao(...kichBanQuanTriDonVi(), danhSachNguoiDungCoDuLieu());
    await vaoVaKiemMotH1(page, '/quan-tri/nguoi-dung');
  });

  test('rỗng: đúng một h1', async ({ page, apiGia }) => {
    apiGia.khaiBao(...kichBanQuanTriDonVi(), danhSachNguoiDungRong());
    await vaoVaKiemMotH1(page, '/quan-tri/nguoi-dung');
  });

  test('tìm không ra: URL có searchText, đúng một h1', async ({ page, apiGia }) => {
    apiGia.khaiBao(...kichBanQuanTriDonVi(), danhSachNguoiDungTimKhongRa());
    await vaoVaKiemMotH1(page, '/quan-tri/nguoi-dung?searchText=khong-co-ai-ten-nhu-vay');
  });

  test('lỗi tải có chủ ý: đúng một h1', async ({ page, apiGia }) => {
    apiGia.khaiBao(...kichBanQuanTriDonVi(), danhSachNguoiDungLoiTai());
    await vaoVaKiemMotH1(page, '/quan-tri/nguoi-dung');
  });
});

test.describe('người dùng — chi tiết — ca khói', () => {
  test('người dùng bình thường: đúng một h1', async ({ page, apiGia }) => {
    apiGia.khaiBao(...kichBanQuanTriDonVi(), chiTietNguoiDungMau());
    await vaoVaKiemMotH1(page, `/quan-tri/nguoi-dung/${ID_NGUOI_DUNG_MAU}`);
  });

  test('người dùng bị khoá: đúng một h1', async ({ page, apiGia }) => {
    apiGia.khaiBao(...kichBanQuanTriDonVi(), chiTietNguoiDungBiKhoa());
    await vaoVaKiemMotH1(page, `/quan-tri/nguoi-dung/${ID_NGUOI_DUNG_BI_KHOA}`);
  });

  test('chuỗi dài: đúng một h1', async ({ page, apiGia }) => {
    apiGia.khaiBao(...kichBanQuanTriDonVi(), chiTietNguoiDungTenDai());
    await vaoVaKiemMotH1(page, `/quan-tri/nguoi-dung/${ID_NGUOI_DUNG_TEN_DAI}`);
  });

  test('không tồn tại (404): đúng một h1', async ({ page, apiGia }) => {
    const id = '0192f3c1-0000-7000-8000-00000000dead';
    apiGia.khaiBao(...kichBanQuanTriDonVi(), chiTietNguoiDungKhongTimThay(id));
    await vaoVaKiemMotH1(page, `/quan-tri/nguoi-dung/${id}`);
  });

  test('lỗi tải có chủ ý: đúng một h1', async ({ page, apiGia }) => {
    apiGia.khaiBao(...kichBanQuanTriDonVi(), chiTietNguoiDungLoiTai(ID_NGUOI_DUNG_MAU));
    await vaoVaKiemMotH1(page, `/quan-tri/nguoi-dung/${ID_NGUOI_DUNG_MAU}`);
  });
});

// --- Lưới giao diện, đợt 2 (T3 lượt b của ADR-0117) ----------------------------------------------------------------
// Trạng thái theo mục *Trạng thái* của 10-nguoi-dung.md, cả hai màn (danh sách · chi tiết). CHƯA bật — không có dữ
// liệu giả cho các ca POST/PUT của màn: ba hộp thoại (tạo, đổi vai trò, khoá/đặt lại mật khẩu) và trạng thái
// *kiểm tra dữ liệu* trong hộp; lỗi `ROLE_NOT_FOUND` của danh sách (cần thêm `GET /core/roles/{id}` cho chip lọc).
const kb = kichBanQuanTriDonVi();
const DS = '/quan-tri/nguoi-dung';
const ID_KHONG_TON_TAI = '0192f3c1-0000-7000-8000-00000000dead';
const ID_BANG_LOC = 'nguoi-dung-bang-loc';
const BANG_LOC = `#${ID_BANG_LOC}`;

/** Mở màn rồi bấm nút lọc: trạng thái "bảng lọc mở" của FilterPanel.md §"Trong Drawer". */
function moBangLocMo(): MoTrangThai {
  const moMan = moManVaoH1(DS, [...kb, danhSachNguoiDungCoDuLieu()]);
  return async (page, apiGia) => {
    const don = await moMan(page, apiGia);
    await page.locator('.toolbar__nut-loc button').click();
    await expect(page.locator(BANG_LOC)).toBeVisible();
    return don;
  };
}

phuManTrongKhung('10-nguoi-dung', [
  {
    ten: 'danh sách · mặc định (có dữ liệu)',
    macDinh: true,
    mo: moManVaoH1(DS, [...kb, danhSachNguoiDungCoDuLieu()]),
  },
  {
    ten: 'danh sách · bảng lọc mở (Drawer edit, FilterPanel)',
    mo: moBangLocMo(),
  },
  { ten: 'danh sách · đang tải', mo: moManDangTai(DS, kb, danhSachNguoiDungCoDuLieu()) },
  {
    ten: 'danh sách · rỗng (chưa có người dùng)',
    mo: moManVaoH1(DS, [...kb, danhSachNguoiDungRong()]),
  },
  {
    ten: 'danh sách · rỗng (tìm không ra)',
    mo: moManVaoH1(`${DS}?searchText=khong-co-ai-ten-nhu-vay`, [
      ...kb,
      danhSachNguoiDungTimKhongRa(),
    ]),
  },
  { ten: 'danh sách · lỗi tải', mo: moManVaoH1(DS, [...kb, danhSachNguoiDungLoiTai()]) },
  {
    ten: 'chi tiết · mặc định',
    macDinh: true,
    mo: moManVaoH1(`${DS}/${ID_NGUOI_DUNG_MAU}`, [...kb, chiTietNguoiDungMau()]),
  },
  {
    ten: 'chi tiết · bị khoá',
    mo: moManVaoH1(`${DS}/${ID_NGUOI_DUNG_BI_KHOA}`, [...kb, chiTietNguoiDungBiKhoa()]),
  },
  {
    ten: 'chi tiết · chuỗi dài',
    mo: moManVaoH1(`${DS}/${ID_NGUOI_DUNG_TEN_DAI}`, [...kb, chiTietNguoiDungTenDai()]),
  },
  {
    ten: 'chi tiết · đang tải',
    khongCoH1: true,
    mo: moManDangTai(`${DS}/${ID_NGUOI_DUNG_MAU}`, kb, chiTietNguoiDungMau(), {
      coH1: false,
      chiBao: 'app-page-header .skeleton-loader',
    }),
  },
  {
    ten: 'chi tiết · không tồn tại (record-not-found)',
    mo: moManVaoH1(`${DS}/${ID_KHONG_TON_TAI}`, [
      ...kb,
      chiTietNguoiDungKhongTimThay(ID_KHONG_TON_TAI),
    ]),
  },
  {
    ten: 'chi tiết · lỗi tải',
    mo: moManVaoH1(`${DS}/${ID_NGUOI_DUNG_MAU}`, [
      ...kb,
      chiTietNguoiDungLoiTai(ID_NGUOI_DUNG_MAU),
    ]),
  },
]);

// Khối danh sách liền (ListScreen.md §2–§4, DataTable.md §"Cột hành động") — đo trên trình duyệt, project `rong-sang`.
test.describe('người dùng — danh sách — khối liền và cột hành động', () => {
  test.beforeEach(({ apiGia }) => apiGia.khaiBao(...kb, danhSachNguoiDungCoDuLieu()));

  test('Toolbar attached trong khung, PageHeader không nút, ô tìm có trần', async ({ page }) => {
    await kiemKhoiLien(page, DS);
  });

  test('chỉ thân bảng cuộn; th dính đỉnh', async ({ page }) => {
    await kiemChiThanBangCuon(page, DS);
  });

  test('màn thấp: main cuộn, vùng cuộn giữ sàn', async ({ page }) => {
    await kiemSanVungCuon(page, DS);
  });

  test('cột hành động: ghim phải, ẩn bằng opacity, hiện khi rê chuột / focus, trong thứ tự Tab', async ({
    page,
  }) => {
    await kiemCotHanhDong(page, DS);
  });
});

// Bảng lọc (FilterPanel.md §"Trong Drawer", Drawer.md §Accessibility, Toolbar.md §"Nút lọc") — đo trên trình duyệt thật,
// project `rong-sang`. Không phép đo nào khẳng định theo chuỗi tiếng Việt (hướng khoá K21); bộ chọn bám class/id.
test.describe('người dùng — danh sách — bảng lọc', () => {
  test.beforeEach(({ apiGia }) => apiGia.khaiBao(...kb, danhSachNguoiDungCoDuLieu()));

  const nutLoc = (page: Page) => page.locator('.toolbar__nut-loc button');
  const truongDau = (page: Page) => page.locator(`#${ID_BANG_LOC}-truong-roleId`);
  const truongTrangThai = (page: Page) => page.locator(`#${ID_BANG_LOC}-truong-status`);
  const nutAp = (page: Page) => page.locator(`button[type=submit][form="${ID_BANG_LOC}-form"]`);

  async function vaoVaMo(page: Page): Promise<void> {
    await page.goto(DS);
    await expect(page.locator('h1')).toBeVisible();
    await nutLoc(page).click();
    await expect(page.locator(BANG_LOC)).toBeVisible();
  }

  test('nút lọc mở hộp thoại modal; focus vào trường đầu tiên; nút mang aria-expanded/aria-controls', async ({
    page,
  }) => {
    await page.goto(DS);
    await expect(nutLoc(page)).toHaveAttribute('aria-haspopup', 'dialog');
    await expect(nutLoc(page)).toHaveAttribute('aria-expanded', 'false');
    await expect(nutLoc(page)).toHaveAttribute('aria-controls', ID_BANG_LOC);
    await nutLoc(page).click();
    const bang = page.locator(BANG_LOC);
    await expect(bang).toBeVisible();
    await expect(bang).toHaveAttribute('role', 'dialog');
    await expect(bang).toHaveAttribute('aria-modal', 'true');
    await expect(nutLoc(page)).toHaveAttribute('aria-expanded', 'true');
    await expect(truongDau(page)).toBeFocused();
    // Mỗi trường có <label> thật nhìn thấy được.
    await expect(bang.locator('label')).toHaveCount(2);
    await expect(bang.locator('label').first()).toBeVisible();
  });

  test('bẫy focus: Tab và Shift+Tab không bao giờ ra khỏi hộp thoại', async ({ page }) => {
    await vaoVaMo(page);
    const trongBang = () =>
      page.evaluate(
        (id) => document.getElementById(id)!.contains(document.activeElement),
        ID_BANG_LOC,
      );
    for (let i = 0; i < 12; i++) {
      await page.keyboard.press('Tab');
      expect(await trongBang(), `Tab lần ${i + 1} ra khỏi hộp thoại`).toBe(true);
    }
    for (let i = 0; i < 12; i++) {
      await page.keyboard.press('Shift+Tab');
      expect(await trongBang(), `Shift+Tab lần ${i + 1} ra khỏi hộp thoại`).toBe(true);
    }
  });

  test('Escape đóng hộp thoại và trả focus về nút lọc', async ({ page }) => {
    await vaoVaMo(page);
    await page.keyboard.press('Escape');
    await expect(page.locator(BANG_LOC)).toHaveCount(0);
    await expect(nutLoc(page)).toBeFocused();
    await expect(nutLoc(page)).toHaveAttribute('aria-expanded', 'false');
  });

  test('áp lọc: hộp thoại đóng, focus về nút lọc, URL và chip cập nhật, nút lọc mang số điều kiện', async ({
    page,
  }) => {
    await vaoVaMo(page);
    await expect(nutAp(page)).toBeDisabled();
    await truongTrangThai(page).selectOption('locked');
    await expect(nutAp(page)).toBeEnabled();
    await nutAp(page).click();

    await expect(page.locator(BANG_LOC)).toHaveCount(0);
    await expect(nutLoc(page)).toBeFocused();
    await expect(page).toHaveURL(/[?&]status=locked(&|$)/);
    // Chip đơn vị (chỉ đọc) + chip điều kiện vừa áp.
    await expect(page.locator('.toolbar__chips app-filter-chip')).toHaveCount(2);
    await expect(page.locator('.toolbar__nut-loc app-badge')).toHaveCount(1);
  });

  test('đóng mà chưa áp: bỏ phần chưa áp, không đổi URL', async ({ page }) => {
    await vaoVaMo(page);
    await truongTrangThai(page).selectOption('locked');
    await page.keyboard.press('Escape');
    await expect(page.locator(BANG_LOC)).toHaveCount(0);
    expect(new URL(page.url()).searchParams.has('status')).toBe(false);
    await nutLoc(page).click();
    await expect(truongTrangThai(page)).toHaveValue('');
  });

  test('Xoá hết: gỡ điều kiện ngay, hộp thoại giữ mở, focus về trường đầu', async ({ page }) => {
    await page.goto(`${DS}?status=active`);
    await expect(page.locator('h1')).toBeVisible();
    await nutLoc(page).click();
    await expect(truongTrangThai(page)).toHaveValue('active');
    await page.locator(`${BANG_LOC} .p-drawer-footer app-button button`).first().click();

    await expect(page.locator(BANG_LOC)).toBeVisible();
    await expect(truongDau(page)).toBeFocused();
    await expect(truongTrangThai(page)).toHaveValue('');
    expect(new URL(page.url()).searchParams.has('status')).toBe(false);
  });
});

// Vùng báo kết quả của DataTable (DataTable.md §"Vùng báo kết quả"): bộ lọc ra 0 bản ghi → vùng mang câu "không có kết quả".
test.describe('10-nguoi-dung — danh sách — vùng báo kết quả', () => {
  test('lọc ra 0 bản ghi: vùng role=status có câu "không có kết quả", không còn aria-live nào khác', async ({
    page,
    apiGia,
  }) => {
    apiGia.khaiBao(...kichBanQuanTriDonVi(), danhSachNguoiDungTimKhongRa());
    await kiemVungBaoKetQuaKhiKhongCo(
      page,
      '/quan-tri/nguoi-dung?searchText=khong-co-ai-ten-nhu-vay',
    );
  });
});

// Ô đầu của hộp tạo tự nhận focus lúc hộp hiện (Dialog.md §Accessibility; fe-ui-conventions.md §6.5):
// Tab rời ô khi chưa gõ thì không báo lỗi; gõ rồi xoá hết rồi rời ô thì báo.
test.describe('10-nguoi-dung — hộp tạo — ô đầu tự nhận focus', () => {
  const oTen = (page: Page) => page.locator('input#nd-tao-ten');

  async function moHopTao(page: Page): Promise<void> {
    await page.goto(DS);
    await expect(page.locator('h1')).toBeVisible();
    await page.getByRole('button', { name: 'Thêm người dùng' }).click();
    await expect(oTen(page)).toBeFocused();
  }

  test('Tab rời ô đầu khi chưa gõ: không dòng lỗi, aria-invalid không bật', async ({
    page,
    apiGia,
  }) => {
    apiGia.khaiBao(...kb, danhSachNguoiDungCoDuLieu());
    await moHopTao(page);
    await page.keyboard.press('Tab');
    await expect(oTen(page)).not.toBeFocused();
    await expect(oTen(page)).not.toHaveAttribute('aria-invalid', 'true');
    await expect(page.locator('#nd-tao-ten-error')).toHaveCount(0);
  });

  test('gõ rồi xoá hết rồi Tab rời ô: báo lỗi', async ({ page, apiGia }) => {
    apiGia.khaiBao(...kb, danhSachNguoiDungCoDuLieu());
    await moHopTao(page);
    await page.keyboard.type('a');
    await page.keyboard.press('Backspace');
    await page.keyboard.press('Tab');
    await expect(oTen(page)).toHaveAttribute('aria-invalid', 'true');
  });
});

// Lớp nổi neo dưới giảm chuyển động (project `rong-sang` đặt `reducedMotion: 'reduce'`): menu hàng và bảng gợi ý
// của hộp "Gán vai trò" phải còn mở sau khi hiện. Hồi quy: lớp nổi để tại chỗ trong khung cuộn bị cú cuộn của chính nó
// đóng sau ~15ms; sửa ở lớp bọc `shared/ui/menu` và `shared/ui/autocomplete` (gắn lớp nổi vào body).
const vi = JSON.parse(readFileSync('public/i18n/vi.json', 'utf-8')) as {
  nguoiDung: { hanhDong: { ganVaiTro: string } };
};

test.describe('người dùng — danh sách — lớp nổi neo khi giảm chuyển động', () => {
  test.beforeEach(({ apiGia }) =>
    apiGia.khaiBao(...kb, danhSachNguoiDungCoDuLieu(), danhSachVaiTroCoDuLieu()),
  );

  async function nutMenuDongDau(page: Page) {
    await page.goto(DS);
    const hang = page.locator('tbody tr').first();
    await hang.hover();
    return hang.locator('button[aria-haspopup="menu"]');
  }

  test('menu hàng: mở xong còn mở, role=menu, chọn được mục', async ({ page }) => {
    const menu = await moMenuVaKiemConMo(page, await nutMenuDongDau(page));
    await menu.getByRole('menuitem', { name: vi.nguoiDung.hanhDong.ganVaiTro }).click();
    await expect(page.getByRole('dialog')).toBeVisible();
    await expect(page.getByRole('menu')).toHaveCount(0);
  });

  // Ô số dòng của Paginator cũng là lớp nổi neo (`p-select` thư viện tự dựng). Ở chiều cao 600 lớp nổi để tại chỗ làm
  // `main` cuộn rồi tự đóng; các chiều cao khác là đối chứng. Bốn chiều cao quanh chỗ lớp nổi vừa chạm đáy.
  for (const cao of [420, 600, 700, 800]) {
    test(`ô số dòng của Paginator: khung nhìn cao ${cao}px, mở xong còn mở, chọn được mục`, async ({
      page,
    }) => {
      await page.setViewportSize({ width: 1280, height: cao });
      await page.goto(DS);
      await expect(page.locator('h1')).toBeVisible();
      const o = page.locator('app-pagination .p-select').first();
      await o.scrollIntoViewIfNeeded();
      await o.click();
      const bang = page.locator('.p-select-overlay');
      await expect(bang).toBeVisible();
      await choLopNoiDungYen(page);
      await expect(bang).toBeVisible();
      await bang.getByRole('option').nth(1).click();
      await expect(bang).toHaveCount(0);
    });
  }

  test('bảng gợi ý trong hộp "Gán vai trò": hiện, còn mở, chọn được gợi ý', async ({ page }) => {
    const menu = await moMenuVaKiemConMo(page, await nutMenuDongDau(page));
    await menu.getByRole('menuitem', { name: vi.nguoiDung.hanhDong.ganVaiTro }).click();
    const o = page.locator('#ndgan-vai-tro');
    await expect(o).toBeVisible();
    await o.click();
    await o.pressSequentially('Nh');
    const danhSach = page.locator('ul.p-autocomplete-list[role="listbox"]');
    const chip = page.locator('.p-autocomplete-chip');
    const soChipTruoc = await chip.count();
    await expect(danhSach).toBeVisible();
    await choLopNoiDungYen(page);
    await expect(danhSach).toBeVisible();
    await expect(o).toHaveAttribute('aria-expanded', 'true');
    await danhSach.getByRole('option', { name: 'Nhân viên' }).click();
    await expect(chip).toHaveCount(soChipTruoc + 1);
  });
});
