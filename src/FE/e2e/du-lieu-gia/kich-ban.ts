// Bộ ca khởi động theo kịch bản — gom những thứ MỌI màn sau đăng nhập đều cần (token XSRF, `me`, menu)
// để spec không phải nhớ từng ca. Dùng: `apiGia.khaiBao(...kichBanQuanTriDonVi(), danhSachNguoiDungCoDuLieu())`.
// Ca khai sau thay ca khai trước cùng phương thức + đường dẫn, nên spec đổi một ca chỉ cần khai lại nó.
import type { CaGia } from '../ha-tang/api-gia';
import { baoLoiClient, tokenXsrf } from './khoi-dong';
import {
  menuQuanTriDonVi,
  menuVanHanhHeThong,
  meQuanTriBatBuocDoiMatKhau,
  meQuanTriDonVi,
  meVanHanhBatBuocDoiMatKhau,
  meVanHanhHeThong,
} from './phien';

/** Quản trị đơn vị nghiệp vụ đã đăng nhập: token XSRF, `me` 200, menu đủ ba mục quản trị. */
export function kichBanQuanTriDonVi(): readonly CaGia[] {
  return [tokenXsrf, baoLoiClient, meQuanTriDonVi(), menuQuanTriDonVi()];
}

/** Tài khoản vận hành hệ thống đã đăng nhập: token XSRF, `me` 200 (`isSystemOperator`), menu chỉ có trang chủ. */
export function kichBanVanHanhHeThong(): readonly CaGia[] {
  return [tokenXsrf, baoLoiClient, meVanHanhHeThong(), menuVanHanhHeThong()];
}

/**
 * Phiên mang mật khẩu tạm của quản trị đơn vị: token XSRF và `me` với `mustChangePassword = true`.
 * Không có menu: mọi endpoint có danh tính trừ bốn đường ở auth.md §1.2 trả 403 `PASSWORD_CHANGE_REQUIRED`.
 */
export function kichBanQuanTriBatBuocDoiMatKhau(): readonly CaGia[] {
  return [tokenXsrf, baoLoiClient, meQuanTriBatBuocDoiMatKhau()];
}

/** Như trên, cho tài khoản vận hành hệ thống. */
export function kichBanVanHanhBatBuocDoiMatKhau(): readonly CaGia[] {
  return [tokenXsrf, baoLoiClient, meVanHanhBatBuocDoiMatKhau()];
}
