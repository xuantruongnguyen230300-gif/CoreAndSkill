/**
 * Design/Components/AuthCard.md §Accessibility, dòng *Focus sau khi gửi hỏng*. Khoá form lúc gửi
 * làm phần tử đang giữ focus mất focus (về `<body>`); sau khi mở khoá, trang trả focus lại.
 *
 * `focus()` thường — không đi qua cơ chế "ô tự đặt focus lúc mở màn" (fe-ui-conventions.md §6.5):
 * đây là trả focus, không phải mở màn.
 */

/** Phần tử đang giữ focus nếu nó nằm trong `goc`; không thì `null` (focus ở `<body>` hay ngoài vùng). */
export function phanTuDangFocus(goc: Node | null | undefined): HTMLElement | null {
  const dang = document.activeElement;
  return dang instanceof HTMLElement && dang !== document.body && !!goc?.contains(dang)
    ? dang
    : null;
}

/**
 * Trả focus về `truoc` nếu nó còn trong DOM và nhận focus được; ngược lại gọi `phuongAn` (ô đầu tiên
 * đang báo lỗi, không có thì ô đầu tiên của form — do trang cài). Gọi SAU lần render kế tiếp của
 * `moForm()`, khi ô đã hết `disabled` trên DOM.
 */
export function traFocus(truoc: HTMLElement | null, phuongAn: () => void): void {
  const conNhanDuoc =
    truoc !== null &&
    truoc.isConnected &&
    !(truoc as HTMLElement & { disabled?: boolean }).disabled &&
    truoc.checkVisibility();
  if (conNhanDuoc) {
    truoc.focus();
    if (document.activeElement === truoc) return;
  }
  phuongAn();
}
