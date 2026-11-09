import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterOutlet } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { filter, map } from 'rxjs';

import { AuthService } from '../../core/auth/auth.service';
import { maCommitNgan } from '../../core/config/phien-ban';
import { CORE_BRANDING } from '../../core/config/core-branding';
import { CORE_ROUTES } from '../../core/config/core-routes';
import { MenuNode } from '../../core/menu/menu.model';
import { MenuStore } from '../../core/menu/menu.store';
import { ThemePreference, ThemeService } from '../../core/theme/theme.service';
import { UnsavedChangesService } from '../../core/unsaved-changes/unsaved-changes.service';
import { FooterComponent } from '../../shared/components/footer/footer.component';
import { NavItem, SidebarComponent } from '../../shared/components/sidebar/sidebar.component';
import { TopbarComponent, TopbarUser } from '../../shared/components/topbar/topbar.component';
import { UiMenuItem } from '../../shared/ui/types';

/**
 * Design/Screens/00-khung-ung-dung.md. Tầng SMART — inject phiên và menu, truyền xuống
 * `Sidebar`/`Topbar` (dumb, `shared/components/`) qua `input()` (fe-architecture.md §2.3, không
 * ngoại lệ). Khung cao đúng `100dvh`, `main` là vùng cuộn dọc; màn danh sách tự lấp chiều cao `main`
 * (xem shell.component.scss). `Topbar` không mang tiêu đề trang. ADR-0037: `mode`/`sidebarMode` cố định `'expanded'` ở F2 — biến thể collapsed/drawer,
 * flyout menu tài khoản, nút theme và `LanguageSwitcher` hoãn tới F3 (bảng "đã có → còn thiếu" ở
 * Sidebar.md/Topbar.md). Hành vi CỐT LÕI vẫn đủ: menu lọc theo quyền từ server, `aria-current`
 * cho mục đang chọn, đăng xuất, liên kết hồ sơ.
 */
@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [RouterOutlet, TranslatePipe, FooterComponent, SidebarComponent, TopbarComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './shell.component.html',
  styleUrl: './shell.component.scss',
})
export class ShellComponent {
  protected readonly auth = inject(AuthService);
  protected readonly menu = inject(MenuStore);
  protected readonly branding = inject(CORE_BRANDING);
  private readonly router = inject(Router);
  private readonly routes = inject(CORE_ROUTES);
  private readonly translate = inject(TranslateService);
  private readonly unsavedChanges = inject(UnsavedChangesService);
  private readonly themeService = inject(ThemeService);

  /** Giá trị đang áp — Topbar chỉ nhận qua input, không tự đọc service (Topbar.md §API). */
  protected readonly theme = this.themeService.preference;

  /** Câu đọc lên qua vùng `aria-live` sau khi đổi giao diện (Topbar.md §Nút theme, Trợ năng). */
  protected readonly thongBaoTheme = signal('');

  protected readonly nguoiDung = computed(() => this.auth.nguoiDung());
  protected readonly namHienTai = new Date().getFullYear();
  /** 7 ký tự đầu mã commit lúc build; `null` → `Footer` ẩn phần phiên bản (Footer.md §Phiên bản hiển thị). */
  protected readonly maCommit = maCommitNgan();

  /** Cập nhật đúng lúc điều hướng xong — cùng nhịp với tiêu đề tuyến (CoreTitleStrategy). */
  protected readonly activeRoute = toSignal(
    this.router.events.pipe(
      filter((e): e is NavigationEnd => e instanceof NavigationEnd),
      map(() => this.router.url),
    ),
    { initialValue: this.router.url },
  );

  /** `main` đã cuộn khỏi đỉnh → Topbar vẽ bóng (Topbar.md §Kích thước). Đọc từ vùng cuộn thật. */
  protected readonly daCuon = signal(false);

  /** `Sidebar` nhận cây menu đã dịch — Sidebar.md §API: "label là chuỗi ĐÃ dịch". */
  protected readonly navItems = computed<readonly NavItem[]>(() =>
    this.menu.items().map((muc) => this.sangNavItem(muc)),
  );

  protected readonly nguoiDungTopbar = computed<TopbarUser | null>(() => {
    const nd = this.nguoiDung();
    return nd ? { fullName: nd.fullName, userName: nd.userName, email: nd.email } : null;
  });

