import { ChangeDetectionStrategy, Component, effect, inject, untracked } from '@angular/core';
import { MessageService } from 'primeng/api';
import { Toast } from 'primeng/toast';

import { ToastService, ToastSeverity } from '../../../core/toast/toast.service';

/**
 * Thời lượng toast BAY VÀO, mili-giây — phản chiếu token **`--dur-slow`** (`320ms`,
 * src/styles/_tokens.scss; DESIGN.md §7 gán đích danh token này cho "`Toast` bay vào").
 *
 * Vì sao con số sống trong TypeScript chứ không trong CSS: `showTransitionOptions` của
 * `primeng/toast` nhận một chuỗi tham số hoạt ảnh Angular, và chuỗi đó **không giải `var()`**.
 * Luật **F37** (docs/RULES.md §7) đòi hằng số nêu ĐÍCH DANH token nó phản chiếu, để hai nơi truy
 * được về nhau; đổi `--dur-slow` thì sửa cả con số này.
 */
export const TOAST_THOI_LUONG_VAO_MS = 320;

/**
 * Thời lượng toast RỜI ĐI, mili-giây — phản chiếu token **`--dur-base`** (`200ms`,
 * src/styles/_tokens.scss). Cùng lý do "sống trong TypeScript" với hằng trên.
 *
 * 🛑 Ra NHANH hơn vào là có chủ đích, và `Toast` là component nhóm này DUY NHẤT giữ được điều đó:
 * `primeng/toast` có HAI input rời nhau (`showTransitionOptions`, `hideTransitionOptions`), nên cả
 * bốn khoản của Design/Components/Toast.md khớp được. `Dialog` và `Drawer` chỉ có MỘT input
 * `transitionOptions` dùng chung hai chiều, nên chúng đã bỏ hẳn phần bất đối xứng — ADR-0073
 * quyết định 5 và 6. Đừng "cho ba component trông giống nhau" bằng cách gỡ chỗ này.
 */
export const TOAST_THOI_LUONG_RA_MS = 200;

/**
 * Đường cong bay vào — phản chiếu token **`--ease-decelerate`** (`cubic-bezier(0, 0, 0, 1)`,
 * src/styles/_tokens.scss): DESIGN.md §7 dành nó cho phần tử **đi vào** màn hình.
 */
const TOAST_DUONG_CONG_VAO = 'cubic-bezier(0, 0, 0, 1)';

/**
 * Đường cong rời đi — phản chiếu token **`--ease-accelerate`** (`cubic-bezier(0.3, 0, 1, 1)`,
 * src/styles/_tokens.scss): DESIGN.md §7 dành nó cho phần tử **rời khỏi** màn hình.
 */
const TOAST_DUONG_CONG_RA = 'cubic-bezier(0.3, 0, 1, 1)';

/** Chuỗi truyền vào `[showTransitionOptions]`. Ghép từ hai hằng trên — không viết tay lần thứ hai. */
export const TOAST_SHOW_TRANSITION_OPTIONS = `${TOAST_THOI_LUONG_VAO_MS}ms ${TOAST_DUONG_CONG_VAO}`;

/** Chuỗi truyền vào `[hideTransitionOptions]`. */
export const TOAST_HIDE_TRANSITION_OPTIONS = `${TOAST_THOI_LUONG_RA_MS}ms ${TOAST_DUONG_CONG_RA}`;

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
 * Lớp bọc PrimeNG (`shared/ui/`) — nơi DUY NHẤT được import `primeng/*` ngoài `core/theme`,
 * `core/i18n` và `app.config.ts` (luật F5). Đọc hàng đợi từ `ToastService` (core/) rồi đẩy vào
 * `MessageService` của PrimeNG — bản thân `core/` không được biết thư viện UI.
 */
@Component({
  selector: 'app-toast',
  standalone: true,
  imports: [Toast],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [MessageService],
  templateUrl: './toast.component.html',
})
export class ToastComponent {
  private readonly toast = inject(ToastService);
  private readonly messageService = inject(MessageService);

  /**
   * KHÔNG mở thành `input()`: thời lượng chuyển động là thẩm quyền của spec, không phải của nơi
   * gọi — và toàn app chỉ dựng MỘT `<app-toast>` nên không có ca nào cần khác đi.
   */
  protected readonly showTransitionOptions = TOAST_SHOW_TRANSITION_OPTIONS;
  protected readonly hideTransitionOptions = TOAST_HIDE_TRANSITION_OPTIONS;

  constructor() {
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
          life: tuTatSau,
          sticky: tuTatSau === undefined,
        });
      }
    });
  }

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
}
