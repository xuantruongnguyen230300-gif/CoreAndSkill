import { ApiFailure } from './api-result.model';

/** Mã 403 `CORE.AUTH.*` có nhánh riêng ở `errorInterceptor` — không thuộc lớp xuyên suốt. */
const MA_AUTH_CO_NHANH_RIENG = 'CORE.AUTH.PASSWORD_CHANGE_REQUIRED';

/**
 * Nhận diện LỚP LỖI XUYÊN SUỐT — hàm DUY NHẤT trả lời câu hỏi này, cho cả `errorInterceptor` lẫn các
 * hàm dựng câu cho khu lỗi của màn:
 *
 * - mọi status 5xx, có envelope hay không;
 * - 403 mang mã `CORE.AUTH.*`, trừ `CORE.AUTH.PASSWORD_CHANGE_REQUIRED`.
 *
 * Lớp này luôn được `errorInterceptor` toast kèm `traceId`, kể cả khi request mang `BO_QUA_TOAST_LOI`;
 * vì thế khu lỗi của màn KHÔNG hiện nó (hàm dựng câu trả `null`) — một lỗi, một chỗ hiện.
 * 403 không có envelope không thuộc lớp này: không có mã để biết đó là `CORE.AUTH.*`.
 */
export function laLoiXuyenSuot(status: number, body: ApiFailure | null): boolean {
  if (status >= 500 && status <= 599) {
    return true;
  }
  const ma = body?.error.code;
  return (
    status === 403 &&
    ma !== undefined &&
    ma.startsWith('CORE.AUTH.') &&
    ma !== MA_AUTH_CO_NHANH_RIENG
  );
}
