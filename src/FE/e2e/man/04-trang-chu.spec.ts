import { test } from '../ha-tang/fixture';
import { kichBanQuanTriDonVi, kichBanVanHanhHeThong } from '../du-lieu-gia/kich-ban';
import {
  menuLoiTai,
  menuQuanTriDonVi,
  menuRong,
  meDaDangNhap,
  phienQuanTriDonVi,
} from '../du-lieu-gia/phien';
import { vaoVaKiemMotH1 } from './ca-khoi';
import { moManDangTai, moManVaoH1, phuManTrongKhung } from './ca-trong-khung';

// Ca khói F2 cho docs/Design/Screens/04-trang-chu.md và khung ứng dụng (00-khung-ung-dung.md): trang chủ
// luôn nằm trong khung, nên các ca ở đây cũng chạy khung với các trạng thái menu.
test.describe('trang chủ và khung ứng dụng — ca khói', () => {
  test('quản trị đơn vị: vào /trang-chu, đúng một h1', async ({ page, apiGia }) => {
    apiGia.khaiBao(...kichBanQuanTriDonVi());
    await vaoVaKiemMotH1(page, '/trang-chu');
  });

  test('URL gốc / chuyển sang trang chủ, đúng một h1', async ({ page, apiGia }) => {
    apiGia.khaiBao(...kichBanQuanTriDonVi());
    await vaoVaKiemMotH1(page, '/');
    await page.waitForURL('**/trang-chu');
  });

  test('tài khoản vận hành hệ thống: vào /trang-chu, đúng một h1', async ({ page, apiGia }) => {
    apiGia.khaiBao(...kichBanVanHanhHeThong());
    await vaoVaKiemMotH1(page, '/trang-chu');
  });

  test('họ tên và tên đơn vị rất dài: đúng một h1', async ({ page, apiGia }) => {
    apiGia.khaiBao(
      ...kichBanQuanTriDonVi(),
      meDaDangNhap(
        phienQuanTriDonVi({
          fullName:
            'Nguyễn Thị Thuỳ Dương Phương Anh Bảo Châu Minh Khuê Thanh Tâm Hồng Ngọc Diễm Quỳnh Như Ý Lan Chi Hoa',
          tenantName:
            'Ban Quản lý các dự án đầu tư xây dựng công trình giao thông và hạ tầng kỹ thuật đô thị khu vực phía Nam',
        }),
      ),
    );
    await vaoVaKiemMotH1(page, '/trang-chu');
  });

  test('menu rỗng: khung vẫn dựng, đúng một h1', async ({ page, apiGia }) => {
    apiGia.khaiBao(...kichBanQuanTriDonVi(), menuRong());
    await vaoVaKiemMotH1(page, '/trang-chu');
  });

  test('menu lỗi tải có chủ ý: khung vẫn dựng, đúng một h1', async ({ page, apiGia }) => {
    apiGia.khaiBao(...kichBanQuanTriDonVi(), menuLoiTai());
    await vaoVaKiemMotH1(page, '/trang-chu');
  });
});

// --- Lưới giao diện, đợt 2 (T3 lượt b của ADR-0117): trang chủ và khung ứng dụng -----------------------------------
// Trạng thái lấy từ mục *Trạng thái* của 00-khung-ung-dung.md (Sidebar theo menu: đang tải · rỗng · lỗi) — trang chủ
// đi theo trạng thái menu của khung. CHƯA bật (04-trang-chu.md §Trạng thái nói, code chưa có — bảng *Có thật hôm nay → sẽ thành*,
// dòng *Trang*): khối lối tắt, `EmptyState` rỗng/lỗi của trang, `Card` đang tải. Phép đo riêng của Sidebar/Topbar
// (thu gọn ở `vua`, drawer ở `hep`) cũng chưa bật: biến thể `collapsed`/`drawer` chưa có (00-khung-ung-dung.md, dòng *Biến thể*).
const MAN = '04-trang-chu';

phuManTrongKhung(MAN, [
  {
    ten: 'mặc định (quản trị đơn vị)',
    macDinh: true,
    mo: moManVaoH1('/trang-chu', kichBanQuanTriDonVi()),
  },
  { ten: 'mặc định (vận hành hệ thống)', mo: moManVaoH1('/trang-chu', kichBanVanHanhHeThong()) },
  {
    ten: 'họ tên và tên đơn vị rất dài',
    mo: moManVaoH1('/trang-chu', [
      ...kichBanQuanTriDonVi(),
      meDaDangNhap(
        phienQuanTriDonVi({
          fullName:
            'Nguyễn Thị Thuỳ Dương Phương Anh Bảo Châu Minh Khuê Thanh Tâm Hồng Ngọc Diễm Quỳnh Như Ý Lan Chi Hoa',
          tenantName:
            'Ban Quản lý các dự án đầu tư xây dựng công trình giao thông và hạ tầng kỹ thuật đô thị khu vực phía Nam',
        }),
      ),
    ]),
  },
  {
    ten: 'khung đang tải menu',
    mo: moManDangTai('/trang-chu', kichBanQuanTriDonVi(), menuQuanTriDonVi(), {
      chiBao: 'nav.sidebar p',
    }),
  },
  { ten: 'khung menu rỗng', mo: moManVaoH1('/trang-chu', [...kichBanQuanTriDonVi(), menuRong()]) },
  {
    ten: 'khung lỗi tải menu',
    mo: moManVaoH1('/trang-chu', [...kichBanQuanTriDonVi(), menuLoiTai()]),
  },
]);
