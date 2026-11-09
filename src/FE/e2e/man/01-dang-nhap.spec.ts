import { expect, test } from '../ha-tang/fixture';
import { baoLoiClient, meChuaDangNhap, tokenXsrf } from '../du-lieu-gia/khoi-dong';
import { thatBai } from '../ha-tang/envelope';
import { loiHeThongKhongLuong } from '../du-lieu-gia/chung';
import {
  a1MotH1,
  a2ChuHienThi,
  a3KhongCuon,
  a4HoFont,
  a5ONhap,
  a6KheGiuaVung,
  a6KheTieuDeMoTa,
  a6KheTrongForm,
  a7TuDien,
  a8CoYKhongCo,
  a9AuthCardResponsive,
  a10OMatKhauDemPhai,
  a11ToastKhongCuopFocus,
  a12FocusOLoiDau,
  KHONG_CO_DANG_NHAP_THUONG,
  KHONG_CO_DANG_NHAP_VAN_HANH,
} from '../ha-tang/lop-a';
import type { PhanTuCoYKhongCo } from '../ha-tang/lop-a/a8-co-y-khong-co';
import { axeTrangThai, lopA, nutGui } from './ca-xac-thuc';
import type { MoTrangThai, PhepDoCuaMan } from './ca-xac-thuc';

const MAN = '01-dang-nhap';
const O_TEN = '#dang-nhap-ten-dang-nhap';
const O_MAT_KHAU = '#dang-nhap-mat-khau';
// Dòng đã ghi ở bảng "Đã có → còn thiếu" của AuthCard.md — nhãn gắn vào `test.fail()` (ADR-0117 QC6).

// Ca khói T1 (ADR-0117): trang đăng nhập tải được, đúng một <h1>. H1 (không lỗi console / ngoại lệ trang)
// và H2 (không request ngoài ca giả) do fixture `apiGia` canh ở cuối test.
test('trang đăng nhập tải được, có đúng một h1', async ({ page, apiGia }) => {
  apiGia.khaiBao(tokenXsrf, baoLoiClient, meChuaDangNhap);

  await page.goto('/dang-nhap');

  const h1 = page.locator('h1');
  await expect(h1).toHaveCount(1);
  await expect(h1).toBeVisible();
});

// --- Dựng trạng thái ---------------------------------------------------------------------------------------

const moThuong: MoTrangThai = async (page, apiGia) => {
  apiGia.khaiBao(tokenXsrf, baoLoiClient, meChuaDangNhap);
  await page.goto('/dang-nhap');
  await expect(page.locator('h1')).toBeVisible();
};

const moVanHanh: MoTrangThai = async (page, apiGia) => {
  apiGia.khaiBao(tokenXsrf, baoLoiClient, meChuaDangNhap);
  await page.goto('/dang-nhap?cheDo=van-hanh');
  await expect(page.locator('h1')).toBeVisible();
};

/** Chuyển chế độ: vào chế độ thường rồi bấm liên kết sang chế độ vận hành (điều hướng thật trên cùng tuyến). */
const moChuyenCheDo: MoTrangThai = async (page, apiGia) => {
  await moThuong(page, apiGia);
  await page.locator('main a').click();
  await expect(page).toHaveURL(/cheDo=van-hanh/);
  await expect(page.locator('input[autocomplete="organization"]')).toBeVisible();
};

/** Kiểm tra dữ liệu: bấm gửi khi mọi ô còn trống — FE chặn, không gọi API; lỗi hiện dưới từng ô. */
const moKiemTraDuLieu: MoTrangThai = async (page, apiGia) => {
  await moThuong(page, apiGia);
  await nutGui(page).click();
  await expect(page.locator('main form input[aria-invalid="true"]').first()).toBeVisible();
};

const loiSaiThongTin = () =>
  ({
    phuongThuc: 'POST',
    duongDan: '/core/auth/login',
    phanHoi: thatBai('CORE.AUTH.INVALID_CREDENTIALS', { type: 'BusinessRule', status: 422 }),
    loiCoChuY: true,
  }) as const;

/** Lỗi: máy chủ trả `INVALID_CREDENTIALS` — hiện ở khu lỗi của khung, không tô đỏ ô nào. */
const moLoi: MoTrangThai = async (page, apiGia) => {
  await moThuong(page, apiGia);
  apiGia.khaiBao(loiSaiThongTin());
  await page.locator(O_TEN).fill('nguoi-dung');
  await page.locator(O_MAT_KHAU).fill('mat-khau-sai');
  await nutGui(page).click();
  await expect(page.locator('main [role="alert"]')).toBeVisible();
};

