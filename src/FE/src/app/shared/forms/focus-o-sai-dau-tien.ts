import { Injector, Signal, afterNextRender, effect, inject } from '@angular/core';

/**
 * wiki-core/fe/09-forms-validation.md §5 — bấm gửi trên form không hợp lệ thì đưa focus về ô sai
 * ĐẦU TIÊN (theo thứ tự trên trang). Trên hộp thoại cuộn, lỗi có thể nằm ngoài tầm nhìn; không cuộn
 * tới thì nút gửi trông như hỏng. `focus()` của trình duyệt tự cuộn ô vào tầm nhìn.
 *
 * Nhận diện ô sai bằng `aria-invalid="true"` — dấu hiệu mà `app-input` đặt khi `invalid && touched`,
 * cũng là thứ trình đọc màn hình đọc, nên một nguồn cho cả hai. Trả `true` khi đã đặt được focus.
 */
export function focusOSaiDauTien(goc: ParentNode | null | undefined): boolean {
  const o = goc?.querySelector<HTMLElement>('[aria-invalid="true"]');
  if (!o) return false;
  o.focus();
  return true;
}

/**
 * Gọi trong constructor của component (cần injection context). Mỗi lần `lanGuiSai` tăng — lớp logic
 * của hộp tăng nó ở nhánh "bấm gửi nhưng form không hợp lệ" — thì SAU lần render kế tiếp mới đặt
 * focus, vì `aria-invalid` chỉ có mặt trên DOM sau khi lỗi được vẽ. Giá trị khởi tạo `0` không kích
 * hoạt gì.
 */
export function focusOSaiKhiGuiSai(
  lanGuiSai: Signal<number>,
  goc: () => ParentNode | null | undefined,
): void {
  const injector = inject(Injector);
  effect(() => {
    if (lanGuiSai() === 0) return;
    afterNextRender(() => focusOSaiDauTien(goc()), { injector });
  });
}
