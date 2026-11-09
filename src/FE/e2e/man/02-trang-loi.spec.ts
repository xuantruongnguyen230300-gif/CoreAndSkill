import type { Page } from '@playwright/test';

import type { ApiGia } from '../ha-tang/api-gia';
import { expect, test } from '../ha-tang/fixture';
import { kichBanQuanTriDonVi, kichBanVanHanhHeThong } from '../du-lieu-gia/kich-ban';
import { a1MotH1, a2ChuHienThi, a4HoFont } from '../ha-tang/lop-a';
import { chanChunkLazy, chanChunkTuyen } from './ca-chunk-loi';
import { vaoVaKiemMotH1 } from './ca-khoi';
import { axeTrangThai, lopA } from './ca-xac-thuc';
import type { MoTrangThai, PhepDoCuaMan } from './ca-xac-thuc';

// Ca khói F2 cho docs/Design/Screens/02-trang-loi.md: hai trang lỗi nằm TRONG khung ứng dụng.
test.describe('trang lỗi — ca khói', () => {
  test('không có quyền: vào được, đúng một h1', async ({ page, apiGia }) => {
    apiGia.khaiBao(...kichBanQuanTriDonVi());
    await vaoVaKiemMotH1(page, '/khong-co-quyen');
  });

  test('không tìm thấy: URL lạ, đúng một h1', async ({ page, apiGia }) => {
    apiGia.khaiBao(...kichBanQuanTriDonVi());
    await vaoVaKiemMotH1(page, '/duong-dan-khong-ton-tai');
  });

  test('không tìm thấy: tài khoản vận hành hệ thống, đúng một h1', async ({ page, apiGia }) => {
    apiGia.khaiBao(...kichBanVanHanhHeThong());
    await vaoVaKiemMotH1(page, '/duong-dan-khong-ton-tai');
  });
});

// --- Lưới giao diện, đợt 1 (T3 của ADR-0117): hai trang lỗi nằm TRONG khung ứng dụng ----------------------------
// Spec màn: mặc định là trạng thái duy nhất (đang tải · rỗng · lỗi · kiểm tra dữ liệu: không áp dụng) và không
// có bảng *Cố ý không có* — nên không có A8. A3, A6 là luật của khung xác thực, không áp cho trang trong khung.
// Khung (Sidebar/Topbar) nằm trong ảnh và trong lần quét axe; phép đo riêng của khung là việc của T5.
const khongCoQuyen: MoTrangThai = async (page, apiGia) => {
  apiGia.khaiBao(...kichBanQuanTriDonVi());
  await page.goto('/khong-co-quyen');
  await expect(page.locator('h1')).toBeVisible();
};

const khongTimThay: MoTrangThai = async (page, apiGia) => {
  apiGia.khaiBao(...kichBanQuanTriDonVi());
  await page.goto('/duong-dan-khong-ton-tai');
  await expect(page.locator('h1')).toBeVisible();
};

const phepDoTrangLoi: PhepDoCuaMan[] = [
  { ten: 'A1 một h1', chay: (page) => a1MotH1.kiem(page) },
  { ten: 'A2 không null/NaN/khoá dịch', chay: (page) => a2ChuHienThi.kiem(page) },
  { ten: 'A4 họ font --font-sans', chay: (page) => a4HoFont.kiem(page) },
];

lopA('02-trang-loi', 'không có quyền', '@mac-dinh-a', khongCoQuyen, phepDoTrangLoi);
axeTrangThai('02-trang-loi', 'không có quyền', '@mac-dinh-axe', khongCoQuyen);
lopA('02-trang-loi', 'không tìm thấy', '@mac-dinh-a', khongTimThay, phepDoTrangLoi);
axeTrangThai('02-trang-loi', 'không tìm thấy', '@mac-dinh-axe', khongTimThay);

