import { expect, test } from '../ha-tang/fixture';
import {
  hoSoChuoiDai,
  hoSoDayDu,
  hoSoLoiTai,
  hoSoMangCoDacQuyen,
  hoSoTrongTruong,
} from '../du-lieu-gia/ho-so';
import { kichBanQuanTriDonVi, kichBanVanHanhHeThong } from '../du-lieu-gia/kich-ban';
import { vaoVaKiemMotH1 } from './ca-khoi';
import { a5ONhap, a7TuDien, a10OMatKhauDemPhai, a12FocusOLoiDau } from '../ha-tang/lop-a';
import { moManDangTai, moManVaoH1, phuManTrongKhung } from './ca-trong-khung';
import { lopA } from './ca-xac-thuc';
import type { MoTrangThai } from './ca-xac-thuc';

// Ca khói F2 cho docs/Design/Screens/03-ho-so-ca-nhan.md.
test.describe('hồ sơ cá nhân — ca khói', () => {
  test('hồ sơ đủ trường: vào được, đúng một h1', async ({ page, apiGia }) => {
    apiGia.khaiBao(...kichBanQuanTriDonVi(), hoSoDayDu());
    await vaoVaKiemMotH1(page, '/ho-so');
  });

  test('hồ sơ trống ba trường tuỳ chọn: đúng một h1', async ({ page, apiGia }) => {
    apiGia.khaiBao(...kichBanQuanTriDonVi(), hoSoTrongTruong());
    await vaoVaKiemMotH1(page, '/ho-so');
  });

  test('hồ sơ mang cờ đặc quyền: đúng một h1', async ({ page, apiGia }) => {
    apiGia.khaiBao(...kichBanQuanTriDonVi(), hoSoMangCoDacQuyen());
    await vaoVaKiemMotH1(page, '/ho-so');
  });

  test('hồ sơ chuỗi dài: đúng một h1', async ({ page, apiGia }) => {
    apiGia.khaiBao(...kichBanQuanTriDonVi(), hoSoChuoiDai());
    await vaoVaKiemMotH1(page, '/ho-so');
  });

  test('lỗi tải có chủ ý: đúng một h1', async ({ page, apiGia }) => {
    apiGia.khaiBao(...kichBanQuanTriDonVi(), hoSoLoiTai());
    await vaoVaKiemMotH1(page, '/ho-so');
  });

  test('tài khoản vận hành hệ thống cũng vào được hồ sơ, đúng một h1', async ({ page, apiGia }) => {
    apiGia.khaiBao(...kichBanVanHanhHeThong(), hoSoDayDu());
    await vaoVaKiemMotH1(page, '/ho-so');
  });
});

// --- Lưới giao diện, đợt 2 (T3 lượt b của ADR-0117) ----------------------------------------------------------------
// Trạng thái theo mục *Trạng thái* của 03-ho-so-ca-nhan.md: mặc định (cờ đặc quyền có/không), đang tải, lỗi tải,
// kiểm tra dữ liệu, hộp xác nhận từ bỏ cờ (lớp nổi). *Rỗng*: không áp dụng (spec). CHƯA có dữ liệu giả nên chưa
// bật: lỗi `PUT` (409, lỗi chung), lỗi đổi mật khẩu, lỗi từ bỏ cờ trong hộp — các ca POST/PUT của màn.
const MAN = '03-ho-so-ca-nhan';
const kb = kichBanQuanTriDonVi();
const FORM_THONG_TIN = 'form.form-thong-tin';
const FORM_MAT_KHAU = 'form.form-mat-khau';
const NUT_LUU = `${FORM_THONG_TIN} button[type=submit]`;
const NUT_DOI_MAT_KHAU = `${FORM_MAT_KHAU} button[type=submit]`;

/** Kiểm tra dữ liệu: xoá họ tên (bắt buộc) rồi bấm Lưu — FE chặn, không gọi API. */
const moKiemTraDuLieu: MoTrangThai = async (page, apiGia) => {
  await moManVaoH1('/ho-so', [...kb, hoSoDayDu()])(page, apiGia);
  await expect(page.locator('form.form-thong-tin input[autocomplete="name"]')).toBeVisible();
  await page.locator('form.form-thong-tin input[autocomplete="name"]').fill('');
  // Bàn phím, không chuột: ở `hep` khung `Sidebar` chưa thành drawer nên che một phần form (00-khung-ung-dung.md, bảng *Có thật hôm nay*), chuột không chạm tới nút.
  await page.locator('form.form-thong-tin button[type=submit]').press('Enter');
  await expect(
    page.locator('form.form-thong-tin input[aria-invalid="true"]').first(),
  ).toBeVisible();
};

