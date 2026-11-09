import {
  ChangeDetectionStrategy,
  Component,
  TemplateRef,
  computed,
  input,
  linkedSignal,
  output,
  viewChild,
} from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { Dialog, DialogModule } from 'primeng/dialog';
import { TranslatePipe } from '@ngx-translate/core';

import { datFocusLucMo } from '../../forms/focus-luc-mo';

/**
 * Design/Components/Dialog.md. Nơi DUY NHẤT trong ứng dụng được import `primeng/dialog`. Bẫy
 * focus, khôi phục focus và khoá cuộn nền là hành vi "khó" mà PrimeNG đã giải đúng — lớp bọc chỉ
 * thu hẹp API về đúng những gì màn hình Core cần (Design/COMPONENTS.md §4).
 *
 * Chuyển động mở/đóng KHÔNG đi qua TypeScript (luật F62, ADR-0123): thời lượng và đường cong đè
 * bằng token ở `src/styles/_thu-vien.scss`, thư viện đọc giá trị đã tính của phần tử. Lớp bọc
 * không bind `transitionOptions` (deprecated từ PrimeNG 21, không nuôi chuyển động nào) và không
 * truyền `duration` — truyền số thì thư viện ghi style nội tuyến và token không tới được.
 * ⚠️ Đừng bind một `transitionOptions` thay thế để "cho chắc": `focusOnShow` đang tắt; bật lại thì
 * hẹn giờ mặc định 150ms của thư viện quay lại và đặt focus tranh với `khiHien`.
 *
 * 🚧 F3 tối giản: bốn cỡ bề rộng, `dirty`/`dismissAttempted` (ConfirmDialog xử lý hộp hỏi tiếp
 * theo — Dialog không tự vẽ), `loading`/`loadingBlocksClose`, `errorTemplate`. Không dựng
 * `draggable`/`resizable`/`maximizable` — không màn F3 nào cần.
 */
@Component({
  selector: 'app-dialog',
  standalone: true,
  imports: [NgTemplateOutlet, DialogModule, TranslatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './dialog.component.html',
  styleUrl: './dialog.component.scss',
})
export class DialogComponent {
  readonly open = input(false);
  readonly size = input<'sm' | 'md' | 'lg' | 'full'>('md');
  readonly title = input.required<string>();
  readonly description = input<string | null>(null);
  readonly closable = input(true);
  readonly dismissOnBackdrop = input(true);
  /** Form đã có thay đổi chưa lưu — Escape/bấm ra ngoài phát `dismissAttempted` thay vì đóng. */
  readonly dirty = input(false);
  readonly loading = input(false);
  /** Bật khi `loading` là do một thao tác GHI — Escape không đóng được lúc đó (Dialog.md §Trạng thái). */
  readonly loadingBlocksClose = input(false);
  readonly errorTemplate = input<TemplateRef<unknown> | null>(null);

  /** Phát SAU khi đã trả focus về nơi mở — `Dialog` không tự đặt `open` về `false` (component dumb). */
  readonly closed = output<void>();
  /** Phát khi người dùng cố đóng lúc `dirty` — trang cha mở `ConfirmDialog` (Dialog.md §API). */
  readonly dismissAttempted = output<void>();

  private readonly pDialog = viewChild(Dialog);

  /**
   * PrimeNG cần visible hai chiều nội bộ; `open()` (input, một chiều) là nguồn sự thật thật sự.
   * `linkedSignal`: về đúng `open()` mỗi khi trang cha đổi nó, và vẫn ghi được ở `onVisibleChange`
   * (luật K15 — không đồng bộ hai signal bằng `effect()`).
   */
  protected readonly hienThi = linkedSignal(() => this.open());

  protected readonly khoaDong = computed(() => this.loading() && this.loadingBlocksClose());
  protected readonly maskDongDuoc = computed(() => this.closable() && this.dismissOnBackdrop());
  protected readonly bienTheKhoRong = computed(() => `app-dialog--${this.size()}`);

  /**
   * Hộp vừa hiện xong (`onShow` của PrimeNG, cuối hoạt ảnh — cùng lúc PrimeNG từng tự đặt focus).
   * Dialog.md §Accessibility, *Focus lúc mở*: ô nhập đầu; không có thì phần tử tương tác đầu của
   * thân/chân; không có nữa thì chính hộp. KHÔNG BAO GIỜ nút đóng. `focusOnShow` của PrimeNG tắt
   * để mọi lần đặt focus lúc mở đi qua `datFocusLucMo` (fe-ui-conventions.md §6.5).
   */
  protected khiHien(): void {
    const goc = this.pDialog()?.container();
    if (!goc) {
      return;
    }
    const vung = goc.querySelectorAll<HTMLElement>('.p-dialog-content, .p-dialog-footer');
    let dich: HTMLElement | null = null;
    for (const v of vung) {
      dich = v.querySelector<HTMLElement>(
        ':is(input, select, textarea):not([disabled]):not([type="hidden"]):not([readonly])',
      );
      if (dich) break;
    }
    if (!dich) {
      for (const v of vung) {
        dich = v.querySelector<HTMLElement>(
          'button:not([disabled]), a[href], [tabindex]:not([tabindex="-1"])',
        );
        if (dich) break;
      }
    }
    if (!dich) {
      goc.setAttribute('tabindex', '-1');
      dich = goc;
    }
    datFocusLucMo(dich);
  }

  /** `visibleChange` của PrimeNG báo mọi lần đóng: nút X, Escape, hoặc bấm ra ngoài. */
  protected onVisibleChange(hienThiMoi: boolean): void {
    if (hienThiMoi) {
      return;
    }
    if (this.khoaDong()) {
      // Đang gửi dữ liệu — Escape/backdrop không đóng được (Dialog.md §Trạng thái).
      this.hienThi.set(true);
      return;
    }
    if (this.dirty()) {
      // Có thay đổi chưa lưu — không tự đóng, để trang cha hỏi xác nhận.
      this.hienThi.set(true);
      this.dismissAttempted.emit();
      return;
    }
    this.hienThi.set(false);
    this.closed.emit();
  }
}
