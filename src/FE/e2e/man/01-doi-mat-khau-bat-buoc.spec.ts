import { expect, test } from '../ha-tang/fixture';
import {
  kichBanQuanTriBatBuocDoiMatKhau,
  kichBanVanHanhBatBuocDoiMatKhau,
} from '../du-lieu-gia/kich-ban';
import { thatBai } from '../ha-tang/envelope';
import {
  a1MotH1,
  a2ChuHienThi,
  a3KhongCuon,
  a4HoFont,
  a6KheGiuaVung,
  a6KheTieuDeMoTa,
  a6KheTrongForm,
  a9AuthCardResponsive,
  a10OMatKhauDemPhai,
  a12FocusOLoiDau,
} from '../ha-tang/lop-a';
import { axeTrangThai, lopA, nutGui } from './ca-xac-thuc';
import type { MoTrangThai, PhepDoCuaMan } from './ca-xac-thuc';
import { vaoVaKiemMotH1 } from './ca-khoi';

const MAN = '01-doi-mat-khau-bat-buoc';
const DUONG_DAN = '/doi-mat-khau-bat-buoc';
const O_HIEN_TAI = '#doi-mat-khau-bat-buoc-hien-tai';
const O_MOI = '#doi-mat-khau-bat-buoc-moi';
const O_NHAP_LAI = '#doi-mat-khau-bat-buoc-nhap-lai';

// Ca khói F2 cho màn thứ hai của docs/Design/Screens/01-dang-nhap.md (`/doi-mat-khau-bat-buoc`).
test.describe('đổi mật khẩu bắt buộc — ca khói', () => {
  test('quản trị đơn vị mang mật khẩu tạm: vào được, đúng một h1', async ({ page, apiGia }) => {
    apiGia.khaiBao(...kichBanQuanTriBatBuocDoiMatKhau());
    await vaoVaKiemMotH1(page, DUONG_DAN);
  });

  test('tài khoản vận hành mang mật khẩu tạm: vào được, đúng một h1', async ({ page, apiGia }) => {
    apiGia.khaiBao(...kichBanVanHanhBatBuocDoiMatKhau());
    await vaoVaKiemMotH1(page, DUONG_DAN);
  });

  test('mustChangePassword: mở màn khác bị đưa về màn đổi mật khẩu, vẫn đúng một h1', async ({
    page,
    apiGia,
  }) => {
    apiGia.khaiBao(...kichBanQuanTriBatBuocDoiMatKhau());
    await vaoVaKiemMotH1(page, '/trang-chu');
    await page.waitForURL('**/doi-mat-khau-bat-buoc');
  });
});

// --- Dựng trạng thái ---------------------------------------------------------------------------------------

const moMacDinh: MoTrangThai = async (page, apiGia) => {
  apiGia.khaiBao(...kichBanQuanTriBatBuocDoiMatKhau());
  await page.goto(DUONG_DAN);
  await expect(page.locator('h1')).toBeVisible();
};

/** Kiểm tra dữ liệu: bấm gửi khi ba ô còn trống — FE chặn, không gọi API. */
const moKiemTraDuLieu: MoTrangThai = async (page, apiGia) => {
  await moMacDinh(page, apiGia);
  await nutGui(page).click();
  await expect(page.locator('main form input[aria-invalid="true"]').first()).toBeVisible();
};

const loiChinhSach = () =>
  ({
    phuongThuc: 'POST',
    duongDan: '/core/auth/change-password-required',
    phanHoi: thatBai('CORE.AUTH.CHANGE_PASSWORD_FAILED', {
      type: 'BusinessRule',
      status: 422,
      fieldErrors: {
        CurrentPassword: [{ code: 'CORE.AUTH.PASSWORD_MISMATCH', messageParams: null }],
        NewPassword: [{ code: 'CORE.AUTH.NEW_PASSWORD_SAME_AS_CURRENT', messageParams: null }],
      },
    }),
    loiCoChuY: true,
  }) as const;

