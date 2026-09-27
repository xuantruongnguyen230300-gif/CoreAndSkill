import { Injectable, inject } from '@angular/core';
import { RouterStateSnapshot, TitleStrategy } from '@angular/router';
import { Title } from '@angular/platform-browser';
import { TranslateService } from '@ngx-translate/core';
import { Subscription } from 'rxjs';

import { CORE_BRANDING } from '../config/core-branding';

/**
 * `title` của route giữ KHOÁ i18n, không câu — dịch rồi nối hậu tố ` · ` + tên sản phẩm
 * (fe-routing-guard.md §2.2). Route không khai `title` ⇒ chỉ còn tên sản phẩm.
 *
 * Dịch bằng `stream`, không `instant`: khởi động không chờ tệp dịch (fe-routing-guard.md §3.6),
 * nên điều hướng đầu tiên có thể xong trước khi bảng dịch về — `instant` lúc đó trả khoá thô và
 * tiêu đề kẹt ở khoá. `stream` phát lại khi bảng dịch về hoặc đổi ngôn ngữ. Mỗi lần điều hướng
 * huỷ luồng của tuyến trước.
 *
 * Đăng ký ở app.config.ts: `{ provide: TitleStrategy, useClass: CoreTitleStrategy }`.
 */
@Injectable({ providedIn: 'root' })
export class CoreTitleStrategy extends TitleStrategy {
  private readonly title = inject(Title);
  private readonly translate = inject(TranslateService);
  private readonly branding = inject(CORE_BRANDING);
  private dangDich: Subscription | null = null;

  override updateTitle(snapshot: RouterStateSnapshot): void {
    this.dangDich?.unsubscribe();
    this.dangDich = null;
    const khoa = this.buildTitle(snapshot);
    if (!khoa) {
      this.title.setTitle(this.branding.name);
      return;
    }
    this.dangDich = this.translate.stream(khoa).subscribe((cau: string) => {
      this.title.setTitle(cau ? `${cau} · ${this.branding.name}` : this.branding.name);
    });
  }
}
