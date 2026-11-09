// Dữ liệu giả của màn hồ sơ cá nhân. Card nguồn: docs/contracts/profile.md §1 (`GET /core/profile`).
import type { HoSoDto } from '../../src/app/platform/ho-so/models/ho-so.dto';
import type { CaGia } from '../ha-tang/api-gia';
import { thanhCong } from '../ha-tang/envelope';
import { loiHeThongKhongLuong } from './chung';

const DUONG_DAN = '/core/profile';
const VERSION = '9d3b4c7e-2f10-4a8b-b1c5-6e7f8a9b0c1d';

function caHoSo(dto: HoSoDto): CaGia {
  return { phuongThuc: 'GET', duongDan: DUONG_DAN, phanHoi: thanhCong(dto) };
}

/** Hồ sơ đủ trường, không mang cờ đặc quyền. Khớp `phienQuanTriDonVi` ở `phien.ts`. */
export function hoSoDayDu(): CaGia {
  return caHoSo({
    userName: 'an.nv',
    email: 'an.nv@don-vi-mau.example',
    fullName: 'Nguyễn Văn An',
    phoneNumber: '0912345678',
    preferredLanguage: 'vi',
    hasPermissionBypass: false,
    version: VERSION,
  });
}

/** Hồ sơ chưa đặt email, số điện thoại, ngôn ngữ — ba trường `string | null` đều `null`. */
export function hoSoTrongTruong(): CaGia {
  return caHoSo({
    userName: 'an.nv',
    email: null,
    fullName: 'Nguyễn Văn An',
    phoneNumber: null,
    preferredLanguage: null,
    hasPermissionBypass: false,
    version: VERSION,
  });
}

/** Hồ sơ mang cờ `has_permission_bypass` — màn hiện thao tác từ bỏ cờ (profile.md §1, §3). */
export function hoSoMangCoDacQuyen(): CaGia {
  return caHoSo({
    userName: 'an.nv',
    email: 'an.nv@don-vi-mau.example',
    fullName: 'Nguyễn Văn An',
    phoneNumber: '0912345678',
    preferredLanguage: 'vi',
    hasPermissionBypass: true,
    version: VERSION,
  });
}

/** Hồ sơ có họ tên, tên đăng nhập và email rất dài — thử tràn ô và tiêu đề. */
export function hoSoChuoiDai(): CaGia {
  return caHoSo({
    userName: 'ten.dang.nhap.rat.dai.de.thu.tran.o.nhap.lieu',
    email:
      'nguyen.thi.thuy.duong.phuong.anh.bao.chau.minh.khue.thanh.tam@phong-chuyen-mon-rat-dai.don-vi-mau.example',
    fullName:
      'Nguyễn Thị Thuỳ Dương Phương Anh Bảo Châu Minh Khuê Thanh Tâm Hồng Ngọc Diễm Quỳnh Như Ý Lan Chi Hoa',
    phoneNumber: '0912345678',
    preferredLanguage: 'vi',
    hasPermissionBypass: false,
    version: VERSION,
  });
}

/** Lỗi tải hồ sơ có chủ ý: 500 `CORE.SYSTEM.UNEXPECTED`. */
export function hoSoLoiTai(): CaGia {
  return {
    phuongThuc: 'GET',
    duongDan: DUONG_DAN,
    phanHoi: loiHeThongKhongLuong(),
    loiCoChuY: true,
  };
}
