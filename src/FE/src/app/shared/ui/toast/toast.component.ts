import { DOCUMENT, NgTemplateOutlet } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  effect,
  inject,
  input,
  signal,
  TemplateRef,
  untracked,
} from '@angular/core';
import { MessageService } from 'primeng/api';
import { ExclamationTriangleIcon } from 'primeng/icons/exclamationtriangle';
import { InfoCircleIcon } from 'primeng/icons/infocircle';
import { TimesCircleIcon } from 'primeng/icons/timescircle';
import { Toast } from 'primeng/toast';
import type { ToastPassThroughOptions } from 'primeng/types/toast';

import { ToastHanhDong, ToastService, ToastSeverity } from '../../../core/toast/toast.service';

/**
 * Thời gian tự tắt theo vai, mili-giây — bảng §Biến thể của Design/Components/Toast.md
 * (`success` 4 giây, `info` 5 giây, `warning` 8 giây). Tên vai của spec ánh xạ sang tên severity
 * của `ToastService`: spec `warning` ⇒ `warn`, spec `danger` ⇒ `error`. Hai bộ tên còn lệch nhau
 * là một mục ĐANG MỞ trong bảng của spec ("Bốn vai và tên của chúng"); ánh xạ ở đây là chỗ duy
 * nhất hai bộ tên gặp nhau, nên chốt tên xong thì sửa cả đây.
 *
 * 🛑 Vai `error` CỐ Ý vắng mặt, và sự vắng mặt đó được diễn đạt bằng KIỂU chứ không bằng một lời
 * chú: `Exclude<…, 'error'>` làm trình biên dịch đỏ nếu có người thêm `error` vào bảng. Spec khai
 * "Không tự tắt" cho vai đó — xem `thoiGianTuTat`.
 *
 * Đây là THAM SỐ HÀNH VI, không phải thời lượng một chuyển tiếp: nó KHÔNG phản chiếu token
 * `--dur-*` nào, cùng loại với `TOOLTIP_DELAY_CHUOT_MS`. Chỗ quyết là bảng §Biến thể — đổi con số
 * thì sửa spec trước.
 */
const TOAST_THOI_GIAN_TU_TAT_MS: Readonly<Record<Exclude<ToastSeverity, 'error'>, number>> = {
  success: 4_000,
  info: 5_000,
  warn: 8_000,
};

/**
 * Vùng thông báo theo vai (Design/Components/Toast.md §Accessibility, ba hàng "Vùng thông báo",
 * "role", "Ngăn chứa tồn tại sẵn"). Thư viện gắn cứng `role="alert"` + `aria-live="assertive"`
 * lên MỌI thông báo; pass-through ghi đè sau thuộc tính tĩnh, nên `success` / `info` hạ xuống
 * `role="status"` + `aria-live="polite"`, còn `warn` / `error` trả `{}` để GIỮ `alert` +
 * `assertive` của thư viện. `host` mang `aria-live="polite"` ngay khi phần tử chủ được dựng —
 * vùng live có mặt trong DOM TRƯỚC toast đầu tiên.
 *
 * `instance` của pass-through kiểu `unknown`: chỉ đọc `message.severity` qua kiểu hẹp, không ép
 * sang kiểu nội bộ của thư viện. Cố ý KHÔNG khai khoá `motion` ở đây (luật F62, F63).
 *
 * Hai rủi ro đã biết, chỉ đo được phần DOM: (1) `warn` / `error` nằm trong vùng `polite` của phần
 * tử chủ nên trình đọc màn hình có thể đọc hai lần; (2) có khe giữa thuộc tính tĩnh của thư viện
 * và lúc pass-through ghi đè. Trước khi lật dòng spec: đo bằng trình đọc màn hình thật.
 */
const TOAST_PT: ToastPassThroughOptions = {
  host: { 'aria-live': 'polite' },
  message: ({ instance }) => {
    const severity = (instance as { readonly message?: { readonly severity?: string } }).message
      ?.severity;
    return severity === 'success' || severity === 'info'
      ? { role: 'status', 'aria-live': 'polite' }
      : {};
  },
};

/**
 * Ngữ cảnh của mẫu nút hành động mà nơi dùng truyền vào `nutHanhDong`. `$implicit` là hành động;
 * `bam(id, hanhDong)` chạy nó đúng một lần và đặt `daBam` — nút phải `disabled` theo `daBam`.
 */
export interface ToastNutHanhDongContext {
  readonly $implicit: ToastHanhDong;
  readonly id: number;
  readonly daBam: boolean;
  readonly bam: (id: number, hanhDong: ToastHanhDong) => void;
}

/**
 * Lớp bọc PrimeNG (`shared/ui/`) — nơi DUY NHẤT được import `primeng/*` ngoài `core/theme`,
 * `core/i18n` và `app.config.ts` (luật F5). Đọc hàng đợi từ `ToastService` (core/) rồi đẩy vào
 * `MessageService` của PrimeNG — bản thân `core/` không được biết thư viện UI.
 *
 * Chuyển động vào/ra KHÔNG đi qua TypeScript (luật F62, ADR-0123): thời lượng và đường cong đè bằng
 * token ở `src/styles/_thu-vien.scss` (khối toast), thư viện đọc giá trị đã tính của phần tử. Lớp
 * bọc không bind `showTransitionOptions` / `hideTransitionOptions` (deprecated, không ai đọc) và
 * không truyền `duration`. Bất đối xứng vào chậm / ra nhanh của Toast.md nằm ở hai khối đè rời
 * nhau — đừng "cho ba component trông giống nhau" bằng cách gộp chúng.
 */
