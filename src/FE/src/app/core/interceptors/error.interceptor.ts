import {
  HttpContext,
  HttpContextToken,
  HttpErrorResponse,
  HttpEvent,
  HttpInterceptorFn,
  HttpRequest,
} from '@angular/common/http';
import { inject } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { Observable, catchError, from, switchMap, throwError } from 'rxjs';

import { AuthService } from '../auth/auth.service';
import { SessionExpiryHandler } from '../auth/session-expiry.handler';
import { XsrfTokenStore } from '../auth/xsrf-token.store';
import { ToastService } from '../toast/toast.service';
import {
  ApiFailureError,
  BO_QUA_HET_PHIEN,
  BO_QUA_TOAST_LOI,
  CO_TAC_DUNG_PHU,
  docEnvelopeLoi,
} from '../http/api-result.model';
import { dichLoi, thamSoRetryAfter } from '../http/dich-loi';
import { laLoiXuyenSuot } from '../http/loi-xuyen-suot';

/** Đánh dấu request đã được gửi lại một lần sau CSRF_REJECTED — chặn vòng lặp. */
const DA_THU_LAI_XSRF = new HttpContextToken<boolean>(() => false);

/** Phương thức BE không kiểm CSRF (AntiforgeryValidationMiddleware bỏ qua) — không có gì để "lấy token rồi gửi lại". */
const LENH_AN_TOAN: ReadonlySet<string> = new Set(['GET', 'HEAD', 'OPTIONS', 'TRACE']);

/**
 * `HttpContext.set()` sửa map TẠI CHỖ, mà một service singleton thường giữ MỘT context dùng chung
 * cho mọi lệnh ghi — gắn cờ thẳng vào nó thì cờ nằm lại vĩnh viễn và những lần CSRF hết hạn sau
 * không còn được thử lại. Luôn dựng bản sao, chép các token đang có, rồi mới gắn cờ.
 */
function ctxDaThuLaiXsrf(goc: HttpContext): HttpContext {
  const ban = new HttpContext();
  for (const token of goc.keys()) {
    ban.set(token, goc.get(token));
  }
  return ban.set(DA_THU_LAI_XSRF, true);
}

/**
 * Với `responseType: 'blob'` (tải tệp xuất — fe-api-client.md §6.4), Angular giao MỌI thân phản hồi
 * dưới dạng `Blob`, kể cả thân của lỗi 4xx/5xx dù server gửi `application/json`. `docEnvelopeLoi`
 * kiểm `'success' in body` trên một `Blob` ⇒ `false` ⇒ `null` ⇒ người dùng nhận câu *mất kết nối*
 * cho một lỗi 422 có mã và có thông điệp nói rõ phải làm gì.
 *
 * Bóc ở ĐÂY, một lần, trước mọi nhánh — không ở từng service export. `Blob.text()` bất đồng bộ nên
 * lời gọi này trả Promise và nhánh gọi nó phải là `from(...).pipe(switchMap(...))`.
 * Thân không phải JSON (proxy trả HTML, tệp rỗng) ⇒ `error` là `null` và nhánh dưới xử lý y như
 * một phản hồi không phải envelope.
 */
async function bocThanBlob(err: HttpErrorResponse): Promise<HttpErrorResponse> {
  let than: unknown = null;
  try {
    const chu = await (err.error as Blob).text();
    than = chu === '' ? null : JSON.parse(chu);
  } catch {
    than = null;
  }
  return new HttpErrorResponse({
    error: than,
    headers: err.headers,
    status: err.status,
    statusText: err.statusText,
    url: err.url ?? undefined,
  });
}

