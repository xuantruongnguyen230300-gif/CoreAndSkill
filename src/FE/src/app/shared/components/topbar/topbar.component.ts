import { ChangeDetectionStrategy, Component, computed, input, output, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslatePipe } from '@ngx-translate/core';
import { Observable } from 'rxjs';

import type { LanguageOption } from '../../../core/config/core-i18n';
import { TRUY_VAN_DUOI_BP_XS } from '../../../core/theme/diem-ngat';
import { MenuComponent } from '../../ui/menu/menu.component';
import type { UiMenuItem } from '../../ui/types';
import { IconButtonComponent } from '../icon-button/icon-button.component';

export interface TopbarUser {
  readonly fullName: string;
  readonly userName: string;
  readonly email: string | null;
}

export type TopbarTheme = 'light' | 'dark' | 'system';

/** Tên icon TRẦN theo giá trị đang áp — Design/Icons.md §5. */
/** Ba giá trị người dùng chọn được — thứ tự mục Menu theme: Sáng · Tối · Theo hệ điều hành. */
const ICON_THEME: Record<TopbarTheme, string> = {
  light: 'sun',
  dark: 'moon',
  system: 'desktop',
};

const KHOA_MUC_THEME = '__theme';

function truyVanMedia(truyVan: string): Observable<boolean> {
  return new Observable<boolean>((nguoiNhan) => {
    const mql = window.matchMedia(truyVan);
    nguoiNhan.next(mql.matches);
    const khiDoi = (e: MediaQueryListEvent): void => nguoiNhan.next(e.matches);
    mql.addEventListener('change', khiDoi);
    return () => mql.removeEventListener('change', khiDoi);
  });
}

/**
 * Design/Components/Topbar.md. 🚧 Minimal F2 (ADR-0037): khu tài khoản là MỘT nút mở menu nổi;
 * mục "Ngôn ngữ" và hamburger/nút thu-gọn Sidebar (gắn với `sidebarMode` khác
 * `'expanded'`) CHƯA render — xem bảng "đã có → còn thiếu" ở Topbar.md. Nút theme + Menu theme ĐÃ
 * dựng (Q61, 2026-10-06). Component DUMB: không tự đọc phiên, không tự gọi router, không đụng
 * `localStorage` (COMPONENTS.md §5) — `platform/shell` truyền dữ liệu qua `input()`, nhận lại qua
 * `output()`.
 */
