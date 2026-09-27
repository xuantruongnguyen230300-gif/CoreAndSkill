import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  computed,
  effect,
  input,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import { ButtonComponent } from '../button/button.component';
import { NoticeBannerComponent } from '../notice-banner/notice-banner.component';

let dem = 0;

/**
 * Design/Components/ConfirmDialog.md. 🚧 Minimal F2 (ADR-0037): dựng TRỰC TIẾP (bẫy focus, khoá
 * cuộn nền, Escape, backdrop) thay vì trên `Dialog` — `Dialog.md` CHƯA được dựng ở F2 (không nằm
 * trong bốn component kéo sớm), nên hành vi overlay được viết ngay trong chính component này.
 * Khi F3 dựng `Dialog` thật, tách phần overlay dùng chung ra là việc còn nợ — ghi ở bảng
 * "đã có → còn thiếu" trong ConfirmDialog.md. Component dumb: chỉ hỏi, không tự thực hiện hành
 * động (COMPONENTS.md §5).
 */
@Component({
  selector: 'app-confirm-dialog',
  standalone: true,
  imports: [TranslatePipe, ButtonComponent, NoticeBannerComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './confirm-dialog.component.html',
  styleUrl: './confirm-dialog.component.scss',
})
export class ConfirmDialogComponent {
  readonly open = input(false);
  readonly severity = input<'ask' | 'warning' | 'danger'>('ask');
  readonly title = input.required<string>();
  readonly message = input.required<string>();
  readonly confirmLabel = input.required<string>();
  readonly cancelLabel = input<string | null>(null);
  readonly requireAcknowledge = input<string | null>(null);
  readonly loading = input(false);
  readonly error = input<string | null>(null);

  /** Hộp KHÔNG tự đóng sau khi phát — nơi gọi đóng sau khi thao tác xong (ConfirmDialog.md §API). */
  readonly confirmed = output<void>();
  /** Gộp cả ba đường thoát: nút huỷ, Escape, bấm ra ngoài. */
  readonly cancelled = output<void>();

  protected readonly idTieuDe = `confirm-dialog-tieu-de-${++dem}`;
  protected readonly idNoiDung = `confirm-dialog-noi-dung-${dem}`;

  protected readonly daDanhDau = signal(false);
  protected readonly khoaXacNhan = computed(
    () => this.requireAcknowledge() !== null && !this.daDanhDau(),
  );

  protected readonly icon = computed(() =>
    this.severity() === 'ask' ? 'question-circle' : 'exclamation-triangle',
  );

  private readonly hopThoai = viewChild<ElementRef<HTMLElement>>('hopThoai');
  private phanTuTruocKhiMo: HTMLElement | null = null;

  constructor() {
    effect(() => {
      if (this.open()) {
        this.phanTuTruocKhiMo = document.activeElement as HTMLElement | null;
        this.daDanhDau.set(false);
        document.body.style.overflow = 'hidden';
        queueMicrotask(() =>
          this.hopThoai()?.nativeElement.querySelector<HTMLButtonElement>('button')?.focus(),
        );
      } else {
        document.body.style.overflow = '';
        this.phanTuTruocKhiMo?.focus();
        this.phanTuTruocKhiMo = null;
      }
    });
  }

  protected onKeydown(event: KeyboardEvent): void {
    if (event.key === 'Escape') {
      event.preventDefault();
      this.huy();
      return;
    }
    if (event.key === 'Tab') {
      this.bayFocus(event);
    }
  }

  protected onBackdrop(): void {
    this.huy();
  }

  protected huy(): void {
    if (this.loading()) {
      return;
    }
    this.cancelled.emit();
  }

  protected xacNhan(): void {
    if (this.loading() || this.khoaXacNhan()) {
      return;
    }
    this.confirmed.emit();
  }

  private bayFocus(event: KeyboardEvent): void {
    const vungChua = this.hopThoai()?.nativeElement;
    if (!vungChua) {
      return;
    }
    const dsCoTheFocus = vungChua.querySelectorAll<HTMLElement>(
      'button:not([disabled]), input:not([disabled])',
    );
    if (dsCoTheFocus.length === 0) {
      return;
    }
    const dauTien = dsCoTheFocus[0];
    const cuoiCung = dsCoTheFocus[dsCoTheFocus.length - 1];
    if (event.shiftKey && document.activeElement === dauTien) {
      event.preventDefault();
      cuoiCung.focus();
    } else if (!event.shiftKey && document.activeElement === cuoiCung) {
      event.preventDefault();
      dauTien.focus();
    }
  }
}
