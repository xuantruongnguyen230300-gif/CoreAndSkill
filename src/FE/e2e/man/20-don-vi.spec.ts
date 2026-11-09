import { test } from '../ha-tang/fixture';
import { kichBanVanHanhHeThong } from '../du-lieu-gia/kich-ban';
import {
  danhSachDonViCoDuLieu,
  danhSachDonViLoiTai,
  danhSachDonViRong,
  danhSachDonViTimKhongRa,
} from '../du-lieu-gia/don-vi';
import { vaoVaKiemMotH1 } from './ca-khoi';
import { moManDangTai, moManVaoH1, phuManTrongKhung } from './ca-trong-khung';
import {
  kiemChiThanBangCuon,
  kiemCotHanhDong,
  kiemKhoiLien,
  kiemSanVungCuon,
  kiemVungBaoKetQuaKhiKhongCo,
} from './ca-khoi-danh-sach';

// Ca khói F2 cho docs/Design/Screens/20-don-vi.md — màn của tài khoản vận hành hệ thống.
test.describe('đơn vị (khu hệ thống) — ca khói', () => {
  test('có dữ liệu: vào được, đúng một h1', async ({ page, apiGia }) => {
    apiGia.khaiBao(...kichBanVanHanhHeThong(), danhSachDonViCoDuLieu());
    await vaoVaKiemMotH1(page, '/he-thong/don-vi');
  });

  test('rỗng: đúng một h1', async ({ page, apiGia }) => {
    apiGia.khaiBao(...kichBanVanHanhHeThong(), danhSachDonViRong());
    await vaoVaKiemMotH1(page, '/he-thong/don-vi');
  });

  test('tìm không ra: URL có searchText, đúng một h1', async ({ page, apiGia }) => {
    apiGia.khaiBao(...kichBanVanHanhHeThong(), danhSachDonViTimKhongRa());
    await vaoVaKiemMotH1(page, '/he-thong/don-vi?searchText=khong-co-don-vi-nhu-vay');
  });

  test('lỗi tải có chủ ý: đúng một h1', async ({ page, apiGia }) => {
    apiGia.khaiBao(...kichBanVanHanhHeThong(), danhSachDonViLoiTai());
    await vaoVaKiemMotH1(page, '/he-thong/don-vi');
  });
});

// --- Lưới giao diện, đợt 2 (T3 lượt b của ADR-0117) ----------------------------------------------------------------
// Trạng thái theo mục *Trạng thái* của 20-don-vi.md (khu hệ thống, tài khoản vận hành). CHƯA bật — không có dữ liệu
// giả cho ca POST/PUT/PATCH: các hộp tạo đơn vị, ngưng/bật, khôi phục, tạo quản trị và trạng thái *kiểm tra dữ liệu* trong hộp.
const kb = kichBanVanHanhHeThong();
const DS = '/he-thong/don-vi';

phuManTrongKhung('20-don-vi', [
  {
    ten: 'mặc định (có dữ liệu)',
    macDinh: true,
    mo: moManVaoH1(DS, [...kb, danhSachDonViCoDuLieu()]),
  },
  { ten: 'đang tải', mo: moManDangTai(DS, kb, danhSachDonViCoDuLieu()) },
  { ten: 'rỗng (chưa có đơn vị)', mo: moManVaoH1(DS, [...kb, danhSachDonViRong()]) },
  {
    ten: 'rỗng (tìm không ra)',
    mo: moManVaoH1(`${DS}?searchText=khong-co-don-vi-nhu-vay`, [...kb, danhSachDonViTimKhongRa()]),
  },
  { ten: 'lỗi tải', mo: moManVaoH1(DS, [...kb, danhSachDonViLoiTai()]) },
]);

// Khối danh sách liền (ListScreen.md §2–§4, DataTable.md §"Cột hành động") — đo trên trình duyệt, project `rong-sang`.
test.describe('đơn vị — danh sách — khối liền và cột hành động', () => {
  test.beforeEach(({ apiGia }) => apiGia.khaiBao(...kb, danhSachDonViCoDuLieu()));

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
test.describe('20-don-vi — danh sách — vùng báo kết quả', () => {
  test('lọc ra 0 bản ghi: vùng role=status có câu "không có kết quả", không còn aria-live nào khác', async ({
    page,
    apiGia,
  }) => {
    apiGia.khaiBao(...kichBanVanHanhHeThong(), danhSachDonViTimKhongRa());
    await kiemVungBaoKetQuaKhiKhongCo(page, '/he-thong/don-vi?searchText=khong-co-don-vi-nhu-vay');
  });
});