/** Nơi DUY NHẤT dịch lỗi HTTP. Page không `catchError`, không `try/catch` (fe-api-client.md §3). */
export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  // inject() gọi Ở ĐÂY — callback của catchError KHÔNG phải injection context (bẫy §5.2, 02-http-envelope.md).
  const toast = inject(ToastService);
  const translate = inject(TranslateService);
  const auth = inject(AuthService);
  const hetPhien = inject(SessionExpiryHandler);
  const xsrf = inject(XsrfTokenStore);

  /**
   * Bộ xử lý lỗi cho MỘT request. Dựng theo request để lần gửi lại sau CSRF_REJECTED dùng lại đúng
   * bộ này trên request đã clone (mang cờ `DA_THU_LAI_XSRF` và context của nó).
   */
  const xuLyLoi =
    (yeuCau: HttpRequest<unknown>) =>
    (err: HttpErrorResponse): Observable<HttpEvent<unknown>> => {
      // Thân lỗi là Blob (request tải tệp xuất) — bóc envelope ra TRƯỚC khi dịch, rồi chạy lại
      // chính bộ xử lý này trên phản hồi đã bóc. Không nhánh nào dưới đây phải biết về Blob.
      if (err.error instanceof Blob) {
        return from(bocThanBlob(err)).pipe(switchMap((daBoc) => xuLyLoi(yeuCau)(daBoc)));
      }

      const body = docEnvelopeLoi(err);
      // Mọi nhánh trả lỗi về người gọi qua đây — mang status thật để màn nhận diện lớp xuyên suốt.
      const nem = (): Observable<never> => throwError(() => new ApiFailureError(body, err.status));

      // 403 CSRF — lấy token mới rồi gửi lại ĐÚNG MỘT lần; cờ trên context chặn vòng lặp.
      // CORE.AUTH.ORIGIN_REJECTED KHÔNG vào nhánh này. Chỉ LỆNH GHI mới được thử lại: BE bỏ qua kiểm
      // CSRF cho GET/HEAD/OPTIONS, và chính `xsrf.lamMoi()` là một GET đi qua interceptor này — cho GET
      // vào nhánh thì GET token bị CSRF_REJECTED sẽ gọi lại `lamMoi()` mãi (cờ trên context không phủ nó).
      // Ngoại lệ có tên: GET mang dấu CO_TAC_DUNG_PHU (tải tệp xuất) — BE kiểm token cho nó như lệnh
      // ghi, nên nó cũng được gửi lại một lần. `lamMoi()` KHÔNG mang dấu nên đường đệ quy vẫn đóng.
      const duocGuiLai = !LENH_AN_TOAN.has(yeuCau.method) || yeuCau.context.get(CO_TAC_DUNG_PHU);
      if (
        body?.error?.code === 'CORE.AUTH.CSRF_REJECTED' &&
        duocGuiLai &&
        !yeuCau.context.get(DA_THU_LAI_XSRF)
      ) {
        // Lỗi của CHÍNH lần lấy token được dịch ở lượt GET đó — mang cờ tắt toast của request gốc
        // theo, để màn tự hiện lỗi không bị toast chồng (fe-ui-conventions.md §6.2).
        return xsrf.lamMoi({ boQuaToastLoi: yeuCau.context.get(BO_QUA_TOAST_LOI) }).pipe(
          switchMap((token) => {
            const guiLai = yeuCau.clone({
              setHeaders: { 'X-XSRF-TOKEN': token },
              context: ctxDaThuLaiXsrf(yeuCau.context),
            });
            // catchError KHÔNG bắt lỗi của observable do CALLBACK của chính nó trả về — nên lần gửi lại
            // phải được bọc lại bằng cùng bộ xử lý (đã mang cờ DA_THU_LAI_XSRF ⇒ không có lần gửi thứ ba).
            return next(guiLai).pipe(catchError(xuLyLoi(guiLai)));
          }),
        );
      }

      // 401 — phiên chết. Không toast. Request mang BO_QUA_HET_PHIEN tự xử lý (fe-api-client.md §2.5).
      if (err.status === 401) {
        if (!yeuCau.context.get(BO_QUA_HET_PHIEN)) {
          hetPhien.handle();
        }
        return nem();
      }

      // 403 CORE.AUTH.FORBIDDEN — làm mới quyền và menu, rồi xuống nhánh lớp xuyên suốt. KHÔNG điều hướng.
      if (err.status === 403 && body?.error.code === 'CORE.AUTH.FORBIDDEN') {
        auth.lamMoiQuyen();
      } else if (err.status === 403 && body?.error.code === 'CORE.AUTH.PASSWORD_CHANGE_REQUIRED') {
        // Cờ buộc đổi mật khẩu bật giữa phiên: làm mới phiên, guard lo phần còn lại (F2). KHÔNG điều hướng, KHÔNG toast.
        void auth.lamMoiPhien();
        return nem();
      } else if (err.status === 403 && body !== null && !body.error.code.startsWith('CORE.AUTH.')) {
        // 403 mang mã nghiệp vụ — màn tự xử lý theo card. Không toast chung, không làm mới quyền.
        return nem();
      }

      // Lớp xuyên suốt (5xx, 403 CORE.AUTH.* còn lại) — LUÔN toast kèm traceId, KHÔNG xét
      // BO_QUA_TOAST_LOI: khu lỗi của màn bỏ qua lớp này (`laLoiXuyenSuot`), nên toast là chỗ hiện duy nhất.
      if (laLoiXuyenSuot(err.status, body)) {
        if (body === null) {
          // 5xx không envelope (proxy trả 502/504 dạng HTML): máy chủ CÓ trả lời nên không phải câu mất
          // kết nối; không mã, không traceId — câu của mã phía client `CORE.CLIENT.SERVER_UNAVAILABLE`
          // (be-cqrs-handler.md §7.4). 403 không envelope không vào lớp này, nên `null` ở đây luôn là 5xx.
          toast.loi(translate.instant('loi.CORE.CLIENT.SERVER_UNAVAILABLE'), null);
        } else {
          toast.loi(dichLoi(translate, body), body.traceId);
        }
        return nem();
      }

      // 429 — siết tần suất. Số giây chờ đọc từ Retry-After.
      if (err.status === 429) {
        if (!yeuCau.context.get(BO_QUA_TOAST_LOI)) {
          toast.loi(dichLoi(translate, body, thamSoRetryAfter(err)), body?.traceId ?? null);
        }
        return nem();
      }

      // 409 CORE.CONCURRENCY.CONFLICT — người khác đã ghi sau khi màn đọc.
      // Toast rồi trả lỗi về màn. KHÔNG gửi lại, KHÔNG tải lại hộ.
      if (err.status === 409 && body?.error.code === 'CORE.CONCURRENCY.CONFLICT') {
        if (!yeuCau.context.get(BO_QUA_TOAST_LOI)) {
          toast.loi(dichLoi(translate, body), body?.traceId ?? null);
        }
        return nem();
      }

      // 400/409/422 kèm fieldErrors — lỗi thuộc về form, KHÔNG toast. Trường thật ở body.error.fieldErrors.
      if (body?.error?.fieldErrors) {
        return nem();
      }

      // Màn tự hiển thị lỗi của mình thì tắt toast cho ĐÚNG request đó.
      if (!yeuCau.context.get(BO_QUA_TOAST_LOI)) {
        toast.loi(dichLoi(translate, body), body?.traceId ?? null);
      }
      return nem();
    };

  return next(req).pipe(catchError(xuLyLoi(req)));
};
