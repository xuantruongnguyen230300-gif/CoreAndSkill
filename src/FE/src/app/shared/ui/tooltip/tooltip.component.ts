import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  Renderer2,
  afterNextRender,
  afterRenderEffect,
  computed,
  effect,
  inject,
  input,
  isDevMode,
  signal,
} from '@angular/core';
import { Tooltip } from 'primeng/tooltip';

import {
  canhBaoChuaNoiMoTa,
  canhBaoNoiDungSai,
  doNoiDungChieuVao,
  laComponent,
} from './tooltip-noi-dung';

/**
 * Độ trễ trước khi hộp nổi hiện khi RÊ CHUỘT, mili-giây. Bàn phím KHÔNG dùng giá trị này — nó
 * hiện ngay.
 *
 * Con số quyết ở Design/Components/Tooltip.md §API dự kiến, mục "`delay` là tham số hành vi,
 * không phải token chuyển động": giá trị chỉ đi vào TypeScript, không bao giờ xuất hiện trong CSS,
 * và `--dur-slow` đang mang nghĩa khác (thời lượng một chuyển tiếp chậm). Hằng số này là chỗ ÁP,
 * đúng như một tên token là chỗ áp — đổi con số thì sửa spec trước.
 */
export const TOOLTIP_DELAY_CHUOT_MS = 320;

let dem = 0;

/**
 * Design/Components/Tooltip.md. Nơi DUY NHẤT trong ứng dụng được import `primeng/tooltip`. Định vị
 * một lớp nổi khi trang cuộn hoặc khi nó chạm mép màn hình là hành vi "khó" mà thư viện đã giải
 * đúng (Design/COMPONENTS.md §4); phần Core tự viết là luật KHI NÀO được hiện và HIỆN CÁI GÌ.
 *
 * Hình dạng là lớp BỌC, không phải input `for` (§Hình dạng — đã chốt): thư viện chỉ vào được bằng
 * cách gắn directive của nó lên chính host, nên phần tử neo đi vào bằng nội dung chiếu:
 *
 *     <app-tooltip [text]="…"><button …>…</button></app-tooltip>
 *
 * Ba điều dễ đọc nhầm, cả ba đều đã chốt ở spec:
 *
 *  1. **Hai kênh tách nhau** (§Accessibility). Hộp nổi của thư viện là kênh NHÌN và không tham gia
 *     quan hệ ARIA nào — nó chỉ được dựng lúc hiện, gắn vào `body`, và `create()` của thư viện
 *     không truyền tuỳ chọn `id` vào phần tử nên không có đích ổn định để trỏ tới. Kênh NGHE là
 *     phần tử `.sr-only` do chính lớp bọc vẽ: `role="tooltip"` + `id` tự sinh + đúng chuỗi `text`,
 *     và phần tử neo nhận `aria-describedby` trỏ tới nó. 🛑 Trừ khi phần tử neo là một component
 *     tự dựng control bên trong (`<app-check>`…): khi đó lớp bọc THÔI ghi, `id` đi ra ngoài qua
 *     `idMoTa` và nơi gọi nối vào input `describedBy` của chính component đó — vì sao, và dấu hiệu
 *     nào nhận ra ca đó, nằm cạnh `laComponent` trong `./tooltip-noi-dung`.
 *  2. **Bàn phím đi qua `focusin`/`focusout` trên host**, không qua đường `focus` của thư viện:
 *     thư viện gắn `focus`/`blur` lên chính host, mà `focus` không nổi bọt — phần tử chiếu vào
 *     nhận focus thì không có gì bắn. `tooltipEvent` cố ý giữ nguyên mặc định `'hover'` để thư
 *     viện KHÔNG gắn thêm một cặp thứ hai; hai đường cùng mở một hộp là hai đường sẽ lệch nhau.
 *  3. **`Escape` do lớp bọc tự lo.** `hideOnEscape` của thư viện chỉ đăng ký lắng nghe bên trong
 *     `activate()` — nhánh mở CÓ độ trễ — mà đường bàn phím cố ý gọi thẳng `show()` để hiện ngay.
 *
 * 🚧 Một điểm của spec nền thư viện KHÔNG đạt: §Accessibility đòi chuyển tiếp hiện/ẩn theo
 * `--dur-fast`, nhưng `show()` của thư viện gọi `fadeIn(container, 250)` — một vòng lặp
 * `requestAnimationFrame` ghi thẳng `opacity` nội tuyến, không đọc CSS. Khối override
 * `prefers-reduced-motion` ở `styles/_thu-vien.scss` bỏ hẳn chuyển tiếp đó; riêng con số 250ms
 * không đổi được từ ngoài.
 */