// --- Không tải được phần ứng dụng (chốt 2026-10-08) ------------------------------------------------------------------
// Hai nhánh của quy-uoc/fe-routing-guard.md §8.1: chưa màn nào hiện → khối toàn trang; đã có màn → toast + nút.
// Chunk lazy bị chặn bằng `page.route` (ca-chunk-loi.ts); "Tải lại trang" thật sự tải lại toàn trang tại địa chỉ hiện tại.
const TIEU_DE_KHOI = 'Không tải được trang';
const NUT_TAI_LAI = 'Tải lại trang';
const CAU_LOI =
  'Hệ thống vừa được cập nhật hoặc kết nối bị gián đoạn nên trang chưa tải xong. Tải lại trang để tiếp tục.';
const TIEU_DE_KHONG_CO_QUYEN = 'Không có quyền truy cập';
const LIEN_KET_TRANG_CHU = 'a[href="/trang-chu"]';
const MOT_TOAST = 'app-toast .p-toast-message';

const duongDanHienTai = (url: string): string => {
  const u = new URL(url);
  return u.pathname + u.search;
};

test.describe('không tải được phần ứng dụng — chưa có màn nào (khối toàn trang)', () => {
  test('khối thay trang trắng: một h1, một nút, không khung, thanh địa chỉ giữ URL đích', async ({
    page,
    apiGia,
  }) => {
    apiGia.khaiBao(...kichBanQuanTriDonVi());
    await chanChunkTuyen(page, apiGia, '/khong-co-quyen');

    await page.goto('/khong-co-quyen?lan=1');

    const h1 = page.locator('h1');
    await expect(h1).toHaveCount(1);
    await expect(h1).toHaveText(TIEU_DE_KHOI);
    await expect(page.getByText(CAU_LOI)).toBeVisible();
    await expect(page.getByRole('button', { name: NUT_TAI_LAI })).toHaveCount(1);
    await expect(page.locator('main button')).toHaveCount(1);
    // Không Sidebar, không Topbar: khối đứng một mình.
    await expect(page.locator('app-sidebar, app-topbar, nav, aside')).toHaveCount(0);
    expect(duongDanHienTai(page.url())).toBe('/khong-co-quyen?lan=1');
  });

  test('focus: Tab đầu tiên vào nút "Tải lại trang"', async ({ page, apiGia }) => {
    apiGia.khaiBao(...kichBanQuanTriDonVi());
    await chanChunkTuyen(page, apiGia, '/khong-co-quyen');
    await page.goto('/khong-co-quyen');
    await expect(page.locator('h1')).toHaveText(TIEU_DE_KHOI);

    await page.keyboard.press('Tab');

    await expect(page.getByRole('button', { name: NUT_TAI_LAI })).toBeFocused();
  });

  test('bấm "Tải lại trang": tải lại toàn trang tại ĐÚNG địa chỉ đích, không về /', async ({
    page,
    apiGia,
  }) => {
    apiGia.khaiBao(...kichBanQuanTriDonVi());
    const chan = await chanChunkTuyen(page, apiGia, '/khong-co-quyen');
    await page.goto('/khong-co-quyen?lan=2');
    await expect(page.locator('h1')).toHaveText(TIEU_DE_KHOI);

    chan.tat(); // mạng/bản mới đã ổn lại
    await Promise.all([
      page.waitForEvent('load'),
      page.getByRole('button', { name: NUT_TAI_LAI }).click(),
    ]);

    await expect(page.locator('h1')).toHaveText(TIEU_DE_KHONG_CO_QUYEN);
    expect(duongDanHienTai(page.url())).toBe('/khong-co-quyen?lan=2');
  });
});