@Component({
  selector: 'app-topbar',
  standalone: true,
  imports: [TranslatePipe, IconButtonComponent, MenuComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './topbar.component.html',
  styleUrl: './topbar.component.scss',
})
export class TopbarComponent {
  readonly user = input<TopbarUser | null>(null);
  readonly tenantName = input('');
  readonly menuItems = input<readonly UiMenuItem[]>([]);
  readonly sidebarMode = input<'expanded' | 'collapsed' | 'drawer'>('expanded');
  readonly drawerOpen = input(false);
  readonly state = input<'idle' | 'loading' | 'error'>('idle');
  readonly languages = input<readonly LanguageOption[]>([]);
  readonly currentLanguage = input<string | null>(null);
  readonly pendingLanguage = input<string | null>(null);
  readonly theme = input<TopbarTheme>('light');
  /** `main` đã cuộn khỏi đỉnh → bóng `--shadow-2` (Topbar.md §Kích thước). `Topbar` không tự đọc cuộn. */
  readonly scrolled = input(false);

  readonly menuToggled = output<void>();
  /** `key` của mục trong menu tài khoản — `platform/shell` diễn giải, Topbar không biết route. */
  readonly menuItemSelected = output<string>();
  readonly languageChangeRequested = output<string>();
  /** Giá trị người dùng CHỌN; không phát khi chọn lại giá trị đang áp. */
  readonly themeChangeRequested = output<TopbarTheme>();

  protected readonly chuCaiDau = computed(() =>
    (this.user()?.fullName.trim().charAt(0) ?? '?').toUpperCase(),
  );

  protected readonly iconTheme = computed(() => ICON_THEME[this.theme()]);

  /** Dưới `$bp-xs` (480px) nút theme ẩn, mục "Giao diện" vào menu tài khoản (Topbar.md §Responsive). */
  protected readonly duoiXs = toSignal(truyVanMedia(TRUY_VAN_DUOI_BP_XS), {
    initialValue: false,
  });

  protected readonly menuThemeMo = signal(false);
  protected readonly suKienNeo = signal<Event | null>(null);
  protected readonly menuTaiKhoanMo = signal(false);
  protected readonly suKienNeoTaiKhoan = signal<Event | null>(null);

  /**
   * Danh sách mục menu tài khoản khi dưới `$bp-xs`: thêm mục "Giao diện" đứng trước các mục thường
   * (Topbar.md §API). Gọi từ template mỗi lần kiểm tra thay đổi nên nhớ kết quả theo đối số — trả
   * mảng mới mỗi lần sẽ làm `Menu` dựng lại model liên tục khi đang mở.
   */
  private boNhoXs: {
    nhan: string;
    icon: string;
    goc: readonly UiMenuItem[];
    ketQua: readonly UiMenuItem[];
  } | null = null;

  protected mucTaiKhoanXs(
    nhan: string,
    icon: string,
    goc: readonly UiMenuItem[],
  ): readonly UiMenuItem[] {
    const nho = this.boNhoXs;
    if (nho && nho.nhan === nhan && nho.icon === icon && nho.goc === goc) {
      return nho.ketQua;
    }
    const ketQua = [{ key: KHOA_MUC_THEME, label: nhan, icon }, ...goc];
    this.boNhoXs = { nhan, icon, goc, ketQua };
    return ketQua;
  }

  /** Nút đã mở menu — để trả focus về đó khi chọn bằng chuột (Escape/Enter PrimeNG tự trả). */
  private nutNeo: HTMLElement | null = null;

  /** Dùng chung cho nút trên thanh và mục "Giao diện" của khu tài khoản ở khổ hẹp. */
  protected moMenuTheme(evt: Event): void {
    const goc = evt.target;
    this.nutNeo = goc instanceof Element ? (goc.closest<HTMLElement>('button') ?? null) : null;
    this.suKienNeo.set(evt);
    this.menuThemeMo.set(true);
  }

  protected dongMenuTheme(): void {
    this.menuThemeMo.set(false);
  }

  protected moMenuTaiKhoan(evt: Event): void {
    const goc = evt.target;
    this.nutNeo = goc instanceof Element ? (goc.closest<HTMLElement>('button') ?? null) : null;
    this.suKienNeoTaiKhoan.set(evt);
    this.menuTaiKhoanMo.set(true);
  }

  /** Đóng bằng Escape, chọn mục hay bấm ra ngoài đều trả focus về nút mở (Topbar.md §Accessibility). */
  protected dongMenuTaiKhoan(): void {
    const dangChuyenSangTheme = this.menuThemeMo();
    this.menuTaiKhoanMo.set(false);
    if (!dangChuyenSangTheme) {
      this.nutNeo?.focus();
    }
  }

  protected chonMucTaiKhoan(key: string): void {
    if (key === KHOA_MUC_THEME) {
      // Hai lớp nổi nối tiếp, không lồng: menu theme neo vào CHÍNH nút tài khoản (Topbar.md §API).
      this.suKienNeo.set(this.suKienNeoTaiKhoan());
      this.menuTaiKhoanMo.set(false);
      this.menuThemeMo.set(true);
      return;
    }
    this.menuItemSelected.emit(key);
  }

  protected chonTheme(key: string): void {
    const gia = key === 'light' || key === 'dark' || key === 'system' ? key : null;
    this.nutNeo?.focus();
    if (gia && gia !== this.theme()) {
      this.themeChangeRequested.emit(gia);
    }
  }
}
