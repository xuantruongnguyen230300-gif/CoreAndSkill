import type { ZIndex } from 'primeng/config';

/**
 * Thang lớp chồng DUY NHẤT của app là `--z-*` (`styles/_tokens.scss`, Design/DESIGN.md §6.4).
 * PrimeNG chỉ nhận SỐ, không nhận `var()`, nên giá trị phản chiếu ở đây; đổi token thì đổi cả ở
 * đây (spec `prime-z-index.spec.ts` so hai bên bằng trình duyệt thật).
 *
 * Thứ tự: Dialog / Drawer `edit` + mặt nạ (`modal`, 400 + số đếm của thư viện, tức < 500) < ConfirmDialog
 * (backdrop và hộp cùng `--z-dialog` 500, hộp đứng sau trong DOM) < panel nổi / Menu / Tooltip (600) < Toast (`--z-toast`, CSS riêng).
 */
export const CORE_PRIME_Z_INDEX: ZIndex = {
  modal: 400, // --z-backdrop
  overlay: 600, // --z-popover
  menu: 600, // --z-popover
  tooltip: 600, // --z-popover
};