test.describe('không tải được phần ứng dụng — đã có màn đang hiện (toast)', () => {
  async function vaoManRoiChan(page: Page, apiGia: ApiGia) {
    apiGia.khaiBao(...kichBanQuanTriDonVi());
    const chan = await chanChunkLazy(page, apiGia);
    await page.goto('/khong-co-quyen');
    await expect(page.locator('h1')).toHaveText(TIEU_DE_KHONG_CO_QUYEN);
    chan.bat();
    return chan;
  }

  test('màn giữ nguyên, toast lỗi kèm nút, URL ở lại màn cũ, không khối toàn trang', async ({
    page,
    apiGia,
  }) => {
    await vaoManRoiChan(page, apiGia);

    await page.locator(LIEN_KET_TRANG_CHU).first().click();

    const toast = page.locator(MOT_TOAST);
    await expect(toast).toHaveCount(1);
    await expect(toast).toContainText(CAU_LOI);
    await expect(toast.getByRole('button', { name: NUT_TAI_LAI })).toBeVisible();
    await expect(page.locator('h1')).toHaveCount(1);
    await expect(page.locator('h1')).toHaveText(TIEU_DE_KHONG_CO_QUYEN);
    expect(duongDanHienTai(page.url())).toBe('/khong-co-quyen');
    // Toast `danger` không tự tắt (Toast.md §Biến thể): vẫn còn sau thời gian dài hơn mọi vai tự tắt.
    await page.waitForTimeout(8500);
    await expect(toast).toHaveCount(1);
  });

  test('focus: toast không cướp focus; Tab đi nút hành động rồi tới nút đóng', async ({
    page,
    apiGia,
  }) => {
    await vaoManRoiChan(page, apiGia);

    await page.locator(LIEN_KET_TRANG_CHU).first().focus();
    await page.keyboard.press('Enter');
    const toast = page.locator(MOT_TOAST);
    await expect(toast).toHaveCount(1);
    await page.waitForTimeout(300);
    // Không nút nào của toast giữ focus.
    await expect(toast.locator('button:focus')).toHaveCount(0);

    await toast.getByRole('button', { name: NUT_TAI_LAI }).focus();
    await page.keyboard.press('Tab');
    await expect(toast.locator('button[aria-label]')).toBeFocused();
  });

  test('bấm "Tải lại trang": tải lại toàn trang tại địa chỉ hiện tại, toast biến mất cùng trang cũ', async ({
    page,
    apiGia,
  }) => {
    const chan = await vaoManRoiChan(page, apiGia);
    await page.locator(LIEN_KET_TRANG_CHU).first().click();
    const toast = page.locator(MOT_TOAST);
    await expect(toast).toHaveCount(1);

    chan.tat();
    await Promise.all([
      page.waitForEvent('load'),
      toast.getByRole('button', { name: NUT_TAI_LAI }).click(),
    ]);

    await expect(page.locator('h1')).toHaveText(TIEU_DE_KHONG_CO_QUYEN);
    await expect(page.locator(MOT_TOAST)).toHaveCount(0);
    expect(duongDanHienTai(page.url())).toBe('/khong-co-quyen');
  });
});

// axe WCAG A/AA cho hai trạng thái mới (ADR-0117 QC5): khối toàn trang, và màn có toast kèm nút.
const khoiToanTrang: MoTrangThai = async (page, apiGia) => {
  apiGia.khaiBao(...kichBanQuanTriDonVi());
  await chanChunkTuyen(page, apiGia, '/khong-co-quyen');
  await page.goto('/khong-co-quyen');
  await expect(page.locator('h1')).toHaveText(TIEU_DE_KHOI);
};

const manCoToast: MoTrangThai = async (page, apiGia) => {
  apiGia.khaiBao(...kichBanQuanTriDonVi());
  const chan = await chanChunkLazy(page, apiGia);
  await page.goto('/khong-co-quyen');
  await expect(page.locator('h1')).toHaveText(TIEU_DE_KHONG_CO_QUYEN);
  chan.bat();
  await page.locator(LIEN_KET_TRANG_CHU).first().click();
  await expect(page.locator(MOT_TOAST)).toHaveCount(1);
  await page.waitForTimeout(400); // hoạt ảnh vào của toast
};

lopA('02-trang-loi', 'không tải được phần ứng dụng', '@khac-a', khoiToanTrang, [
  { ten: 'A1 một h1', chay: (page) => a1MotH1.kiem(page) },
  { ten: 'A2 không null/NaN/khoá dịch', chay: (page) => a2ChuHienThi.kiem(page) },
  { ten: 'A4 họ font --font-sans', chay: (page) => a4HoFont.kiem(page) },
]);
axeTrangThai('02-trang-loi', 'không tải được phần ứng dụng', '@khac-axe', khoiToanTrang);
axeTrangThai('02-trang-loi', 'điều hướng hỏng khi đã có màn (toast)', '@khac-axe', manCoToast);
