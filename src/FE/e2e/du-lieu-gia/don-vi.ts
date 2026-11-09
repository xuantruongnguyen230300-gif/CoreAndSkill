// Dữ liệu giả của màn đơn vị (khu hệ thống). Card nguồn: docs/contracts/tenants.md §1
// (`GET /core/system/tenants`). Đơn vị hệ thống KHÔNG nằm trong kết quả; màn không có chi tiết.
import type { DonViDto } from '../../src/app/platform/he-thong/don-vi/models/don-vi.dto';
import type { CaGia } from '../ha-tang/api-gia';
import { loiHeThongKhongLuong, trangDanhSach } from './chung';

const DUONG_DAN = '/core/system/tenants';

const TEN: readonly string[] = [
  'Sở Y tế',
  'Sở Giáo dục và Đào tạo',
  'Sở Nông nghiệp và Phát triển nông thôn',
  'Uỷ ban nhân dân quận Hoàn Kiếm',
  'Bệnh viện Đa khoa Khu vực',
  'Trường Trung học phổ thông Chuyên',
  'Công ty Cổ phần Cấp nước',
  'Trung tâm Kiểm soát bệnh tật',
];

const TEN_DAI =
  'Ban Quản lý các dự án đầu tư xây dựng công trình giao thông và hạ tầng kỹ thuật đô thị khu vực phía Nam';

function donVi(chiSo: number, rieng: Partial<DonViDto> = {}): DonViDto {
  return {
    id: `0192f3c1-8a4e-7d21-9f10-${String(chiSo + 1).padStart(12, '0')}`,
    code: `DV-${String(chiSo + 1).padStart(3, '0')}`,
    name: TEN[chiSo % TEN.length]!,
    isActive: true,
    createdAt: `2026-09-${String((chiSo % 28) + 1).padStart(2, '0')}T03:12:44Z`,
    ...rieng,
  };
}

function trangMau(): readonly DonViDto[] {
  const ds = Array.from({ length: 8 }, (_, i) => donVi(i));
  ds[2] = donVi(2, { isActive: false });
  ds[4] = donVi(4, { createdAt: null });
  ds[5] = donVi(5, { code: 'BQL-GTVT-HTKT-PHIA-NAM', name: TEN_DAI });
  return ds;
}

/** Danh sách có dữ liệu: tám đơn vị (một đã ngưng, một không ngày tạo, một mã và tên dài). */
export function danhSachDonViCoDuLieu(): CaGia {
  return {
    phuongThuc: 'GET',
    duongDan: DUONG_DAN,
    phanHoi: trangDanhSach(trangMau(), { totalCount: 8 }),
  };
}

/** Danh sách rỗng lần đầu dùng — chưa đơn vị nào (ca `first-use`, có nút "Tạo đơn vị"). */
export function danhSachDonViRong(): CaGia {
  return { phuongThuc: 'GET', duongDan: DUONG_DAN, phanHoi: trangDanhSach<DonViDto>([]) };
}

/** Tìm không ra: trên dây giống danh sách rỗng; khác ở URL có `searchText` (ca `no-results`). */
export function danhSachDonViTimKhongRa(): CaGia {
  return danhSachDonViRong();
}

/** Lỗi tải danh sách có chủ ý: 500 `CORE.SYSTEM.UNEXPECTED`. */
export function danhSachDonViLoiTai(): CaGia {
  return {
    phuongThuc: 'GET',
    duongDan: DUONG_DAN,
    phanHoi: loiHeThongKhongLuong(),
    loiCoChuY: true,
  };
}