/** Đang tải: request đăng nhập treo — khung `aria-busy`, mọi ô `disabled`, giữ giá trị đã gõ. */
const moDangTai: MoTrangThai = async (page, apiGia) => {
  await moThuong(page, apiGia);
  let giaiPhong!: () => void;
  const cho = new Promise<void>((xong) => (giaiPhong = xong));
  apiGia.khaiBao({ ...loiSaiThongTin(), cho });
  await page.locator(O_TEN).fill('nguoi-dung');
  await page.locator(O_MAT_KHAU).fill('mat-khau-sai');
  await nutGui(page).click();
  await expect(page.locator('main form[aria-busy="true"]')).toBeVisible();
  return giaiPhong;
};

// --- Danh sách phép đo --------------------------------------------------------------------------------------

const a8 = (danhSach: readonly PhanTuCoYKhongCo[]): PhepDoCuaMan => ({
  ten: 'A8 cố ý không có',
  chay: (page) => a8CoYKhongCo.kiem(page, danhSach),
});

const phepDoChung = (khongCo: readonly PhanTuCoYKhongCo[]): PhepDoCuaMan[] => [
  { ten: 'A1 một h1 trong main', chay: (page) => a1MotH1.kiem(page, { trongMain: true }) },
  { ten: 'A2 không null/NaN/khoá dịch', chay: (page) => a2ChuHienThi.kiem(page) },
  { ten: 'A3 không cuộn khi đủ chỗ', chay: (page) => a3KhongCuon.kiem(page) },
  { ten: 'A4 họ font --font-sans', chay: (page) => a4HoFont.kiem(page) },
  { ten: 'A6a khe trong form', chay: (page) => a6KheTrongForm.kiem(page) },
  { ten: 'A6b khe giữa các vùng', chay: (page) => a6KheGiuaVung.kiem(page) },
  a8(khongCo),
];

const phepDoCoMoTa: PhepDoCuaMan = {
  ten: 'A6c khe tiêu đề → mô tả',
  chay: (page) => a6KheTieuDeMoTa.kiem(page),
};

// --- Mặc định: mọi khung nhìn × theme theo QC3 --------------------------------------------------------------
lopA(MAN, 'mặc định (chế độ thường)', '@mac-dinh-a', moThuong, [
  ...phepDoChung(KHONG_CO_DANG_NHAP_THUONG),
  { ten: 'A10 ô mật khẩu chừa chỗ nút hiện/ẩn', chay: (page) => a10OMatKhauDemPhai.kiem(page) },
]);
axeTrangThai(MAN, 'mặc định (chế độ thường)', '@mac-dinh-axe', moThuong);

// --- Trạng thái khác ----------------------------------------------------------------------------------------
lopA(MAN, 'chế độ vận hành', '@khac-a', moVanHanh, [
  ...phepDoChung(KHONG_CO_DANG_NHAP_VAN_HANH),
  phepDoCoMoTa,
]);
axeTrangThai(MAN, 'chế độ vận hành', '@khac-axe', moVanHanh);

lopA(MAN, 'chuyển chế độ', '@khac-a', moChuyenCheDo, [
  ...phepDoChung(KHONG_CO_DANG_NHAP_VAN_HANH),
  phepDoCoMoTa,
]);

lopA(MAN, 'kiểm tra dữ liệu', '@khac-a', moKiemTraDuLieu, [
  ...phepDoChung(KHONG_CO_DANG_NHAP_THUONG),
  { ten: 'A12 focus về ô lỗi đầu tiên', chay: (page) => a12FocusOLoiDau.kiem(page) },
]);
axeTrangThai(MAN, 'kiểm tra dữ liệu', '@khac-axe', moKiemTraDuLieu);

lopA(MAN, 'lỗi đăng nhập', '@khac-a', moLoi, phepDoChung(KHONG_CO_DANG_NHAP_THUONG));
axeTrangThai(MAN, 'lỗi đăng nhập', '@khac-axe', moLoi);

lopA(MAN, 'đang tải (đang gửi)', '@khac-a', moDangTai, phepDoChung(KHONG_CO_DANG_NHAP_THUONG));
axeTrangThai(MAN, 'đang tải (đang gửi)', '@khac-axe', moDangTai);

