import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';

import { XsrfTokenStore } from '../auth/xsrf-token.store';
import { API_BASE_URL } from '../http/api-base-url';
import { CO_TAC_DUNG_PHU } from '../http/api-result.model';

const GHI = new Set(['POST', 'PUT', 'PATCH', 'DELETE']);

/** Ghép base URL và cookie phiên vào mọi request tương đối; gắn header XSRF cho request ghi. */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const base = inject(API_BASE_URL);
  const xsrf = inject(XsrfTokenStore);

  // URL tuyệt đối đi thẳng: không ghép base URL, không kèm cookie, KHÔNG mang token.
  if (req.url.startsWith('http')) {
    return next(req);
  }

  // Lệnh ghi, CỘNG đúng một ngoại lệ có tên: request được đánh dấu tường minh là có tác dụng phụ
  // (hôm nay chỉ tải tệp xuất — nó ghi nhật ký kiểm toán nên BE kiểm token như lệnh ghi, ADR-0062).
  // Vẫn KHÔNG gắn cho mọi GET: errorInterceptor sẽ không phân biệt được GET nào được gửi lại.
  const canToken = GHI.has(req.method) || req.context.get(CO_TAC_DUNG_PHU);
  const token = canToken ? xsrf.token() : null;
  return next(
    req.clone({
      url: `${base}${req.url}`,
      withCredentials: true,
      setHeaders: token ? { 'X-XSRF-TOKEN': token } : {},
    }),
  );
};
