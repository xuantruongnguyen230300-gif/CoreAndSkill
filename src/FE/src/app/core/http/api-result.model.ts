import { HttpContextToken, HttpErrorResponse } from '@angular/common/http';

/** Mã lỗi nghiệp vụ do BE khai trong catalog. Chuỗi ổn định, KHÔNG dịch, KHÔNG hiển thị. */
export type BusinessCode = string;

/** Tham số của thông điệp, truyền theo TÊN. Khoá giữ nguyên như BE gửi. */
export type MessageParams = Readonly<Record<string, string>>;

/** Một lỗi của một ô nhập. `code` là khoá dịch, KHÔNG phải câu hiển thị. */
export interface ApiFieldError {
  readonly code: BusinessCode;
  readonly messageParams: MessageParams | null;
}

export interface ApiError {
  /** Mã nghiệp vụ để FE ra quyết định và tra bảng dịch. */
  readonly code: BusinessCode;

  /** Loại lỗi do BE khai. DEV-FACING — KHÔNG rẽ nhánh theo trường này; rẽ theo mã HTTP, và theo `code` khi cần. */
  readonly type: string;

  /** Câu mặc định của BE, dev-facing — chỉ dùng làm đường lùi khi FE chưa có bản dịch. */
  readonly message: string;

  readonly messageParams: MessageParams | null;

  /** Lỗi theo từng ô nhập. Khoá là tên property phía BE; giá trị là DANH SÁCH MÃ LỖI. */
  readonly fieldErrors: Readonly<Record<string, readonly ApiFieldError[]>> | null;
}

export interface ApiSuccess<T> {
  readonly success: true;
  readonly data: T;
  readonly error: null;
  readonly traceId: string;
}

export interface ApiFailure {
  readonly success: false;
  readonly data: null;
  readonly error: ApiError;
  readonly traceId: string;
}

export type ApiResult<T> = ApiSuccess<T> | ApiFailure;

/** Đọc envelope ra khỏi lỗi HTTP. `null` là BÌNH THƯỜNG (mất mạng, proxy trả HTML). Nhận diện bằng tên field CÓ THẬT trên dây. */
export function docEnvelopeLoi(err: HttpErrorResponse): ApiFailure | null {
  const body: unknown = err.error;
  return body && typeof body === 'object' && 'success' in body && 'traceId' in body
    ? (body as ApiFailure)
    : null;
}

/**
 * Cờ tắt toast mặc định cho ĐÚNG một request — không phải cờ toàn cục. KHÔNG tắt được toast của lớp
 * lỗi xuyên suốt (`laLoiXuyenSuot`: 5xx, 403 `CORE.AUTH.*` trừ `PASSWORD_CHANGE_REQUIRED`) — lớp đó
 * luôn toast kèm `traceId` (ADR-0094).
 */
export const BO_QUA_TOAST_LOI = new HttpContextToken<boolean>(() => false);

/** Cờ cho request mà 401 là câu trả lời BÌNH THƯỜNG, không phải hết phiên — dùng ở `me` lúc khởi động và `logout`. */
export const BO_QUA_HET_PHIEN = new HttpContextToken<boolean>(() => false);

/**
 * Dấu tường minh: request này CÓ tác dụng phụ phía máy chủ dù mang phương thức an toàn — hôm nay
 * chỉ endpoint xuất, nó ghi một dòng nhật ký kiểm toán nên BE kiểm `X-XSRF-TOKEN` như lệnh ghi
 * (`[RequireAntiforgery]`, ADR-0062). Service gọi export đặt cờ này; `authInterceptor` gắn token
 * (fe-api-client.md §2.1 ràng buộc 3) và `errorInterceptor` gửi lại đúng một lần khi CSRF_REJECTED
 * (§2.2). KHÔNG tự đặt header trong service, và KHÔNG gắn token cho mọi GET: interceptor lỗi sẽ
 * không còn phân biệt được GET nào được phép gửi lại (§6.4).
 */
export const CO_TAC_DUNG_PHU = new HttpContextToken<boolean>(() => false);

/**
 * Lỗi ném ra khi envelope báo thất bại; mang envelope ĐÃ BÓC. `body` là `null` khi phản hồi không
 * phải envelope. `status` là HTTP status thật của phản hồi lỗi — `errorInterceptor` luôn điền; `0` khi
 * không có phản hồi (mất mạng) hoặc khi lỗi không đến từ một phản hồi HTTP lỗi (`unwrapData`).
 * Màn rẽ nhánh theo `code`; `status` để nhận diện lớp lỗi xuyên suốt (`laLoiXuyenSuot`).
 */
export class ApiFailureError extends Error {
  constructor(
    readonly body: ApiFailure | null,
    readonly status = 0,
  ) {
    // Mã lấy từ danh mục mã phía client ở be-cqrs-handler.md §7.4 — FE KHÔNG tự chế mã.
    super(body?.error.code ?? 'CORE.CLIENT.NO_CONNECTION');
  }
}
