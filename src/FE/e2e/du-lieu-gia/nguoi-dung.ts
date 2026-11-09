// Dữ liệu giả của màn người dùng. Card nguồn: docs/contracts/users.md §3 (`GET /core/users`) và §4
// (`GET /core/users/{id}` — cùng hình dạng một phần tử `items`). Mỗi hàm tạo một ca rõ tên.
import type {
  NguoiDungDto,
  VaiTroRutGonDto,
} from '../../src/app/platform/quan-tri/nguoi-dung/models/nguoi-dung.dto';
import type { CaGia } from '../ha-tang/api-gia';
import { thanhCong, thatBai } from '../ha-tang/envelope';
import { loiHeThongKhongLuong, trangDanhSach } from './chung';

const DUONG_DAN = '/core/users';

export const VAI_TRO_QUAN_TRI: VaiTroRutGonDto = {
  id: '0192f3c2-1111-7000-8000-000000000001',
  name: 'Quản trị hệ thống',
  isSystem: true,
};
export const VAI_TRO_NHAN_VIEN: VaiTroRutGonDto = {
  id: '0192f3c2-1111-7000-8000-000000000002',
  name: 'Nhân viên',
  isSystem: false,
};
const VAI_TRO_DAI: VaiTroRutGonDto = {
  id: '0192f3c2-1111-7000-8000-000000000009',
  name: 'Chuyên viên phụ trách thẩm định hồ sơ đề nghị cấp phép hoạt động tại các cơ sở trực thuộc',
  isSystem: false,
};

const HO_TEN: readonly string[] = [
  'Nguyễn Văn An',
  'Trần Thị Bích Ngọc',
  'Lê Hoàng Đức',
  'Phạm Thị Thu Hằng',
  'Hoàng Quốc Việt',
  'Đặng Thị Mỹ Linh',
  'Vũ Đình Khôi',
  'Bùi Thị Ánh Tuyết',
  'Đỗ Ngọc Quỳnh Anh',
  'Ngô Thanh Sơn',
  'Dương Thị Hồng Nhung',
  'Lý Minh Triết',
  'Phan Thị Kim Oanh',
  'Trịnh Văn Phúc',
  'Mai Thị Xuân Đào',
  'Đinh Công Hậu',
  'Cao Thị Ngân Hà',
  'Tạ Quang Vinh',
  'Lương Thị Yến',
  'Võ Đăng Khoa',
];

/** Họ tên và email rất dài, để thử tràn ô bảng và tiêu đề trang chi tiết. */
const HO_TEN_DAI =
  'Nguyễn Thị Thuỳ Dương Phương Anh Bảo Châu Minh Khuê Thanh Tâm Hồng Ngọc Diễm Quỳnh Như Ý Lan Chi Hoa';
const EMAIL_DAI =
  'nguyen.thi.thuy.duong.phuong.anh.bao.chau.minh.khue.thanh.tam@phong-chuyen-mon-rat-dai.don-vi-mau.example';
const TEN_DANG_NHAP_DAI = 'ten.dang.nhap.rat.dai.de.thu.tran.o.bang';

function hau(chiSo: number): string {
  return String(chiSo).padStart(12, '0');
}

function nguoiDung(chiSo: number, rieng: Partial<NguoiDungDto> = {}): NguoiDungDto {
  const userName = `nguoi.dung.${chiSo + 1}`;
  return {
    id: `0192f3c1-8a4e-7d21-9f10-${hau(chiSo + 1)}`,
    userName,
    email: `${userName}@don-vi-mau.example`,
    fullName: HO_TEN[chiSo % HO_TEN.length]!,
    roles: chiSo % 3 === 0 ? [VAI_TRO_QUAN_TRI] : [VAI_TRO_NHAN_VIEN],
    isLocked: false,
    lockoutEnd: null,
    lockedByAdmin: false,
    mustChangePassword: false,
    createdAt: `2026-09-${String((chiSo % 28) + 1).padStart(2, '0')}T03:12:45.120Z`,
    version: `9d3b4c7e-2f10-4a8b-b1c5-${hau(chiSo + 1)}`,
    ...rieng,
  };
}

/** Hai mươi dòng, trong đó vài dòng mang biến thể ô: nhiều vai trò, khoá tay, khoá tự động, không email, tên dài. */
function trangMau(): readonly NguoiDungDto[] {
  const ds = Array.from({ length: 20 }, (_, i) => nguoiDung(i));
  ds[1] = nguoiDung(1, { roles: [VAI_TRO_NHAN_VIEN, VAI_TRO_DAI, VAI_TRO_QUAN_TRI] });
  ds[2] = nguoiDung(2, {
    isLocked: true,
    lockedByAdmin: true,
    lockoutEnd: '2126-01-01T00:00:00.000Z',
    roles: [],
  });
  ds[3] = nguoiDung(3, {
    isLocked: true,
    lockedByAdmin: false,
    lockoutEnd: '2026-10-06T08:30:00.000Z',
  });
  ds[4] = nguoiDung(4, { email: null, createdAt: null });
  ds[5] = nguoiDung(5, { mustChangePassword: true, roles: [] });
  ds[6] = nguoiDung(6, { fullName: HO_TEN_DAI, email: EMAIL_DAI, userName: TEN_DANG_NHAP_DAI });
  return ds;
}

