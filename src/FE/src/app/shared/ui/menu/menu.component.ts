import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  input,
  output,
  viewChild,
} from '@angular/core';
import { MenuItem } from 'primeng/api';
import { Menu, MenuModule } from 'primeng/menu';

import type { UiMenuItem } from '../types';

/**
 * `Menu.show(event)` của PrimeNG neo overlay vào `event.currentTarget`. Nhưng effect của
 * `MenuComponent` chạy SAU khi sự kiện `click` đã phát xong (Angular không đồng bộ), và trình duyệt
 * đặt `currentTarget` về `null` khi phát xong — nên phải lấy neo từ `event.target` (không bị xoá),
 * quy về `<button>` chứa nó để căn theo cả nút chứ không theo icon bên trong.
 */
function phanTuNeo(evt: Event): Element | null {
  const goc = evt.target;
  return goc instanceof Element ? (goc.closest('button') ?? goc) : null;
}

/**
 * Chữ ký đầy đủ ở quy-uoc/fe-ui-conventions.md §9; khai MỘT lần ở `../types.ts`. Re-export ở đây để
 * nơi đang import `UiMenuItem` từ tệp này không phải đổi đường dẫn.
 */
export type { UiMenuItem } from '../types';

/**
 * Design/Components/Menu.md. Nơi DUY NHẤT trong ứng dụng được import `primeng/menu`. Định vị lớp
 * nổi khi trang cuộn hoặc chạm mép màn hình là hành vi "khó" — PrimeNG đã giải đúng
 * (Design/COMPONENTS.md §4).
 *
 * 🚧 F3 tối giản: biến thể `anchored` (duy nhất cần — hành động hàng của Design/Screens/11-vai-tro.md),
 * hai cỡ, `header`, mục khoá kèm lý do (`tooltip` có sẵn của PrimeNG `MenuItem`), vạch ngăn trước
 * nhóm `danger`. Biến thể `context`, `groups` nhiều nhóm, `selectedKey` (menu chọn giá trị — dùng
 * ở Topbar, không phải F3), `loading`/`error` (mục có tải mục theo yêu cầu) CHƯA dựng — danh sách
 * mục của F3 luôn tĩnh, không màn nào cần chờ tải.
 *
 * PrimeNG không có input `[visible]` khai báo cho popup — nó cần một sự kiện DOM để định vị
 * overlay (`Menu.show(event)`/`.hide()`). `anchorEvent` KHÔNG có trong API "dự kiến" của
 * Menu.md — đây là điểm khác biệt có chủ đích, ghi rõ chứ không giấu: trang cha truyền sự kiện
 * `click` gốc của nút đã mở menu (`(clicked)="$event ? ... : ..."` không đủ vì Button/IconButton
 * không phát `MouseEvent` gốc; page dùng phần tử `<button>`/`(click)` thuần cho nút mở menu khi
 * cần phối cùng component này — xem cách dùng ở vai-tro danh-sach.page.html).
 */
@Component({
  selector: 'app-menu',
  standalone: true,
  imports: [MenuModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './menu.component.html',
  styleUrl: './menu.component.scss',
})
export class MenuComponent {
  readonly items = input<readonly UiMenuItem[]>([]);
  readonly variant = input<'anchored' | 'context'>('anchored');
  readonly size = input<'sm' | 'md'>('md');
  readonly header = input<string | null>(null);
  readonly ariaLabel = input.required<string>();
  readonly open = input(false);
  /** Sự kiện gốc của phần tử đã mở menu — cần để PrimeNG định vị overlay. Xem ghi chú ở đầu file. */
  readonly anchorEvent = input<Event | null>(null);
  /**
   * Khác `null` → menu chọn MỘT giá trị (Menu.md §API): mọi mục `menuitemradio`, mục trùng khoá mang
   * `aria-checked="true"` và nhận focus khi menu mở; chọn lại đúng mục đó không phát `selected`.
   */
  readonly selectedKey = input<string | null>(null);

  /** Phát khoá của mục được chọn. KHÔNG phát khi mục đang khoá (Menu.md §API). */
  readonly selected = output<string>();
  readonly openChange = output<boolean>();

  private readonly primeMenu = viewChild.required<Menu>('primeMenu');

  protected readonly model = computed<MenuItem[]>(() => {
    const thuong = this.items().filter((m) => !m.danger);
    const nguyHiem = this.items().filter((m) => m.danger);
    const ds: MenuItem[] = [...thuong.map((m) => this.sangMenuItem(m))];
    if (thuong.length > 0 && nguyHiem.length > 0) {
      ds.push({ separator: true });
    }
    ds.push(...nguyHiem.map((m) => this.sangMenuItem(m)));
    return ds;
  });

  constructor() {
    effect(() => {
      const moTa = this.open();
      const evt = this.anchorEvent();
      if (moTa && evt) {
        const neo = phanTuNeo(evt);
        // Không có phần tử nào để neo thì không mở — PrimeNG sẽ ném khi căn vị trí với neo rỗng.
        if (neo) this.primeMenu().show({ currentTarget: neo });
      } else if (!moTa) {
        this.primeMenu().hide();
      }
    });
  }

  /**
   * PrimeNG dựng `<li role="menuitem">` cứng, không cho đổi vai trò theo mục. Menu chọn giá trị phải
   * là `menuitemradio` + `aria-checked` (Menu.md §Accessibility) nên vá lên DOM ngay khi overlay hiện
   * (`onShow` phát trước lúc PrimeNG focus danh sách), và đặt mục đang chọn làm mục có focus ảo.
   * Thứ tự `li` khớp thứ tự `model()`; separator là `li role="separator"`, không mang
   * `data-pc-section="item"`.
   * Hỏng khi nào: PrimeNG đổi `data-pc-section="item"` ở bản nâng cấp major.
   */
  protected onShow(): void {
    const khoaChon = this.selectedKey();
    if (khoaChon === null) {
      return;
    }
    const goc = this.primeMenu().containerViewChild()?.nativeElement as HTMLElement | undefined;
    if (!goc) {
      return;
    }
    const thuTu = [
      ...this.items().filter((m) => !m.danger),
      ...this.items().filter((m) => m.danger),
    ];
    const dsLi = Array.from(goc.querySelectorAll<HTMLElement>('li[data-pc-section="item"]'));
    dsLi.forEach((li, i) => {
      const dangChon = thuTu[i]?.key === khoaChon;
      li.setAttribute('role', 'menuitemradio');
      li.setAttribute('aria-checked', String(dangChon));
      if (dangChon) {
        this.primeMenu().focusedOptionIndex.set(li.id);
      }
    });
  }

  protected onHide(): void {
    this.openChange.emit(false);
  }

  private sangMenuItem(muc: UiMenuItem): MenuItem {
    return {
      label: muc.label,
      icon: muc.icon ? `pi pi-${muc.icon}` : undefined,
      disabled: muc.disabled ?? false,
      tooltip: muc.disabled ? (muc.disabledReason ?? undefined) : undefined,
      tooltipPosition: 'left',
      styleClass:
        [
          muc.danger ? 'app-menu__muc--nguy-hiem' : '',
          muc.key === this.selectedKey() ? 'app-menu__muc--chon' : '',
        ]
          .filter(Boolean)
          .join(' ') || undefined,
      command: () => {
        if (muc.disabled || muc.key === this.selectedKey()) {
          return;
        }
        this.selected.emit(muc.key);
      },
    };
  }
}