  /**
   * Ngôn ngữ ưa thích v1 chỉ một mục — mục "Ngôn ngữ" của Topbar không cần ở đây (Topbar.md §API).
   * `computed`, không phải mảng dựng một lần: khởi động không chờ tệp dịch (fe-routing-guard.md
   * §3.6), nên dịch lúc dựng khung có thể ra khoá thô; `instant` đọc signal nên computed tính lại
   * khi bảng dịch về.
   */
  protected readonly taiKhoanMenuItems = computed<readonly UiMenuItem[]>(() => [
    { key: 'ho-so', label: this.translate.instant('hoSo.tieuDe'), icon: 'user' },
    {
      key: 'dang-xuat',
      label: this.translate.instant('xacThuc.dangXuat'),
      icon: 'sign-out',
      danger: true,
    },
  ]);

  constructor() {
    // Nạp menu MỘT LẦN lúc khung dựng — không nạp lại mỗi lần đổi route (docs/wiki-core/fe/07-auth-identity.md §2.1).
    // `authGuard` đã đảm bảo có người dùng tại đây.
    const id = this.auth.nguoiDung()?.id;
    if (id) {
      this.menu.lamMoi(id);
    }
  }

  protected khiChonMucTaiKhoan(key: string): void {
    if (key === 'dang-xuat') {
      void this.dangXuat();
      return;
    }
    if (key === 'ho-so') {
      void this.router.navigate(['/ho-so']);
    }
  }

  /** Áp + lưu là việc của ; xong mới đọc câu thông báo (Topbar.md §Nút theme). */
  protected khiDoiTheme(theme: ThemePreference): void {
    this.themeService.setPreference(theme);
    const khoa = {
      light: 'khung.theme.daChuyenSang',
      dark: 'khung.theme.daChuyenToi',
      system: 'khung.theme.daChuyenTheoHeThong',
    }[theme];
    this.thongBaoTheme.set(this.translate.instant(khoa));
  }

  protected khiCuon(evt: Event): void {
    this.daCuon.set((evt.target as HTMLElement).scrollTop > 0);
  }

  protected thuLaiTaiMenu(): void {
    const id = this.auth.nguoiDung()?.id;
    if (id) {
      this.menu.thuLai(id);
    }
  }

  /**
   * A17 (ADR-0040) — đăng xuất KHÔNG đi qua Router trước khi gửi request nên `canDeactivate`
   * (`unsavedChangesGuard`) không tự chạy cho hành động này; gọi thẳng `xinRoiTrang()` để đi
   * qua ĐÚNG một hộp hỏi dùng chung, hỏi TRƯỚC khi gửi request (fe-routing-guard.md §4.1). "Rời đi"
   * không gỡ đăng ký thay đổi chưa lưu: đăng xuất thành công thì `dangXuat()` đã dọn phiên trước khi
   * điều hướng nên `unsavedChangesGuard` cho rời mà không hỏi lại; đăng xuất hỏng thì màn ở lại với
   * lớp bảo vệ còn nguyên (D6 §4). Đổi
   * ngôn ngữ (F3 — `Topbar.languageChangeRequested` chưa render ở F2, xem Design/Screens/00)
   * phải gọi qua cùng `xinRoiTrang()` này khi được dựng, không mở đường riêng.
   */
  private async dangXuat(): Promise<void> {
    const roiDuoc = await this.unsavedChanges.xinRoiTrang();
    if (!roiDuoc) {
      return;
    }
    this.auth.dangXuat().subscribe({
      next: () => void this.router.navigate([this.routes.dangNhap]),
      error: () => undefined, // interceptor đã toast; ở lại màn, thử lại được (D6 §4)
    });
  }

  private sangNavItem(muc: MenuNode): NavItem {
    return {
      id: muc.id,
      label: this.translate.instant(muc.labelKey),
      icon: tenIconTran(muc.icon),
      route: muc.route ?? undefined,
      children:
        muc.children.length > 0 ? muc.children.map((con) => this.sangNavItem(con)) : undefined,
    };
  }
}

/**
 * Máy chủ trả icon mang sẵn tiền tố `pi-` (contracts/meta-menu.md §1.3); input `icon` của mọi
 * component nhận TÊN TRẦN (Design/Icons.md §5) — shell là chỗ cắt để Sidebar ghép lớp như các
 * component khác. Nhận cả hai dạng nên đổi hợp đồng sau này không vỡ.
 */
export function tenIconTran(icon: string | null | undefined): string | undefined {
  const ten = icon?.trim().replace(/^(pi\s+)?pi-/, '');
  return ten ? ten : undefined;
}