/** Danh sách có dữ liệu: trang 1 của 137 người, hai mươi dòng, đủ biến thể ô. */
export function danhSachNguoiDungCoDuLieu(): CaGia {
  return {
    phuongThuc: 'GET',
    duongDan: DUONG_DAN,
    phanHoi: trangDanhSach(trangMau(), { totalCount: 137 }),
  };
}

/** Danh sách rỗng lần đầu dùng — chưa người dùng nào (ca `first-use` của EmptyState). */
export function danhSachNguoiDungRong(): CaGia {
  return {
    phuongThuc: 'GET',
    duongDan: DUONG_DAN,
    phanHoi: trangDanhSach<NguoiDungDto>([], { totalCount: 0 }),
  };
}

/** Tìm không ra: trên dây giống danh sách rỗng; khác ở URL có `searchText` — spec màn mở URL kèm tham số. */
export function danhSachNguoiDungTimKhongRa(): CaGia {
  return danhSachNguoiDungRong();
}

/** Lỗi tải danh sách có chủ ý: 500 `CORE.SYSTEM.UNEXPECTED`. */
export function danhSachNguoiDungLoiTai(): CaGia {
  return {
    phuongThuc: 'GET',
    duongDan: DUONG_DAN,
    phanHoi: loiHeThongKhongLuong(),
    loiCoChuY: true,
  };
}

/** `roleId` không tồn tại: 422 `CORE.USER.ROLE_NOT_FOUND` — màn hiện lỗi với nút "Xoá bộ lọc" (users.md §3). */
export function danhSachNguoiDungVaiTroKhongTonTai(roleId: string): CaGia {
  return {
    phuongThuc: 'GET',
    duongDan: DUONG_DAN,
    phanHoi: thatBai('CORE.USER.ROLE_NOT_FOUND', {
      type: 'BusinessRule',
      status: 422,
      messageParams: { RoleId: roleId },
    }),
    loiCoChuY: true,
  };
}

/** Id của người dùng bình thường (dòng đầu của trang mẫu). */
export const ID_NGUOI_DUNG_MAU = nguoiDung(0).id;
/** Id của người dùng bị quản trị khoá, chưa có vai trò. */
export const ID_NGUOI_DUNG_BI_KHOA = nguoiDung(2).id;
/** Id của người dùng có họ tên, email, tên đăng nhập rất dài. */
export const ID_NGUOI_DUNG_TEN_DAI = nguoiDung(6).id;

function chiTiet(dto: NguoiDungDto): CaGia {
  return { phuongThuc: 'GET', duongDan: `${DUONG_DAN}/${dto.id}`, phanHoi: thanhCong(dto) };
}

/** Chi tiết một người dùng bình thường (có vai trò hệ thống). Id: `ID_NGUOI_DUNG_MAU`. */
export function chiTietNguoiDungMau(): CaGia {
  return chiTiet(nguoiDung(0));
}

/** Chi tiết người dùng bị quản trị khoá. Id: `ID_NGUOI_DUNG_BI_KHOA`. */
export function chiTietNguoiDungBiKhoa(): CaGia {
  return chiTiet(trangMau()[2]!);
}

/** Chi tiết người dùng có chuỗi dài để thử tràn. Id: `ID_NGUOI_DUNG_TEN_DAI`. */
export function chiTietNguoiDungTenDai(): CaGia {
  return chiTiet(trangMau()[6]!);
}

/** Chi tiết không tồn tại: 404 `CORE.USER.NOT_FOUND` (users.md §4) — màn hiện `record-not-found`. */
export function chiTietNguoiDungKhongTimThay(id: string): CaGia {
  return {
    phuongThuc: 'GET',
    duongDan: `${DUONG_DAN}/${id}`,
    phanHoi: thatBai('CORE.USER.NOT_FOUND', { type: 'NotFound', status: 404 }),
    loiCoChuY: true,
  };
}

/** Lỗi tải chi tiết có chủ ý: 500 `CORE.SYSTEM.UNEXPECTED`. */
export function chiTietNguoiDungLoiTai(id: string): CaGia {
  return {
    phuongThuc: 'GET',
    duongDan: `${DUONG_DAN}/${id}`,
    phanHoi: loiHeThongKhongLuong(),
    loiCoChuY: true,
  };
}