@Component({
  selector: 'app-tooltip',
  // Nơi gọi đọc `idMoTa` qua tham chiếu template `#tip="appTooltip"` — khoản 1 của §Accessibility,
  // "Phần tử neo là một component Core": `id` phần tử mô tả là BỀ MẶT CÔNG KHAI.
  exportAs: 'appTooltip',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  hostDirectives: [Tooltip],
  templateUrl: './tooltip.component.html',
  styleUrl: './tooltip.component.scss',
  host: {
    '(mouseenter)': 'khiReChuot()',
    '(focusin)': 'khiNhanFocus()',
    '(focusout)': 'khiMatFocus($event)',
    '(keydown.escape)': 'khiEscape()',
  },
})
export class TooltipComponent {
  private readonly el = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly renderer = inject(Renderer2);
  /** `hostDirectives` dựng directive của thư viện TRƯỚC component, nên nó có sẵn để inject. */
  private readonly prime = inject(Tooltip, { self: true });

  /** Bắt buộc. Rỗng thì không dựng gì — §Trạng thái `empty`. */
  readonly text = input.required<string>();
  /**
   * Hợp đồng về ĐỘ DÀI nội dung, không phải một hình thức riêng — §Biến thể và §Kích thước chốt
   * MỘT cỡ duy nhất, nên hai biến thể trông giống hệt nhau hôm nay. Giá trị vẫn đi ra hộp nổi
   * thành một lớp để `styles/_thu-vien.scss` tách được hai ca nếu spec đổi ý.
   */
  readonly variant = input<'label' | 'hint'>('label');
  /** Chỉ là hướng ƯU TIÊN — thư viện tự lật khi không đủ chỗ. */
  readonly position = input<'top' | 'bottom' | 'left' | 'right'>('top');
  /** Mili-giây, CHỈ áp cho chuột. Bàn phím luôn hiện ngay, không nhận giá trị này. */
  readonly delay = input(TOOLTIP_DELAY_CHUOT_MS);
  readonly disabled = input(false);

  /**
   * `id` của phần tử mô tả. **Công khai** (spec §Accessibility, "Phần tử neo là một component
   * Core", khoản 1) — phần tử neo là một component Core tự dựng control bên trong thì nơi gọi
   * phải nối được `id` này vào input `describedBy` của chính component đó:
   *
   *     <app-tooltip #tip="appTooltip" [text]="…">
   *       <app-check [describedBy]="tip.idMoTa" … />
   *     </app-tooltip>
   */
  readonly idMoTa = `tooltip-mo-ta-${++dem}`;

  /** Phần tử chiếu vào — đích của `aria-describedby`. */
  private readonly neo = signal<HTMLElement | null>(null);
  /** Đúng MỘT phần tử chiếu vào (§Hình dạng, ràng buộc 1). Sai thì không dựng gì. */
  private readonly noiDungHopLe = signal(false);

  private readonly chu = computed(() => this.text().trim());

  /** Tắt hẳn: nội dung rỗng, `disabled`, hoặc nội dung chiếu vào không đúng một phần tử. */
  protected readonly tat = computed(
    () => this.disabled() || this.chu() === '' || !this.noiDungHopLe(),
  );

  /** Hộp nổi hiện được thì phần tử mô tả cũng có mặt — và nó ở lại kể cả khi CSS tắt phần nhìn. */
  protected readonly moTaHienDuoc = computed(() => !this.tat());

  /** Xem `laComponent` ở `./tooltip-noi-dung` — nhánh mà lớp bọc THÔI ghi `aria-describedby`. */
  private readonly neoLaComponent = computed(() => {
    const neo = this.neo();
    return neo !== null && laComponent(neo);
  });

