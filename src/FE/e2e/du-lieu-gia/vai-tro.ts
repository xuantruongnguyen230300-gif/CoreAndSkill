// Dữ liệu giả của màn vai trò. Card nguồn: docs/contracts/roles.md §1 (`GET /core/roles`) và §5
// (`GET /core/roles/{id}` — cùng hình dạng một phần tử `items`). Cùng đường dẫn `/core/roles` còn phục
// vụ ô chọn vai trò ở màn người dùng (tìm theo `searchText`), nên các hàm danh sách dùng được cho cả hai.
import type { VaiTroDto } from '../../src/app/platform/quan-tri/vai-tro/models/vai-tro.dto';
import type { CaGia } from '../ha-tang/api-gia';
import { thanhCong, thatBai } from '../ha-tang/envelope';
import { loiHeThongKhongLuong, trangDanhSach } from './chung';

const DUONG_DAN = '/core/roles';

const TEN: readonly string[] = [
  'Quản trị hệ thống',
  'Nhân viên',
  'Kế toán trưởng',
  'Văn thư lưu trữ',
  'Chuyên viên tổng hợp',
  'Lãnh đạo đơn vị',
  'Thanh tra viên',
  'Cán bộ tiếp nhận một cửa',
  'Kiểm soát viên nội bộ',
  'Trưởng phòng',
];

const TEN_DAI =
  'Chuyên viên phụ trách thẩm định hồ sơ đề nghị cấp phép hoạt động tại các cơ sở trực thuộc đơn vị';

function hau(chiSo: number): string {
  return String(chiSo + 1).padStart(12, '0');
}

function vaiTro(chiSo: number, rieng: Partial<VaiTroDto> = {}): VaiTroDto {
  return {
    id: `0192f3c2-1111-7000-8000-${hau(chiSo)}`,
    name: TEN[chiSo % TEN.length]!,
    isSystem: chiSo === 0,
    userCount: chiSo === 0 ? 3 : chiSo * 4,
    createdAt: `2026-09-${String((chiSo % 28) + 1).padStart(2, '0')}T03:12:44Z`,
    version: `4b1f0c2e-7a3d-4e59-9c86-${hau(chiSo)}`,
    ...rieng,
  };
}

function trangMau(): readonly VaiTroDto[] {
  const ds = Array.from({ length: 10 }, (_, i) => vaiTro(i));
  ds[3] = vaiTro(3, { userCount: 0 });
  ds[4] = vaiTro(4, { name: TEN_DAI });
  ds[5] = vaiTro(5, { createdAt: null });
  return ds;
}

/** Danh sách có dữ liệu: mười vai trò (một vai trò hệ thống, một chưa ai mang, một tên dài, một không ngày tạo). */
export function danhSachVaiTroCoDuLieu(): CaGia {
  return { phuongThuc: 'GET', duongDan: DUONG_DAN, phanHoi: trangDanhSach(trangMau()) };
}

/** Danh sách rỗng lần đầu dùng. */
export function danhSachVaiTroRong(): CaGia {
  return { phuongThuc: 'GET', duongDan: DUONG_DAN, phanHoi: trangDanhSach<VaiTroDto>([]) };
}

/** Tìm không ra: trên dây giống danh sách rỗng; khác ở URL có `searchText`. */
export function danhSachVaiTroTimKhongRa(): CaGia {
  return danhSachVaiTroRong();
}

/** Lỗi tải danh sách có chủ ý: 500 `CORE.SYSTEM.UNEXPECTED`. */
export function danhSachVaiTroLoiTai(): CaGia {
  return {
    phuongThuc: 'GET',
    duongDan: DUONG_DAN,
    phanHoi: loiHeThongKhongLuong(),
    loiCoChuY: true,
  };
}

/** Id vai trò hệ thống đầu tiên của trang mẫu. */
export const ID_VAI_TRO_HE_THONG = vaiTro(0).id;

/** `GET /core/roles/{id}` của vai trò hệ thống đầu tiên (màn người dùng dùng để dựng chip lọc `roleId`). */
export function chiTietVaiTroHeThong(): CaGia {
  const dto = vaiTro(0);
  return { phuongThuc: 'GET', duongDan: `${DUONG_DAN}/${dto.id}`, phanHoi: thanhCong(dto) };
}

/** 404 `CORE.ROLE.NOT_FOUND` (roles.md §5). */
export function chiTietVaiTroKhongTimThay(id: string): CaGia {
  return {
    phuongThuc: 'GET',
    duongDan: `${DUONG_DAN}/${id}`,
    phanHoi: thatBai('CORE.ROLE.NOT_FOUND', { type: 'NotFound', status: 404 }),
    loiCoChuY: true,
  };
}