@Component({
  selector: 'app-toast',
  standalone: true,
  imports: [Toast, NgTemplateOutlet, ExclamationTriangleIcon, InfoCircleIcon, TimesCircleIcon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [MessageService],
  templateUrl: './toast.component.html',
  styleUrl: './toast.component.scss',
})
export class ToastComponent {
  protected readonly pt = TOAST_PT;
  private readonly toast = inject(ToastService);
  private readonly messageService = inject(MessageService);

  /**
   * Mẫu vẽ nút hành động. `shared/ui/` không được import `shared/components/` (ESLint, fe-architecture.md
   * §2.2), nên nút `Button` `secondary` cỡ `sm` (Toast.md) do nơi ghép — `app.html` — vẽ và truyền vào.
   * Không truyền mẫu thì toast có `hanhDong` không hiện nút.
   */
  readonly nutHanhDong = input<TemplateRef<ToastNutHanhDongContext> | null>(null);

  /** Id các toast đã bấm nút hành động — nút `disabled` ngay để không bấm đôi (Toast.md §Accessibility). */
  protected readonly daBam = signal<ReadonlySet<number>>(new Set());

  private readonly doc = inject(DOCUMENT);
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);

  constructor() {
    this.chanToastCuopFocus();
    effect(() => {
      if (this.toast.hangDoi().length === 0) {
        return;
      }
      for (const message of untracked(() => this.toast.layHet())) {
        const tuTatSau = this.thoiGianTuTat(message.severity);
        this.messageService.add({
          severity: message.severity,
          summary: message.summary,
          detail: message.traceId ? `traceId: ${message.traceId}` : undefined,
          data: { id: message.id, hanhDong: message.hanhDong },
          life: tuTatSau,
          sticky: tuTatSau === undefined,
        });
      }
    });
  }

  /** Chạy hành động của toast đúng một lần: ghi nhận trước, chạy sau. */
  protected readonly bam = (id: number, hanhDong: ToastHanhDong): void => {
    if (this.daBam().has(id)) {
      return;
    }
    this.daBam.update((ds) => new Set(ds).add(id));
    hanhDong.chay();
  };

  /**
   * Thời gian tự tắt của một vai, hoặc `undefined` khi vai đó KHÔNG tự tắt.
   *
   * 🛑 Toast lỗi ở lại tới khi người dùng đóng — Design/Components/Toast.md §Biến thể và ❌ "Không
   * để toast lỗi tự tắt". Hỏng theo chiều này KHÔNG gây lỗi, không làm test nào đỏ, và người dùng
   * không biết mình vừa mất gì: bỏ lỡ một toast thành công thì không mất gì vì kết quả đã thấy
   * trên màn hình, còn bỏ lỡ một toast LỖI nghĩa là họ tin thao tác đã thành công.
   *
   * `sticky` là đường DUY NHẤT tắt đồng hồ, và bỏ trống `life` KHÔNG đủ: `primeng/toast` chỉ bọc
   * lời gọi `setTimeout` trong `if (!this.message?.sticky)`, còn bên trong thì `this.message?.life
   * || this.life || 3000` rơi về **3000** khi `life` rỗng — đúng con số làm toast lỗi biến mất
   * sau ba giây, tức chính lỗi đang sửa.
   *
   * Đồng hồ vẫn thuộc về thư viện. §API dự kiến của spec đặt "hẹn giờ" ở hàng đợi `core/`; đưa nó
   * về đó là đổi bề mặt của `ToastService`, việc khác — mục này chỉ đóng chiều hỏng nguy hiểm.
   */
  private thoiGianTuTat(severity: ToastSeverity): number | undefined {
    return severity === 'error' ? undefined : TOAST_THOI_GIAN_TU_TAT_MS[severity];
  }

  /**
   * Toast KHÔNG tự cướp focus — kể cả toast lỗi (Design/Components/Toast.md §Token dùng, ~:144-147).
   * Template của `primeng/toast` gắn `autofocus` cứng lên nút đóng, nên trình duyệt chuyển focus vào
   * đó mỗi khi một toast được chèn. Hai lớp: gỡ thuộc tính ngay khi nút xuất hiện, và nếu focus vẫn
   * rơi vào nút đóng thì trả về phần tử đang giữ focus lúc toast xuất hiện. Người dùng chủ động Tab
   * tới nút đóng sau đó không bị đụng tới.
   */
  private chanToastCuopFocus(): void {
    const view = this.doc.defaultView;
    if (!view || typeof MutationObserver === 'undefined') {
      return;
    }
    const observer = new MutationObserver((mutations) => {
      const truoc = this.doc.activeElement as HTMLElement | null;
      let coNutMoi = false;
      for (const m of mutations) {
        m.addedNodes.forEach((n) => {
          if (!(n instanceof HTMLElement)) {
            return;
          }
          const nuts = Array.from(n.querySelectorAll('button[autofocus]'));
          if (n.matches('button[autofocus]')) {
            nuts.push(n);
          }
          for (const nut of nuts) {
            nut.removeAttribute('autofocus');
            coNutMoi = true;
          }
        });
      }
      if (!coNutMoi) {
        return;
      }
      view.requestAnimationFrame(() => {
        const hien = this.doc.activeElement as HTMLElement | null;
        if (hien && hien !== truoc && this.host.nativeElement.contains(hien)) {
          if (truoc && truoc !== this.doc.body && truoc.isConnected) {
            truoc.focus({ preventScroll: true });
          } else {
            hien.blur();
          }
        }
      });
    });
    observer.observe(this.host.nativeElement, { childList: true, subtree: true });
    inject(DestroyRef).onDestroy(() => observer.disconnect());
  }
}
