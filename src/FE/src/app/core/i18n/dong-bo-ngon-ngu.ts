import {
  DOCUMENT,
  DestroyRef,
  EnvironmentProviders,
  Injectable,
  inject,
  provideEnvironmentInitializer,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslateService } from '@ngx-translate/core';
import { Translation } from 'primeng/api';
import { PrimeNG } from 'primeng/config';

import { CORE_I18N } from '../config/core-i18n';

/**
 * Miền khoá dịch giữ chuỗi của PrimeNG. Hình dạng của nhánh này TRÙNG `Translation` của PrimeNG
 * (tên khoá là tên của PrimeNG, kể cả nhánh con `aria`) — cùng lý do khoá lỗi giữ nguyên mã BE
 * (fe-ui-conventions.md §5.3): tra thẳng, không cần bảng chuyển đổi thứ hai.
 */
export const KHOA_THU_VIEN_UI = 'thuVienUi';

/**
 * Những thứ phải đổi theo ngôn ngữ mà không nằm trong template của app (wiki-core/fe/08-i18n.md
 * §7 bảng "dễ quên", §8):
 *   - ngôn ngữ ban đầu và ngôn ngữ dự phòng — đọc từ seam `CORE_I18N.defaultLanguage`, không viết
 *     cứng ở `app.config.ts`;
 *   - thuộc tính `lang` của `<html>` — trình đọc màn hình chọn giọng, trình duyệt gợi ý dịch theo nó;
 *   - bộ chuỗi của PrimeNG ("No results found", nhãn ARIA của Paginator…) — nạp từ nhánh
 *     `thuVienUi` của tệp dịch và nạp LẠI mỗi lần đổi ngôn ngữ, không sửa từng chuỗi ở nơi gọi.
 *
 * Nằm ở `core/i18n` — một trong hai chỗ của `core/` được phép chạm thư viện UI (allowlist F5,
 * wiki-core/fe/05-component-library.md §2.4).
 */
@Injectable({ providedIn: 'root' })
export class DongBoNgonNgu {
  private readonly translate = inject(TranslateService);
  private readonly primeng = inject(PrimeNG);
  private readonly document = inject(DOCUMENT);
  private readonly i18n = inject(CORE_I18N);
  private readonly destroyRef = inject(DestroyRef);

  batDau(): void {
    const macDinh = this.i18n.defaultLanguage;
    // Cả hai lệnh tự đăng ký nạp tệp dịch bên trong ngx-translate — không cần subscribe, và không
    // chặn khởi động chờ tệp dịch (fe-routing-guard.md §3.6).
    this.translate.setFallbackLang(macDinh);
    this.translate.use(macDinh);
    this.datLang(this.translate.getCurrentLang() ?? macDinh);

    this.translate.onLangChange
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((e) => this.datLang(e.lang));

    this.translate
      .stream(KHOA_THU_VIEN_UI)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((bo: unknown) => this.napChuoiThuVien(bo));
  }

  private datLang(ma: string): void {
    this.document.documentElement.lang = ma;
  }

  /**
   * `PrimeNG.setTranslation` gộp NÔNG (`{ ...cũ, ...mới }`): gửi thẳng `aria` một phần sẽ XOÁ mọi
   * nhãn aria không có trong bản dịch — nên nhánh `aria` gộp tay. Tệp dịch chưa về (hoặc thiếu nhánh)
   * thì `stream` trả lại chính chuỗi khoá — bỏ qua, giữ bộ chuỗi đang có.
   */
  private napChuoiThuVien(bo: unknown): void {
    if (typeof bo !== 'object' || bo === null) {
      return;
    }
    const moi = bo as Translation;
    this.primeng.setTranslation({
      ...moi,
      aria: { ...(this.primeng.translation.aria ?? {}), ...(moi.aria ?? {}) },
    });
  }
}

/** Đăng ký ở `app.config.ts`, sau `provideCoreI18n` và `provideTranslateService`. */
export function provideDongBoNgonNgu(): EnvironmentProviders {
  return provideEnvironmentInitializer(() => inject(DongBoNgonNgu).batDau());
}
