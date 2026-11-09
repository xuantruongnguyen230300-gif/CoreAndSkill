import { readFileSync } from 'node:fs';

import { expect, test } from '../ha-tang/fixture';
import { kichBanQuanTriDonVi } from '../du-lieu-gia/kich-ban';
import {
  danhSachVaiTroCoDuLieu,
  danhSachVaiTroLoiTai,
  danhSachVaiTroRong,
  danhSachVaiTroTimKhongRa,
} from '../du-lieu-gia/vai-tro';
import { vaoVaKiemMotH1 } from './ca-khoi';
import { moMenuVaKiemConMo } from './ca-lop-noi-neo';
import { moManDangTai, moManVaoH1, phuManTrongKhung } from './ca-trong-khung';
import {
  kiemChiThanBangCuon,
  kiemCotHanhDong,
  kiemKhoiLien,
  kiemSanVungCuon,
  kiemVungBaoKetQuaKhiKhongCo,
} from './ca-khoi-danh-sach';

// Ca khói F2 cho docs/Design/Screens/11-vai-tro.md.
test.describe('vai trò — ca khói', () => {
  test('có dữ liệu: vào được, đúng một h1', async ({ page, apiGia }) => {
    apiGia.khaiBao(...kichBanQuanTriDonVi(), danhSachVaiTroCoDuLieu());
    await vaoVaKiemMotH1(page, '/quan-tri/vai-tro');
  });

  test('rỗng: đúng một h1', async ({ page, apiGia }) => {
    apiGia.khaiBao(...kichBanQuanTriDonVi(), danhSachVaiTroRong());
    await vaoVaKiemMotH1(page, '/quan-tri/vai-tro');
  });

  test('tìm không ra: URL có searchText, đúng một h1', async ({ page, apiGia }) => {
    apiGia.khaiBao(...kichBanQuanTriDonVi(), danhSachVaiTroTimKhongRa());
    await vaoVaKiemMotH1(page, '/quan-tri/vai-tro?searchText=khong-co-vai-tro-nhu-vay');
  });

  test('lỗi tải có chủ ý: đúng một h1', async ({ page, apiGia }) => {
    apiGia.khaiBao(...kichBanQuanTriDonVi(), danhSachVaiTroLoiTai());
    await vaoVaKiemMotH1(page, '/quan-tri/vai-tro');
  });
});

// --- Lưới giao diện, đợt 2 (T3 lượt b của ADR-0117) ----------------------------------------------------------------
// Trạng thái theo mục *Trạng thái* của 11-vai-tro.md. CHƯA bật — không có dữ liệu giả cho ca POST/PUT: hộp tạo/đổi
// tên, *kiểm tra dữ liệu* trong hộp, xung đột đồng thời 409 và nhánh "Tải lại".
const kb = kichBanQuanTriDonVi();
const DS = '/quan-tri/vai-tro';

phuManTrongKhung('11-vai-tro', [
  {
    ten: 'mặc định (có dữ liệu)',
    macDinh: true,
    mo: moManVaoH1(DS, [...kb, danhSachVaiTroCoDuLieu()]),
  },
  { ten: 'đang tải', mo: moManDangTai(DS, kb, danhSachVaiTroCoDuLieu()) },
  { ten: 'rỗng (chưa có vai trò)', mo: moManVaoH1(DS, [...kb, danhSachVaiTroRong()]) },
  {
    ten: 'rỗng (tìm không ra)',
    mo: moManVaoH1(`${DS}?searchText=khong-co-vai-tro-nhu-vay`, [
      ...kb,
      danhSachVaiTroTimKhongRa(),
    ]),
  },
  { ten: 'lỗi tải', mo: moManVaoH1(DS, [...kb, danhSachVaiTroLoiTai()]) },
]);

// Khối danh sách liền (ListScreen.md §2–§4, DataTable.md §"Cột hành động") — đo trên trình duyệt, project `rong-sang`.
test.describe('vai trò — danh sách — khối liền và cột hành động', () => {
  test.beforeEach(({ apiGia }) => apiGia.khaiBao(...kb, danhSachVaiTroCoDuLieu()));

  test('Toolbar attached trong khung, PageHeader không nút, ô tìm có trần', async ({ page }) => {
    await kiemKhoiLien(page, DS);
  });

  test('chỉ thân bảng cuộn', async ({ page }) => {
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

// Vùng báo kết quả của DataTable (DataTable.md §"Vùng báo kết quả"): bộ lọc ra 0 bản ghi → vùng mang câu "không có kết quả".
test.describe('11-vai-tro — danh sách — vùng báo kết quả', () => {
  test('lọc ra 0 bản ghi: vùng role=status có câu "không có kết quả", không còn aria-live nào khác', async ({
    page,
    apiGia,
  }) => {
    apiGia.khaiBao(...kichBanQuanTriDonVi(), danhSachVaiTroTimKhongRa());
    await kiemVungBaoKetQuaKhiKhongCo(
      page,
      '/quan-tri/vai-tro?searchText=khong-co-vai-tro-nhu-vay',
    );
  });
});

// Lớp nổi neo dưới giảm chuyển động (project `rong-sang` đặt `reducedMotion: 'reduce'`): menu hàng còn mở sau khi hiện.
// Hồi quy: menu để tại chỗ trong khung cuộn của bảng bị cú cuộn của chính nó đóng sau ~15ms.
const vi = JSON.parse(readFileSync('public/i18n/vi.json', 'utf-8')) as {
  vaiTro: { menu: { doiTen: string } };
};

test.describe('vai trò — danh sách — lớp nổi neo khi giảm chuyển động', () => {
  test('menu hàng: mở xong còn mở, role=menu, chọn được mục', async ({ page, apiGia }) => {
    apiGia.khaiBao(...kb, danhSachVaiTroCoDuLieu());
    await page.goto(DS);
    // Hàng thứ hai là vai trò thường (hàng đầu là vai trò hệ thống, mục "Đổi tên" bị khoá).
    const hang = page.locator('tbody tr').nth(1);
    await hang.hover();
    const menu = await moMenuVaKiemConMo(page, hang.locator('button[aria-haspopup="menu"]'));
    await menu.getByRole('menuitem', { name: vi.vaiTro.menu.doiTen }).click();
    await expect(page.getByRole('dialog')).toBeVisible();
    await expect(page.getByRole('menu')).toHaveCount(0);
  });
});