async function dienHopLe(page: import('@playwright/test').Page): Promise<void> {
  await page.locator(O_HIEN_TAI).fill('mat-khau-tam');
  await page.locator(O_MOI).fill('mat-khau-moi');
  await page.locator(O_NHAP_LAI).fill('mat-khau-moi');
}

/** Lỗi: máy chủ từ chối — lỗi theo ô ở hai ô (mật khẩu hiện tại sai; mật khẩu mới trùng mật khẩu hiện tại). */
const moLoi: MoTrangThai = async (page, apiGia) => {
  await moMacDinh(page, apiGia);
  apiGia.khaiBao(loiChinhSach());
  await dienHopLe(page);
  await nutGui(page).click();
  await expect(page.locator(`${O_MOI}[aria-invalid="true"]`)).toBeVisible();
  await expect(page.locator(`${O_HIEN_TAI}[aria-invalid="true"]`)).toBeVisible();
};

/** Đang tải: request đổi mật khẩu treo. */
const moDangTai: MoTrangThai = async (page, apiGia) => {
  await moMacDinh(page, apiGia);
  let giaiPhong!: () => void;
  const cho = new Promise<void>((xong) => (giaiPhong = xong));
  apiGia.khaiBao({ ...loiChinhSach(), cho });
  await dienHopLe(page);
  await nutGui(page).click();
  await expect(page.locator('main form[aria-busy="true"]')).toBeVisible();
  return giaiPhong;
};

// --- Phép đo ------------------------------------------------------------------------------------------------

const phepDo: PhepDoCuaMan[] = [
  { ten: 'A1 một h1 trong main', chay: (page) => a1MotH1.kiem(page, { trongMain: true }) },
  { ten: 'A2 không null/NaN/khoá dịch', chay: (page) => a2ChuHienThi.kiem(page) },
  { ten: 'A3 không cuộn khi đủ chỗ', chay: (page) => a3KhongCuon.kiem(page) },
  { ten: 'A4 họ font --font-sans', chay: (page) => a4HoFont.kiem(page) },
  { ten: 'A6a khe trong form', chay: (page) => a6KheTrongForm.kiem(page) },
  { ten: 'A6b khe giữa các vùng', chay: (page) => a6KheGiuaVung.kiem(page) },
  { ten: 'A6c khe tiêu đề → mô tả', chay: (page) => a6KheTieuDeMoTa.kiem(page) },
];

lopA(MAN, 'mặc định', '@mac-dinh-a', moMacDinh, [
  ...phepDo,
  { ten: 'A10 ô mật khẩu chừa chỗ nút hiện/ẩn', chay: (page) => a10OMatKhauDemPhai.kiem(page) },
]);
lopA(MAN, 'khung theo bề rộng', '@mot-khung', moMacDinh, [
  { ten: 'A9 AuthCard responsive', chay: (page) => a9AuthCardResponsive.kiem(page) },
]);
axeTrangThai(MAN, 'mặc định', '@mac-dinh-axe', moMacDinh);

lopA(MAN, 'kiểm tra dữ liệu', '@khac-a', moKiemTraDuLieu, [
  ...phepDo,
  { ten: 'A12 focus về ô lỗi đầu tiên', chay: (page) => a12FocusOLoiDau.kiem(page) },
]);
axeTrangThai(MAN, 'kiểm tra dữ liệu', '@khac-axe', moKiemTraDuLieu);

lopA(MAN, 'lỗi đổi mật khẩu', '@khac-a', moLoi, phepDo);
axeTrangThai(MAN, 'lỗi đổi mật khẩu', '@khac-axe', moLoi);

lopA(MAN, 'đang tải (đang gửi)', '@khac-a', moDangTai, phepDo);
axeTrangThai(MAN, 'đang tải (đang gửi)', '@khac-axe', moDangTai);