// --- Khung xác thực theo bề rộng (A9) — tự đặt khung nhìn, một lần ở rong-sang -----------------------------------
lopA(MAN, 'khung theo bề rộng', '@mot-khung', moThuong, [
  { ten: 'A9 AuthCard responsive', chay: (page) => a9AuthCardResponsive.kiem(page) },
]);

// --- Lỗi hệ thống 500: toast lỗi hiện, không cướp focus (A11) ------------------------------------------------------
/** Gõ dở rồi Enter: máy chủ trả 500 `CORE.SYSTEM.UNEXPECTED` — lớp lỗi xuyên suốt luôn toast kèm traceId. */
const moLoiHeThong: MoTrangThai = async (page, apiGia) => {
  await moThuong(page, apiGia);
  apiGia.khaiBao({
    phuongThuc: 'POST',
    duongDan: '/core/auth/login',
    phanHoi: loiHeThongKhongLuong(),
    loiCoChuY: true,
  });
  await page.locator(O_TEN).fill('nguoi-dung');
  await page.locator(O_MAT_KHAU).fill('mat-khau-dang-go');
};
lopA(MAN, 'lỗi hệ thống 500 (toast)', '@khac-a', moLoiHeThong, [
  {
    ten: 'A11 toast lỗi không cướp focus',
    chay: (page) =>
      a11ToastKhongCuopFocus.kiem(page, () => page.locator(O_MAT_KHAU).press('Enter')),
  },
]);

// --- Tương tác của ô nhập chữ — một lần, trên màn đầu tiên dùng ba biến thể (chế độ vận hành) ------------------
lopA(MAN, 'tương tác ô nhập (chế độ vận hành)', '@tuong-tac', moVanHanh, [
  { ten: 'A5 một đường biên hai trục', chay: (page) => a5ONhap.kiem(page, nutGui(page)) },
  { ten: 'A7 ô tự điền giữ nền', chay: (page) => a7TuDien.kiem(page) },
]);

// --- Focus sau khi gửi hỏng (AuthCard.md §Accessibility) ----------------------------------------------------------
// Khoá form lúc gửi làm ô đang focus mất focus; sau lỗi máy chủ, focus phải về đúng ô người dùng vừa bấm Enter.
for (const [ten, oGui] of [
  ['ô mật khẩu', O_MAT_KHAU],
  ['ô tên đăng nhập', O_TEN],
] as const) {
  test(`lỗi máy chủ khi Enter ở ${ten} → focus về đúng ô đó, không ở body`, async ({
    page,
    apiGia,
  }) => {
    await moThuong(page, apiGia);
    apiGia.khaiBao(loiSaiThongTin());
    await page.locator(O_TEN).fill('nguoi-dung');
    await page.locator(O_MAT_KHAU).fill('mat-khau-sai');
    await page.locator(oGui).press('Enter');
    await expect(page.locator('main [role="alert"]')).toBeVisible();
    await expect(page.locator('main form[aria-busy="true"]')).toHaveCount(0);
    await expect(page.locator(oGui)).toBeFocused();
  });
}

// --- Ô tự nhận focus lúc mở (fe-ui-conventions.md §6.5; FormRow.md, *Lỗi hiện khi nào*) ---------------------------
// Vào màn, ô đầu tự nhận focus; Tab rời ô khi chưa gõ gì thì không có dòng lỗi và `aria-invalid` không bật.
test('ô tên đăng nhập tự nhận focus: Tab rời ô khi chưa gõ → không dòng lỗi, không aria-invalid', async ({
  page,
  apiGia,
}) => {
  await moThuong(page, apiGia);
  await expect(page.locator(O_TEN)).toBeFocused();
  await page.keyboard.press('Tab');
  await expect(page.locator(O_TEN)).not.toBeFocused();
  await expect(page.locator(O_TEN)).not.toHaveAttribute('aria-invalid', 'true');
  await expect(page.locator(`${O_TEN}-error`)).toHaveCount(0);
});

test('ô tên đăng nhập: gõ rồi xoá hết rồi Tab rời ô → báo lỗi', async ({ page, apiGia }) => {
  await moThuong(page, apiGia);
  await expect(page.locator(O_TEN)).toBeFocused();
  await page.keyboard.type('a');
  await page.keyboard.press('Backspace');
  await page.keyboard.press('Tab');
  await expect(page.locator(O_TEN)).toHaveAttribute('aria-invalid', 'true');
});
