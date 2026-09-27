import {
  ChangeDetectionStrategy,
  Component,
  TemplateRef,
  computed,
  effect,
  input,
  output,
  signal,
} from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { DialogModule } from 'primeng/dialog';
import { TranslatePipe } from '@ngx-translate/core';

/**
 * Thời lượng mở VÀ đóng hộp thoại, mili-giây — phản chiếu token **`--dur-base`** (`200ms`,
 * src/styles/_tokens.scss; DESIGN.md §7 gán đích danh token này cho "Mở/đóng `Dialog`").
 *
 * Vì sao con số sống trong TypeScript chứ không trong CSS: `transitionOptions` của `primeng/dialog`
 * nhận một chuỗi tham số hoạt ảnh Angular, và chuỗi đó bị phân tích thành số — nó **không giải
 * `var()`**. Token đặt được *giá trị* nhưng không *đi tới* được, nên giá trị buộc phải sống ở nơi
 * thứ hai; luật **F37** (docs/RULES.md §7) đòi nơi thứ hai đó nêu ĐÍCH DANH token nó phản chiếu,
 * để hai nơi truy được về nhau. Đổi `--dur-base` thì sửa cả con số này — cổng F37 so hai bên.
 *
 * MỘT thời lượng cho cả hai chiều, không phải hai: `transitionOptions` là **một** input và cùng
 * chuỗi đó đi vào cả `showAnimation` lẫn `hideAnimation`. ADR-0073 quyết định 5 bỏ hẳn phần bất
 * đối xứng vào/ra của spec vì nền này không chở được nó — khác `Toast`, nơi hai input rời nhau nên
 * bất đối xứng giữ được.
 *
 * Backdrop KHÔNG đi qua đây: nó là CSS thuần và nối bằng `semantic.mask.transitionDuration` ở
 * core/theme/prime-preset.ts, với CÙNG token `--dur-base` — đó là cách câu "backdrop mờ dần cùng
 * lúc với hộp" của spec thành thứ kiểm được (ADR-0073 quyết định 7).
 */
export const DIALOG_THOI_LUONG_MS = 200;

/**
 * Đường cong mở VÀ đóng — phản chiếu token **`--ease-standard`** (`cubic-bezier(0.2, 0, 0, 1)`,
 * src/styles/_tokens.scss). DESIGN.md §7 gọi nó là "mặc định cho mọi chuyển tiếp", và đây đúng là
 * ca cần nó: một đường cong phục vụ cả chiều vào lẫn chiều ra. `--ease-decelerate` chỉ dành cho
 * phần tử ĐI VÀO màn hình, `--ease-accelerate` chỉ cho phần tử RỜI KHỎI — không token nào trong hai
 * cái đó đúng cho một giá trị dùng chung hai chiều.
 *
 * Cùng lý do với `DIALOG_THOI_LUONG_MS`: chuỗi hoạt ảnh Angular không giải `var()`.
 */
const DIALOG_DUONG_CONG = 'cubic-bezier(0.2, 0, 0, 1)';

/**
 * Chuỗi truyền vào `[transitionOptions]`. Ghép từ hai hằng trên — không viết tay lần thứ hai.
 *
 * ⚠️ Đừng chèn thêm một đơn vị thời gian nào vào chuỗi này: `parseDurationToMilliseconds` của
 * `primeng/dialog` quét `/([\d.]+)(ms|s)\b/g` rồi **CỘNG DỒN** mọi khớp để biết lúc nào trả focus
 * vào hộp. Một `0.3s` lạc trong chuỗi làm focus trễ thêm 300ms, không lỗi nào báo.
 */
export const DIALOG_TRANSITION_OPTIONS = `${DIALOG_THOI_LUONG_MS}ms ${DIALOG_DUONG_CONG}`;

/**
 * Design/Components/Dialog.md. Nơi DUY NHẤT trong ứng dụng được import `primeng/dialog`. Bẫy
 * focus, khôi phục focus và khoá cuộn nền là hành vi "khó" mà PrimeNG đã giải đúng — lớp bọc chỉ
 * thu hẹp API về đúng những gì màn hình Core cần (Design/COMPONENTS.md §4).
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

  /** PrimeNG cần visible hai chiều nội bộ; `open()` (input, một chiều) là nguồn sự thật thật sự. */
  protected readonly hienThi = signal(false);

  /**
   * KHÔNG mở thành `input()`: thời lượng chuyển động là thẩm quyền của spec, không phải của nơi
   * gọi. Mỗi hộp thoại tự chọn một con số là mất luôn thứ mà hằng số có tên vừa mua được.
   */
  protected readonly transitionOptions = DIALOG_TRANSITION_OPTIONS;

  protected readonly khoaDong = computed(() => this.loading() && this.loadingBlocksClose());
  protected readonly maskDongDuoc = computed(() => this.closable() && this.dismissOnBackdrop());
  protected readonly bienTheKhoRong = computed(() => `app-dialog--${this.size()}`);

  constructor() {
    effect(() => {
      this.hienThi.set(this.open());
    });
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
