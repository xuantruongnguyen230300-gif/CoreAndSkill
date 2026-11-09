// Dữ liệu giả lúc app khởi động — chỉ phần ca khói `01-dang-nhap.spec.ts` cần. Card nguồn:
// docs/contracts/auth.md §2 (`GET /core/antiforgery/token`) và §5 (`GET /core/auth/me`, 401 lúc chưa đăng nhập).
// F2 (frontend-expert) mở rộng tệp này; envelope và mã lỗi chỉ dựng qua `ha-tang/envelope`.
import type { CaGia } from '../ha-tang/api-gia';
import { thanhCong, thatBai } from '../ha-tang/envelope';

export const tokenXsrf: CaGia = {
  phuongThuc: 'GET',
  duongDan: '/core/antiforgery/token',
  phanHoi: thanhCong({ token: 'xsrf-gia' }),
};

/** `me` lúc chưa đăng nhập: 401 là câu trả lời BÌNH THƯỜNG của lần khởi động — lỗi có chủ ý. */
export const meChuaDangNhap: CaGia = {
  phuongThuc: 'GET',
  duongDan: '/core/auth/me',
  phanHoi: thatBai('CORE.AUTH.NOT_AUTHENTICATED', { type: 'Unauthorized', status: 401 }),
  loiCoChuY: true,
};

/**
 * `POST /core/client-errors` (docs/contracts/client-errors.md, Response 200). Bản `production` — và
 * `environment.e2e.ts` mang `production: true` — gửi báo cáo lỗi FE tới đây khi đã có token XSRF, nên
 * mọi kịch bản có `tokenXsrf` phải khai kèm ca này; thiếu thì lỗi của app (ví dụ chunk không tải được)
 * thành thêm một request "chưa khai báo" và lưới đỏ vì lý do khác lỗi gốc.
 */
export const baoLoiClient: CaGia = {
  phuongThuc: 'POST',
  duongDan: '/core/client-errors',
  phanHoi: thanhCong(null),
};
