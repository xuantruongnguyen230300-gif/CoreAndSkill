import { AbstractControl } from '@angular/forms';

/**
 * fe-ui-conventions.md §6.5, đoạn *Ô tự đặt focus lúc mở*. Ô mà màn, `Dialog` hay `Drawer` `edit`
 * tự đặt focus vào khi mở mang cờ này; rời ô đó khi chưa sửa gì thì control không `touched`.
 * Cờ không bao giờ gỡ.
 */
const CO_FOCUS_LUC_MO = 'data-focus-luc-mo';

/**
 * Hàm DUY NHẤT gắn cờ. Gắn cờ TRƯỚC rồi mới đặt focus: nếu ô mất focus ngay sau đó thì
 * `roiOLaCham` đã thấy cờ. Trả focus vì lý do khác (sau lỗi máy chủ, sau khi tải lại bản mới) gọi
 * `focus()` thường, không qua đây.
 */
export function datFocusLucMo(phanTu: HTMLElement, tuyChon?: FocusOptions): void {
  phanTu.setAttribute(CO_FOCUS_LUC_MO, '');
  phanTu.focus(tuyChon);
}

/**
 * CVA gọi `onTouched` lúc rời ô chỉ khi hàm này trả `true`: `false` khi phần tử vừa mất focus mang
 * cờ **và** control chưa `dirty`.
 */
export function roiOLaCham(phanTu: EventTarget | null, control: AbstractControl | null): boolean {
  const mangCo = phanTu instanceof Element && phanTu.hasAttribute(CO_FOCUS_LUC_MO);
  return !(mangCo && control?.dirty !== true);
}
