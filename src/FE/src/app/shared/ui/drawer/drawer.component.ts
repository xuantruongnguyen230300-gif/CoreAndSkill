import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnChanges,
  SimpleChanges,
  computed,
  inject,
  input,
  linkedSignal,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { DOCUMENT } from '@angular/common';
import { Drawer, DrawerModule } from 'primeng/drawer';
import { TranslatePipe } from '@ngx-translate/core';

import { TRUY_VAN_DUOI_BP_SM } from '../../../core/theme/diem-ngat';
import { datFocusLucMo } from '../../forms/focus-luc-mo';

let boDemId = 0;

/**
 * Design/Components/Drawer.md. Nơi DUY NHẤT trong ứng dụng được import `primeng/drawer`. Bẫy focus,
 * khoá cuộn nền, lớp phủ là hành vi "khó" mà PrimeNG đã giải — lớp bọc thu hẹp API về đúng những gì
 * màn Core cần và gánh phần PrimeNG KHÔNG làm đúng spec:
 *
 * - `role="dialog"`, `aria-modal`, `aria-labelledby`, `id` gốc: `p-drawer` cứng `role="complementary"`
 *   và không có `aria-modal` → đặt lại trên phần tử gốc ngay khi mở (`khiHien`).
 * - Focus khi mở: vào ô nhập đầu tiên (hoặc nút đóng nếu thân không có ô nhập).
 * - Focus khi đóng: ghi nhớ phần tử giữ focus lúc mở, trả về ĐÓ ở mọi đường đóng (nút đóng,
 *   Escape, bấm lớp phủ, trang đặt `open = false`); phần tử không còn trong DOM thì về `main`.
 *
 * Chuyển động trượt vào/ra KHÔNG đi qua TypeScript (luật F62, ADR-0123): thời lượng và đường cong
 * của tấm đè bằng token ở `src/styles/_thu-vien.scss` (khối `.app-drawer`), thư viện đọc giá trị
 * đã tính của phần tử. Lớp bọc không bind `transitionOptions` (deprecated, không ai đọc) và không
 * truyền `duration`.
 *
 * 🚧 Chỉ dựng biến thể `edit` (có lớp phủ, bẫy focus, `aria-modal`). Biến thể `inspect` HOÃN (Drawer.md, mục *Biến thể `inspect` — hoãn*):
 * `p-drawer` (PrimeNG 21.1.10, bản đang ghim) gắn cứng `pFocusTrap` trên phần tử gốc và không có input tắt nó — Drawer.md
 * §Cần chốt 2 đã có câu trả lời "không tắt được", nên `inspect` phải tự dựng phần khung. Cũng chưa
 * dựng `dirty`/`confirmDiscard` (panel lọc luôn `dirty = false`, màn sửa nhanh chưa có) và chế độ
 * toàn màn của thư viện (`fullScreen` không phơi ra — Drawer.md §Cần chốt 3 tự đóng).
 */