/** Kiểm tra dữ liệu của form đổi mật khẩu: bấm đổi khi cả ba ô còn trống — FE chặn, không gọi API. */
const moKiemTraDuLieuMatKhau: MoTrangThai = async (page, apiGia) => {
  await moManVaoH1('/ho-so', [...kb, hoSoDayDu()])(page, apiGia);
  await page.locator(NUT_DOI_MAT_KHAU).press('Enter');
  await expect(page.locator(`${FORM_MAT_KHAU} input[aria-invalid="true"]`).first()).toBeVisible();
};

/** Lớp nổi: bấm "Từ bỏ cờ đặc quyền" mở `ConfirmDialog` `danger`. */
const moHopXacNhanTuBoCo: MoTrangThai = async (page, apiGia) => {
  await moManVaoH1('/ho-so', [...kb, hoSoMangCoDacQuyen()])(page, apiGia);
  await page.locator('app-card [slot="footer"] button').press('Enter');
  await expect(page.locator('[role="alertdialog"], [role="dialog"]').first()).toBeVisible();
};

phuManTrongKhung(MAN, [
  {
    ten: 'mặc định',
    macDinh: true,
    mo: moManVaoH1('/ho-so', [...kb, hoSoDayDu()]),
    phepDoThem: [
      {
        ten: 'A10 ô mật khẩu chừa chỗ nút hiện/ẩn',
        chay: (page) => a10OMatKhauDemPhai.kiem(page, FORM_MAT_KHAU),
      },
    ],
  },
  { ten: 'mặc định, mang cờ đặc quyền', mo: moManVaoH1('/ho-so', [...kb, hoSoMangCoDacQuyen()]) },
  { ten: 'ba trường tuỳ chọn trống', mo: moManVaoH1('/ho-so', [...kb, hoSoTrongTruong()]) },
  { ten: 'chuỗi dài', mo: moManVaoH1('/ho-so', [...kb, hoSoChuoiDai()]) },
  { ten: 'đang tải', mo: moManDangTai('/ho-so', kb, hoSoDayDu()) },
  { ten: 'lỗi tải hồ sơ', mo: moManVaoH1('/ho-so', [...kb, hoSoLoiTai()]) },
  {
    ten: 'kiểm tra dữ liệu (form thông tin)',
    mo: moKiemTraDuLieu,
    phepDoThem: [
      {
        ten: 'A12 focus về ô lỗi đầu tiên',
        chay: (page) => a12FocusOLoiDau.kiem(page, FORM_THONG_TIN),
      },
    ],
  },
  {
    ten: 'kiểm tra dữ liệu (form đổi mật khẩu)',
    mo: moKiemTraDuLieuMatKhau,
    phepDoThem: [
      {
        ten: 'A12 focus về ô lỗi đầu tiên',
        chay: (page) => a12FocusOLoiDau.kiem(page, FORM_MAT_KHAU),
      },
    ],
  },
  { ten: 'hộp xác nhận từ bỏ cờ (lớp nổi)', mo: moHopXacNhanTuBoCo },
]);

// --- Tương tác của `Input` — một lần, trên màn đầu tiên dùng nó (QC3 hàng 3): `password` (ba ô đổi mật khẩu) và `text` (họ tên)
// Nguồn: DESIGN.md §2.6 *Ô nhập chữ* cho cả `Input` (DESIGN.md:24 ghi "đã đổi trong mã, chưa đo" — đây là lần đo đầu).
lopA(
  MAN,
  'tương tác ô nhập (Input password)',
  '@tuong-tac',
  moManVaoH1('/ho-so', [...kb, hoSoDayDu()]),
  [
    {
      ten: 'A5 một đường biên hai trục',
      chay: (page) => a5ONhap.kiem(page, page.locator(NUT_DOI_MAT_KHAU), FORM_MAT_KHAU),
    },
    { ten: 'A7 ô tự điền giữ nền', chay: (page) => a7TuDien.kiem(page, FORM_MAT_KHAU) },
  ],
);
lopA(
  MAN,
  'tương tác ô nhập (Input text)',
  '@tuong-tac',
  moManVaoH1('/ho-so', [...kb, hoSoDayDu()]),
  [
    {
      ten: 'A5 một đường biên hai trục',
      chay: (page) =>
        a5ONhap.kiem(
          page,
          page.locator(NUT_LUU),
          FORM_THONG_TIN,
          `${FORM_THONG_TIN} input[autocomplete="name"]`,
        ),
    },
  ],
);
