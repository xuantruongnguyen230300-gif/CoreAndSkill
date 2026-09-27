import { HttpBackend, HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { TranslateLoader, TranslationObject, mergeDeep } from '@ngx-translate/core';
import { Observable, forkJoin, map, of } from 'rxjs';

import { CORE_I18N } from '../config/core-i18n';

/**
 * Nạp bản dịch từ NHIỀU nguồn theo thứ tự khai ở `CORE_I18N.sources`, tầng sau ghi đè khoá trùng
 * của tầng trước (08-i18n.md §2.3). Dùng `HttpBackend`, KHÔNG dùng `HttpClient` của DI —
 * bộ nạp tệp tĩnh phải đi vòng qua toàn bộ chuỗi interceptor (fe-api-client.md §2.1).
 */
@Injectable()
export class MultiSourceTranslateLoader extends TranslateLoader {
  private readonly http = new HttpClient(inject(HttpBackend));
  private readonly i18n = inject(CORE_I18N);

  getTranslation(lang: string): Observable<TranslationObject> {
    if (this.i18n.sources.length === 0) {
      return of({});
    }
    const requests = this.i18n.sources.map((source) =>
      this.http.get<TranslationObject>(`${source}${lang}.json`),
    );
    return forkJoin(requests).pipe(
      map((parts) => parts.reduce<TranslationObject>((acc, part) => mergeDeep(acc, part), {})),
    );
  }
}