@Component({
  selector: 'app-drawer',
  standalone: true,
  imports: [DrawerModule, TranslatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './drawer.component.html',
  styleUrl: './drawer.component.scss',
})
export class DrawerComponent implements OnChanges {
  private readonly doc = inject(DOCUMENT);
  private readonly destroyRef = inject(DestroyRef);

  /** Trang cha giữ nguồn sự thật (component dumb). */
  readonly open = input(false);
  /** Chỉ `edit`: xem phần 🚧 ở chú thích lớp. */
  readonly variant = input<'edit'>('edit');
  readonly position = input<'right' | 'left' | 'bottom'>('right');
  readonly size = input<'sm' | 'md' | 'lg'>('md');
  readonly title = input.required<string>();
  readonly loading = input(false);
  /** `id` của phần tử gốc — đích `aria-controls` của nút mở (vd. nút lọc của Toolbar). */
  readonly panelId = input<string | null>(null);

  /** Phát SAU khi drawer đã tự đóng (nút đóng, Escape, bấm lớp phủ) — drawer không tự đặt `open`. */
  readonly closed = output<void>();

  private readonly pDrawer = viewChild(Drawer);

  /**
   * `visible` hai chiều của PrimeNG; `open()` (một chiều) là nguồn sự thật thật sự. `linkedSignal`:
   * về đúng `open()` mỗi khi trang cha đổi nó, và vẫn ghi được ở `onVisibleChange` (luật K15 — không
   * đồng bộ hai signal bằng `effect()`). Phần việc DOM đi kèm lúc `open` đổi ở `ngOnChanges`.
   */
  protected readonly hienThi = linkedSignal(() => this.open());
  protected readonly idTieuDe = `app-drawer-tieu-de-${++boDemId}`;

  private readonly manHep = signal(false);
  /** `left` giữ nguyên ở mọi ngưỡng (Sidebar); còn lại hẹp hơn `$bp-sm` thì neo đáy. */
  protected readonly viTriThat = computed(() =>
    this.position() !== 'left' && this.manHep() ? 'bottom' : this.position(),
  );
  protected readonly lopGoc = computed(
    () => `app-drawer app-drawer--${this.size()} app-drawer--${this.viTriThat()}`,
  );

  /** Phần tử giữ focus lúc mở — nơi trả focus về khi đóng. */
  private noiTraFocus: HTMLElement | null = null;
  /** Đã ghi nhớ focus lúc mở mà chưa trả — tránh trả focus hai lần khi người dùng đóng trước rồi trang cha mới đặt `open = false`. */
  private canTraFocus = false;
  /**
   * Lớp phủ của lần mở hiện tại. Thư viện dựng nó thẳng vào <body> (ngoài cây của component) và chỉ
   * tự gỡ khi `animationend` của pha ra bắn — xem `donLopPhuKhiHuy`.
   */
  private matNa: HTMLElement | null = null;

  constructor() {
    const mq = this.doc.defaultView?.matchMedia?.(TRUY_VAN_DUOI_BP_SM);
    if (mq) {
      this.manHep.set(mq.matches);
      const khiDoi = (e: MediaQueryListEvent): void => this.manHep.set(e.matches);
      mq.addEventListener('change', khiDoi);
      this.destroyRef.onDestroy(() => mq.removeEventListener('change', khiDoi));
    }

    this.destroyRef.onDestroy(() => this.donLopPhuKhiHuy());
  }

  /**
   * Việc DOM đi kèm `open` đổi. `ngOnChanges` chạy TRƯỚC khi template cập nhật nên focus vẫn ở nút
   * vừa mở (lúc này `activeElement` còn là nút đó, drawer chưa dựng).
   */
  ngOnChanges(thayDoi: SimpleChanges): void {
    if (!thayDoi['open']) {
      return;
    }
    if (this.open()) {
      const dangFocus = this.doc.activeElement;
      this.noiTraFocus = dangFocus instanceof HTMLElement ? dangFocus : null;
      this.canTraFocus = true;
    } else if (this.canTraFocus) {
      // Trang cha đặt `open = false` (vd. bấm Áp dụng): không phát `closed`, nhưng vẫn trả focus.
      this.traFocus();
    }
  }

  /** `visibleChange(false)` của PrimeNG: nút đóng, Escape hoặc bấm lớp phủ. */
  protected onVisibleChange(hienThiMoi: boolean): void {
    if (hienThiMoi || !this.hienThi()) {
      return;
    }
    this.hienThi.set(false);
    this.traFocus();
    this.closed.emit();
  }

  /** PrimeNG đã dựng phần tử gốc: đặt ngữ nghĩa đúng spec rồi đưa focus vào trong. */
  protected khiHien(): void {
    const goc = this.pDrawer()?.container;
    if (!goc) {
      return;
    }
    this.matNa = this.pDrawer()?.mask ?? null;
    goc.setAttribute('role', 'dialog');
    goc.setAttribute('aria-modal', 'true');
    goc.setAttribute('aria-labelledby', this.idTieuDe);
    const id = this.panelId();
    if (id) {
      goc.id = id;
    }
    const oNhap = goc.querySelector<HTMLElement>(
      '.app-drawer__than :is(input, select, textarea):not([disabled]):not([type="hidden"])',
    );
    const nutDong = goc.querySelector<HTMLElement>('.p-drawer-close-button button');
    // preventScroll: phần tử đang ở ngoài khung hình lúc bắt đầu trượt vào.
    // Đi qua datFocusLucMo: ô đầu tự nhận focus không bị tính là `touched` khi rời (fe-ui-conventions.md §6.5).
    const dich = oNhap ?? nutDong;
    if (dich) datFocusLucMo(dich, { preventScroll: true });
  }

  /**
   * Lớp phủ nằm ngoài cây của component, nên huỷ component KHÔNG kéo nó đi. Thư viện chỉ gỡ nó ở hai
   * chỗ: `animationend` của pha ra, và `ngOnDestroy` khi `visible` còn `true`. Huỷ giữa lúc hộp đang
   * đóng (`visible` đã `false`, `animationend` chưa bắn) lọt khe giữa hai chỗ: lớp phủ phủ kín màn
   * và khoá cuộn nền ở lại tới khi tải lại trang. Gỡ thẳng khỏi DOM bằng `remove()` chứ không chỉ
   * qua `destroyModal`, để lớp phủ chắc chắn đi cùng lượt huỷ.
   */
  private donLopPhuKhiHuy(): void {
    const thuVien = this.pDrawer();
    if (thuVien?.mask) {
      thuVien.destroyModal();
    }
    this.matNa?.remove();
    this.matNa = null;
  }

  private traFocus(): void {
    const nut = this.noiTraFocus;
    this.noiTraFocus = null;
    this.canTraFocus = false;
    const dich = nut?.isConnected ? nut : this.doc.querySelector<HTMLElement>('main');
    if (!dich) {
      return;
    }
    if (!dich.matches('button, a[href], input, select, textarea, [tabindex]')) {
      dich.setAttribute('tabindex', '-1');
    }
    dich.focus({ preventScroll: true });
  }
}
