// ADR-0117 QC2, dòng "Envelope": envelope CHỈ dựng qua hai hàm này. Dữ liệu giả không tự viết
// `success` hay `traceId`. Kiểu lấy từ FE bằng `import type` — Playwright không nạp mã Angular.
import type { ApiError, ApiFailure, ApiSuccess } from '../../src/app/core/http/api-result.model';
import { kiemTraCoMa, kiemTraMaLoi } from './ma-loi-hop-dong';

export const TRACE_ID_GIA = '00000000000000000000000000000e2e';

export interface PhanHoi {
  readonly status: number;
  readonly body: ApiSuccess<unknown> | ApiFailure;
  /** Header thêm (ví dụ `Retry-After`); CORS do hạ tầng gắn. */
  readonly headers?: Readonly<Record<string, string>>;
}

export function thanhCong<T>(data: T, status = 200): PhanHoi {
  const body: ApiSuccess<T> = { success: true, data, error: null, traceId: TRACE_ID_GIA };
  return { status, body };
}

export interface TuyChonThatBai {
  /** Loại lỗi — đối chiếu bảng lỗi của card nếu card có khai. */
  readonly type: string;
  readonly status: number;
  readonly message?: string;
  readonly messageParams?: ApiError['messageParams'];
  readonly fieldErrors?: ApiError['fieldErrors'];
  readonly headers?: Readonly<Record<string, string>>;
}

export function thatBai(code: string, tuyChon: TuyChonThatBai): PhanHoi {
  kiemTraMaLoi(code, tuyChon.type, tuyChon.status);
  for (const [truong, loi] of Object.entries(tuyChon.fieldErrors ?? {})) {
    for (const l of loi) {
      try {
        kiemTraCoMa(l.code);
      } catch (e) {
        throw new Error(`fieldErrors["${truong}"]: ${(e as Error).message}`);
      }
    }
  }
  const body: ApiFailure = {
    success: false,
    data: null,
    error: {
      code,
      type: tuyChon.type,
      message: tuyChon.message ?? '',
      messageParams: tuyChon.messageParams ?? null,
      fieldErrors: tuyChon.fieldErrors ?? null,
    },
    traceId: TRACE_ID_GIA,
  };
  return { status: tuyChon.status, body, ...(tuyChon.headers ? { headers: tuyChon.headers } : {}) };
}