  constructor() {
    // Đọc DOM SAU khi render, không ở ngAfterViewInit: `noiDungHopLe` được template đọc, và ghi
    // một signal như vậy giữa lượt kiểm tra của Angular là ExpressionChangedAfterItHasBeenChecked.
    afterNextRender(() => this.chotNeo());

    // Thư viện đọc mọi tuỳ chọn qua `_tooltipOptions`; `setOption` là đường công khai để ghi vào
    // đó mà không phải mở từng input ra thành bề mặt công khai của lớp bọc.
    effect(() => {
      const tat = this.tat();
      this.prime.setOption({
        tooltipLabel: this.chu(),
        tooltipPosition: this.position(),
        disabled: tat,
        // Chỉ chuột mới chờ. Đường bàn phím gọi thẳng `show()`, không qua nhánh có độ trễ này.
        showDelay: this.delay(),
        tooltipStyleClass: this.lopHopNoi('chuot'),
        // Thang `--z-*` của Design là thang duy nhất — không để thang zIndex riêng của thư viện
        // chồng lên nó. `style.zIndex` nhận `var()` như mọi giá trị CSS khác.
        tooltipZIndex: 'var(--z-popover)',
      });
      if (tat) {
        this.prime.deactivate();
      }
    });

    effect(() => {
      const neo = this.neo();
      if (!neo) {
        return;
      }
      // Component Core tự dựng control bên trong thì CHÍNH NÓ ghi, qua input `describedBy` của
      // nó; lớp bọc THÔI ghi lên host của nó (§Accessibility, khoản 2 — lý do ở `laComponent`).
      if (this.neoLaComponent()) {
        return;
      }
      if (this.tat()) {
        this.renderer.removeAttribute(neo, 'aria-describedby');
      } else {
        this.renderer.setAttribute(neo, 'aria-describedby', this.idMoTa);
      }
    });

    // Khoản 4 của §Accessibility — nội dung lời cảnh báo và lý do nó phải tồn tại nằm ở
    // `canhBaoChuaNoiMoTa`. `afterRenderEffect` chứ không phải `effect`: phải đọc DOM SAU khi cả
    // lượt render xong, lúc component neo đã kịp ghi `describedBy` mà nơi gọi truyền vào.
    if (isDevMode()) {
      afterRenderEffect(() => {
        const neo = this.neo();
        if (!neo || !this.neoLaComponent() || this.tat()) {
          return;
        }
        canhBaoChuaNoiMoTa(neo, this.idMoTa);
      });
    }
  }

  /** Nối phép phán xét thuần ở `./tooltip-noi-dung` vào hai signal trạng thái của lớp bọc. */
  private chotNeo(): void {
    const host = this.el.nativeElement;
    const { neo, hopLe } = doNoiDungChieuVao(host);

    this.neo.set(neo);
    this.noiDungHopLe.set(hopLe);
    if (!hopLe && isDevMode()) {
      canhBaoNoiDungSai(host);
    }
  }

  /**
   * Lớp gắn lên hộp nổi qua `tooltipStyleClass`. Mang cả NGUỒN mở vì hai nguồn tắt theo hai điều
   * kiện khác nhau ở `styles/_thu-vien.scss`: `@media (hover: hover)` của §Trạng thái chỉ nói về
   * đường chuột, còn `$bp-sm` của §Responsive tắt cả hai.
   */
  private lopHopNoi(nguon: 'chuot' | 'ban-phim'): string {
    return `app-tooltip app-tooltip--${this.variant()} app-tooltip--${nguon}`;
  }

  protected khiReChuot(): void {
    this.prime.setOption({ tooltipStyleClass: this.lopHopNoi('chuot') });
  }

  /**
   * `focusin` nổi bọt, nên phần tử chiếu vào nhận focus bằng bàn phím thì host biết. Gọi thẳng
   * `show()` chứ không `activate()`: người dùng bàn phím đã chủ động đi tới đó, bắt họ chờ
   * `delay` là vô nghĩa (§Trạng thái `focus-visible`). `show()` tự bỏ qua khi nội dung rỗng hoặc
   * tuỳ chọn `disabled` đang bật, nên không cần canh lại ở đây.
   */
  protected khiNhanFocus(): void {
    this.prime.setOption({ tooltipStyleClass: this.lopHopNoi('ban-phim') });
    this.prime.show();
  }

  protected khiMatFocus(evt: FocusEvent): void {
    const toi = evt.relatedTarget;
    // Focus còn quanh quẩn bên trong phần tử neo thì chưa phải là rời đi.
    if (toi instanceof Node && this.el.nativeElement.contains(toi)) {
      return;
    }
    this.prime.deactivate();
  }

  /** Ẩn hộp nổi mà KHÔNG đụng tới focus — focus ở lại trên phần tử neo (§Accessibility). */
  protected khiEscape(): void {
    this.prime.deactivate();
  }
}
