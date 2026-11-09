import { DOCUMENT } from '@angular/common';
import { DestroyRef, inject } from '@angular/core';

import { ClientErrorReporter } from './client-error-reporter';

/**
 * Lỗi tải tài nguyên (ảnh, script, stylesheet…). Dựng thành lớp riêng để báo cáo mang `kind` rõ ràng
 * và KHÔNG mang `stack` — ngăn xếp của một sự kiện tải hỏng chỉ trỏ vào chính bộ nghe này.
 */
export class ResourceLoadError extends Error {
  constructor(message: string) {
    super(message);
    this.name = 'ResourceLoadError';
    this.stack = undefined;
  }
}

/**
 * URL của tài nguyên, KHÔNG query/fragment (query có thể mang giá trị người dùng — client-errors.md §1)
 * và KHÔNG nội dung của `data:`/`blob:` (có thể là cả một tệp người dùng chọn): với các scheme đó chỉ
 * giữ tên scheme.
 */
function urlAnToan(url: string, goc: string): string {
  try {
    const u = new URL(url, goc);
    return u.protocol === 'http:' || u.protocol === 'https:' ? u.origin + u.pathname : u.protocol;
  } catch {
    return '(url không đọc được)';
  }
}

function tenTaiNguyen(dich: Element): string {
  const url = dich.getAttribute('src') ?? dich.getAttribute('href') ?? '';
  const goc = dich.ownerDocument.baseURI;
  return `Không tải được tài nguyên <${dich.tagName.toLowerCase()}> ${urlAnToan(url, goc)}`;
}

/**
 * Nguồn lỗi thứ ba của 10-observability.md §3: tài nguyên tải hỏng. Sự kiện `error` của phần tử KHÔNG
 * nổi bọt lên `window` nên bộ nghe thường không thấy — phải nghe ở giai đoạn capture. Bộ nghe này chỉ
 * nhận sự kiện có đích là PHẦN TỬ; lỗi script thường (đích là `window`) đã đến `ErrorHandler` qua
 * `provideBrowserGlobalErrorListeners()`, nhận ở đây là báo hai lần.
 *
 * Gọi trong ngữ cảnh tiêm (initializer của môi trường). Bộ nghe gỡ khi injector bị huỷ.
 */
export function nghePhanTuTaiHong(): void {
  const cuaSo = inject(DOCUMENT).defaultView;
  if (cuaSo === null) {
    return;
  }
  const reporter = inject(ClientErrorReporter);
  const nghe = (suKien: Event): void => {
    try {
      const dich = suKien.target;
      if (dich instanceof Element) {
        reporter.capture(new ResourceLoadError(tenTaiNguyen(dich)));
      }
    } catch {
      // Bọc kín (§4.1 ràng buộc 3): bộ nghe sự kiện ném lỗi sẽ thành lỗi `window` — vòng lặp.
    }
  };
  cuaSo.addEventListener('error', nghe, true);
  inject(DestroyRef).onDestroy(() => cuaSo.removeEventListener('error', nghe, true));
}
