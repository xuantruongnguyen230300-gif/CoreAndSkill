import { test } from '../ha-tang/fixture';
import { kichBanQuanTriDonVi } from '../du-lieu-gia/kich-ban';
import {
  maTranPhanQuyenCoDuLieu,
  maTranPhanQuyenKhongCoVaiTro,
  maTranPhanQuyenLoiTai,
} from '../du-lieu-gia/phan-quyen';
import { vaoVaKiemMotH1 } from './ca-khoi';
import { moManDangTai, moManVaoH1, phuManTrongKhung } from './ca-trong-khung';

// Ca khói F2 cho docs/Design/Screens/12-ma-tran-phan-quyen.md.
test.describe('ma trận phân quyền — ca khói', () => {
  test('có dữ liệu: vào được, đúng một h1', async ({ page, apiGia }) => {
    apiGia.khaiBao(...kichBanQuanTriDonVi(), maTranPhanQuyenCoDuLieu());
    await vaoVaKiemMotH1(page, '/quan-tri/phan-quyen');
  });

  test('không có vai trò nào: đúng một h1', async ({ page, apiGia }) => {
    apiGia.khaiBao(...kichBanQuanTriDonVi(), maTranPhanQuyenKhongCoVaiTro());
    await vaoVaKiemMotH1(page, '/quan-tri/phan-quyen');
  });

  test('lỗi tải có chủ ý: đúng một h1', async ({ page, apiGia }) => {
    apiGia.khaiBao(...kichBanQuanTriDonVi(), maTranPhanQuyenLoiTai());
    await vaoVaKiemMotH1(page, '/quan-tri/phan-quyen');
  });
});

// --- Lưới giao diện, đợt 2 (T3 lượt b của ADR-0117) ----------------------------------------------------------------
// Trạng thái theo mục *Trạng thái* của 12-ma-tran-phan-quyen.md. *Lọc không ra*, *kiểm tra dữ liệu*: không áp dụng
// (spec). CHƯA bật — không có dữ liệu giả cho: danh mục quyền rỗng (`not-configured`), `PUT` lưu và lỗi lưu, "Tải lại ma trận".
const kb = kichBanQuanTriDonVi();
const DS = '/quan-tri/phan-quyen';

phuManTrongKhung('12-ma-tran-phan-quyen', [
  {
    ten: 'mặc định (có dữ liệu)',
    macDinh: true,
    mo: moManVaoH1(DS, [...kb, maTranPhanQuyenCoDuLieu()]),
  },
  { ten: 'đang tải', mo: moManDangTai(DS, kb, maTranPhanQuyenCoDuLieu()) },
  {
    ten: 'rỗng (đơn vị chưa có vai trò)',
    mo: moManVaoH1(DS, [...kb, maTranPhanQuyenKhongCoVaiTro()]),
  },
  { ten: 'lỗi tải', mo: moManVaoH1(DS, [...kb, maTranPhanQuyenLoiTai()]) },
]);
