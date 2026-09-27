import { EnvironmentProviders, InjectionToken, makeEnvironmentProviders } from '@angular/core';
import type { NguoiDung } from '../quan-tri/nguoi-dung/models/nguoi-dung.model';
import type { VaiTro } from '../quan-tri/vai-tro/models/vai-tro.model';
import type { DonVi } from '../he-thong/don-vi/models/don-vi.model';

/**
 * Seam cho MÀN Core — hợp đồng ở fe-architecture.md §2.7 (ADR-0057). KHÔNG có giá trị mặc định, và
 * dự án không bắt buộc khai: màn Core đọc bằng `inject(CORE_SCREEN_EXT, { optional: true })?.<mã màn>`,
 * `null` hoặc thiếu khoá của mình ⇒ chạy đúng bản gốc. Đặt ở `platform/config/`, không ở `core/`,
 * vì kiểu dòng của từng khoá là model của màn `platform/` (luật F1).
 *
 * Mọi giá trị đi qua seam phải DỰNG ĐƯỢC ở composition root: không `TemplateRef`, không chuỗi đã
 * dịch — tiêu đề là KHOÁ i18n, ô là `value(row)` trả chuỗi. Hành động dòng, trường lọc, nút riêng
 * trên Toolbar chưa có điểm mở rộng — thêm bằng ADR mới khi dự án hạ nguồn đầu tiên cần.
 */

/** Một cột dự án THÊM vào màn danh sách Core. Khai ở composition root — không có view, nên không TemplateRef. */
export interface ScreenExtColumn<T> {
  /** Duy nhất trong màn, không trùng khoá cột của Core. */
  readonly key: string;
  /** KHOÁ i18n — màn Core dịch khi dựng cột; không phải chuỗi đã dịch. */
  readonly headerKey: string;
  /** Chuỗi ĐÃ định dạng; đọc signal thì ô tự cập nhật. */
  readonly value: (row: T) => string;
  readonly width?: string;
  readonly align?: 'start' | 'end';
  readonly hideBelow?: 'xs' | 'sm' | 'md' | 'lg';
  readonly priority?: 'high' | 'low';
} // không `sortable`: sortBy chỉ nhận allowlist của endpoint Core

/** Phần một dự án được phép THÊM vào một màn Core. Không có gì cho phép BỚT. */
export interface ScreenExtension<T> {
  /** Nối vào SAU cột của Core. */
  readonly columns?: readonly ScreenExtColumn<T>[];
}

/** Khoá là mã MÀN DANH SÁCH Core, kiểu dòng theo từng màn — khoá gõ sai là lỗi biên dịch. */
export interface CoreScreenExtensions {
  readonly users?: ScreenExtension<NguoiDung>;
  readonly roles?: ScreenExtension<VaiTro>;
  readonly tenants?: ScreenExtension<DonVi>;
}

export const CORE_SCREEN_EXT = new InjectionToken<CoreScreenExtensions>('CORE_SCREEN_EXT');

/** `factory` chạy trong ngữ cảnh tiêm — `value(row)` đọc được service của dự án. */
export function provideCoreScreenExt(factory: () => CoreScreenExtensions): EnvironmentProviders {
  return makeEnvironmentProviders([{ provide: CORE_SCREEN_EXT, useFactory: factory }]);
}
