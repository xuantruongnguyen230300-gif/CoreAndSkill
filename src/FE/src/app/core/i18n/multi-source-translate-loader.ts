import { HttpBackend, HttpClient } from '@angular/common/http';
import { ErrorHandler, Injectable, inject } from '@angular/core';
import { TranslateLoader, TranslationObject, mergeDeep } from '@ngx-translate/core';
import { Observable, catchError, forkJoin, map, of, tap, throwError } from 'rxjs';

import { CORE_I18N } from '../config/core-i18n';

/**
 * Một tệp dịch của một tầng không nạp được (thiếu, hoặc trả thân không phải JSON — SPA fallback trả
 * `index.html` 200 cho đường dẫn không có tệp). `cause` giữ lỗi HTTP gốc.
 */
export class LoiNapTepDichError extends Error {
  constructor(
    readonly url: string,
    readonly lang: string,
    cause: unknown,
  ) {
    super(`Không nạp được tệp dịch "${url}" (ngôn ngữ "${lang}")`, { cause });
    this.name = 'LoiNapTepDichError';
  }
}

/**
 * Nạp bản dịch từ NHIỀU nguồn theo thứ tự khai ở `CORE_I18N.sources`, tầng sau ghi đè khoá trùng
 * của tầng trước (08-i18n.md §2.3). Dùng `HttpBackend`, KHÔNG dùng `HttpClient` của DI —
 * bộ nạp tệp tĩnh phải đi vòng qua toàn bộ chuỗi interceptor (fe-api-client.md §2.1).
 *
 * Thiếu tệp của BẤT KỲ tầng nào đã khai thì cả lần nạp hỏng — không gộp phần đã về, vì bản dịch
 * thiếu tầng dự án còn tệ hơn không có (khoá của dự án hiện nguyên chuỗi khoá). Văn bản §2.3 không
 * phân biệt tầng Core với tầng dự án. Lỗi mang tên tệp và được báo qua `ErrorHandler` rồi ném
 * tiếp: `@ngx-translate/core` tự nuốt lỗi của loader thành một `console.warn`.
 */
@Injectable()
export class MultiSourceTranslateLoader extends TranslateLoader {
  private readonly http = new HttpClient(inject(HttpBackend));
  private readonly i18n = inject(CORE_I18N);
  private readonly errorHandler = inject(ErrorHandler);

  getTranslation(lang: string): Observable<TranslationObject> {
    if (this.i18n.sources.length === 0) {
      return of({});
    }
    const requests = this.i18n.sources.map((source) => {
      const url = `${source}${lang}.json`;
      return this.http
        .get<TranslationObject>(url)
        .pipe(
          catchError((err: unknown) => throwError(() => new LoiNapTepDichError(url, lang, err))),
        );
    });
    return forkJoin(requests).pipe(
      tap({ error: (err: unknown) => this.errorHandler.handleError(err) }),
      map((parts) => parts.reduce<TranslationObject>((acc, part) => mergeDeep(acc, part), {})),
    );
  }
}
