import type { DataColumnDef } from '../../shared/ui/data-table/data-table.component';
import type { CoreScreenExtensions, ScreenExtColumn } from './core-screen-ext';

/**
 * Cột Core của một màn danh sách, `key` thuộc tập khoá màn đó đưa vào `kiemCotMoRong`. Màn khai cột
 * Core qua kiểu này nên thêm một cột Core mà quên đưa khoá vào tập kiểm là lỗi biên dịch — đường
 * "Core nâng cấp thêm một cột" của ADR-0084 không lọt qua phép kiểm.
 */
export type CotCore<T, K extends string> = DataColumnDef<T> & { readonly key: K };

/**
 * Định nghĩa gốc của phép kiểm: fe-architecture.md §2.7 luật 6 (ADR-0084). MỘT chỗ cho mọi màn danh
 * sách Core. Cột dự án trùng khoá cột Core, hoặc hai cột dự án trùng khoá nhau ⇒ ném `Error` nêu mã
 * màn và MỌI khoá trùng — ở dev lẫn production: không có nhánh `isDevMode()`, không bỏ qua cột,
 * không đè cột Core.
 *
 * Màn gọi lúc DỰNG (khởi tạo trường), không trong `computed` cột: `computed` tính lại mỗi lần bảng
 * dịch về, và lỗi ném trong đó chỉ nổ khi có thứ đọc bảng. Trả lại chính danh sách cột (rỗng khi
 * không khai) để màn giữ và nối qua `sangCotDataTable`.
 */
export function kiemCotMoRong<T>(
  maMan: keyof CoreScreenExtensions,
  khoaCotCore: readonly string[],
  cot: readonly ScreenExtColumn<T>[] | undefined,
): readonly ScreenExtColumn<T>[] {
  const cuaCore = new Set(khoaCotCore);
  const daGap = new Set<string>();
  const trungCore: string[] = [];
  const trungNhau: string[] = [];
  for (const c of cot ?? []) {
    if (cuaCore.has(c.key)) {
      trungCore.push(c.key);
    } else if (daGap.has(c.key)) {
      trungNhau.push(c.key);
    }
    daGap.add(c.key);
  }
  if (trungCore.length > 0 || trungNhau.length > 0) {
    const lyDo = [
      trungCore.length > 0 ? `trùng khoá cột Core: ${danhSachKhoa(trungCore)}` : null,
      trungNhau.length > 0 ? `hai cột dự án cùng khoá: ${danhSachKhoa(trungNhau)}` : null,
    ].filter((d) => d !== null);
    throw new Error(
      `CORE_SCREEN_EXT — màn "${maMan}": ${lyDo.join('; ')}. Đổi khoá cột của dự án ` +
        '(fe-architecture.md §2.7 luật 6, ADR-0084).',
    );
  }
  return cot ?? [];
}

/**
 * Màn Core đổi mỗi `ScreenExtColumn` thành một `DataColumnDef` có `header` ĐÃ DỊCH và `value`
 * (fe-architecture.md §2.7; `ColumnDef.value` ở fe-ui-conventions.md §9). Hàm thuần, một chỗ cho
 * ba màn danh sách — `dich` là `translate.instant` của màn gọi, để cột tính lại khi bảng dịch về.
 * Cột của seam KHÔNG có `sortable`: `sortBy` chỉ nhận allowlist của endpoint Core. Khoá đã qua
 * `kiemCotMoRong` lúc dựng màn — hàm này không kiểm lại.
 */
export function sangCotDataTable<T>(
  cot: readonly ScreenExtColumn<T>[] | undefined,
  dich: (khoa: string) => string,
): DataColumnDef<T>[] {
  return (cot ?? []).map((c) => ({
    key: c.key,
    header: dich(c.headerKey),
    value: c.value,
    width: c.width,
    align: c.align,
    hideBelow: c.hideBelow,
    priority: c.priority,
  }));
}

/** Mỗi khoá một lần, trong ngoặc kép — khoá trùng ba lần không in ba lần. */
function danhSachKhoa(khoa: readonly string[]): string {
  return [...new Set(khoa)].map((k) => `"${k}"`).join(', ');
}
