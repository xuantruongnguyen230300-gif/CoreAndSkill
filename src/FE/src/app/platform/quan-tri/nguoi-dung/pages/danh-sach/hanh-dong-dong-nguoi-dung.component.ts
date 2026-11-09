import {
  ChangeDetectionStrategy,
  Component,
  TemplateRef,
  computed,
  inject,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';

import { AuthService } from '../../../../../core/auth/auth.service';
import { ConfirmDialogComponent } from '../../../../../shared/components/confirm-dialog/confirm-dialog.component';
import { IconButtonComponent } from '../../../../../shared/components/icon-button/icon-button.component';
import { MenuComponent, UiMenuItem } from '../../../../../shared/ui/menu/menu.component';
import { RowActionsContext } from '../../../../../shared/ui/data-table/data-table.component';
import { HopDatLaiMatKhauComponent } from '../../components/hop-dat-lai-mat-khau/hop-dat-lai-mat-khau.component';
import { HopGanVaiTroComponent } from '../../components/hop-gan-vai-tro/hop-gan-vai-tro.component';
import { HopSuaNguoiDungComponent } from '../../components/hop-sua-nguoi-dung/hop-sua-nguoi-dung.component';
import { NguoiDung } from '../../models/nguoi-dung.model';
import { GanVaiTroChonStore } from '../../state/gan-vai-tro-chon.store';
import { HopChiTietNguoiDungStore } from '../../state/hop-chi-tiet-nguoi-dung.store';

/**
 * Cột hành động của danh sách người dùng (Design/Screens/10-nguoi-dung.md §"Hành động dòng"): nút
 * Sửa, nút ⋯ mở Menu (Khoá/Mở khoá, Đặt lại mật khẩu, Gán vai trò) và MỌI hộp thoại của chúng —
 * dùng lại đúng hộp, store và câu chữ của màn chi tiết. Dữ liệu và `version` lấy từ DÒNG, không
 * `GET` chi tiết trước. Là component THÔNG MINH (inject store) nên nằm cạnh trang chứ không ở
 * `components/` (F33); trang chỉ cắm `mauHanhDong()` vào `rowActionsTemplate` của DataTable và tải
 * lại danh sách khi `daGhi` phát.
 */
@Component({
  selector: 'app-hanh-dong-dong-nguoi-dung',
  standalone: true,
  imports: [
    TranslatePipe,
    ConfirmDialogComponent,
    IconButtonComponent,
    MenuComponent,
    HopDatLaiMatKhauComponent,
    HopGanVaiTroComponent,
    HopSuaNguoiDungComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [GanVaiTroChonStore, HopChiTietNguoiDungStore],
  templateUrl: './hanh-dong-dong-nguoi-dung.component.html',
})
export class HanhDongDongNguoiDungComponent {
  private readonly auth = inject(AuthService);
  private readonly translate = inject(TranslateService);
  protected readonly hop = inject(HopChiTietNguoiDungStore);
  protected readonly ganVaiTro = inject(GanVaiTroChonStore);

  /** Một thao tác ghi xong (hoặc cần lấy `version` mới) — trang tải lại danh sách, giữ trang/lọc. */
  readonly daGhi = output<void>();

  private readonly mau = viewChild.required<TemplateRef<RowActionsContext<NguoiDung>>>('mau');

  protected readonly coSua = computed(() => this.auth.coQuyen('core.user.write'));
  private readonly coKhoa = computed(() => this.auth.coQuyen('core.user.lock'));
  private readonly coDatLai = computed(() => this.auth.coQuyen('core.user.reset-password'));
  private readonly coGanVaiTro = computed(
    () => this.auth.coQuyen('core.user.role.assign') && this.auth.coQuyen('core.role.read'),
  );

  /** `null` khi người xem không có khoá nào của bảng "Hành động dòng" → cột không vẽ, kể cả `th`. */
  readonly mauHanhDong = computed(() =>
    this.coSua() || this.coKhoa() || this.coDatLai() || this.coGanVaiTro() ? this.mau() : null,
  );

  /** Dòng đang được thao tác — các hộp đọc `tenDangNhap` và `version` từ đây. */
  protected readonly dong = signal<NguoiDung | null>(null);
  protected readonly tenDangNhap = computed(() => this.dong()?.userName ?? '');

  // ---- Menu ⋯ theo dòng ----
  protected readonly mucMoMenuId = signal<string | null>(null);
  protected readonly suKienMoMenu = signal<Event | null>(null);

  protected laChinhMinh(nd: NguoiDung): boolean {
    return this.auth.nguoiDung()?.id === nd.id;
  }

  /** `compact` (dạng thẻ): một nút ⋯ gom cả "Sửa", đứng đầu. */
  protected mucMenu(nd: NguoiDung, compact: boolean): UiMenuItem[] {
    const tuMinh = this.laChinhMinh(nd);
    const t = (k: string): string => this.translate.instant(k);
    const ds: UiMenuItem[] = [];
    if (compact && this.coSua()) {
      ds.push({ key: 'sua', label: t('nguoiDung.hanhDong.sua'), icon: 'pencil' });
    }
    if (this.coKhoa() && nd.isLocked) {
      ds.push({ key: 'mo-khoa', label: t('nguoiDung.hanhDong.moKhoa'), icon: 'lock-open' });
    } else if (this.coKhoa() && !tuMinh) {
      ds.push({ key: 'khoa', label: t('nguoiDung.hanhDong.khoa'), icon: 'lock' });
    }
    if (this.coDatLai() && !tuMinh) {
      ds.push({ key: 'dat-lai', label: t('nguoiDung.hanhDong.datLaiMatKhau'), icon: 'key' });
    }
    if (this.coGanVaiTro()) {
      ds.push({ key: 'gan-vai-tro', label: t('nguoiDung.hanhDong.ganVaiTro'), icon: 'users' });
    }
    return ds;
  }

  protected onMoMenu(evt: Event, nd: NguoiDung): void {
    this.suKienMoMenu.set(evt);
    this.mucMoMenuId.set(this.mucMoMenuId() === nd.id ? null : nd.id);
  }

  /** Chỉ đóng khi menu vừa ẩn ĐÚNG là menu đang ghi nhận là mở (cùng lý do ở màn vai trò). */
  protected onDongMenu(id: string): void {
    if (this.mucMoMenuId() === id) this.mucMoMenuId.set(null);
  }

  protected onChonMenu(khoa: string, nd: NguoiDung): void {
    this.mucMoMenuId.set(null);
    if (khoa === 'sua') this.moSua(nd);
    else if (khoa === 'khoa') this.moXacNhanKhoa(nd);
    else if (khoa === 'mo-khoa') this.moKhoa(nd);
    else if (khoa === 'dat-lai') this.moDatLai(nd);
    else if (khoa === 'gan-vai-tro') this.moGanVaiTro(nd);
  }

  // ---- Mở hộp / ghi — luồng ở HopChiTietNguoiDungStore, `xong` = tải lại danh sách ----
  protected moSua(nd: NguoiDung): void {
    this.dong.set(nd);
    this.hop.moHopSua(nd);
  }

  private moDatLai(nd: NguoiDung): void {
    this.dong.set(nd);
    this.hop.moHopDatLai();
  }

  private moGanVaiTro(nd: NguoiDung): void {
    this.dong.set(nd);
    this.ganVaiTro.moHop(nd.roles, this.laChinhMinh(nd));
    this.hop.moHopGanVaiTro();
  }

  private moXacNhanKhoa(nd: NguoiDung): void {
    this.dong.set(nd);
    this.hop.moXacNhanKhoa();
  }

  private moKhoa(nd: NguoiDung): void {
    this.dong.set(nd);
    this.hop.moKhoa(nd, () => this.daGhi.emit());
  }

  private readonly xong = (): void => this.daGhi.emit();

  protected guiSua(): void {
    const nd = this.dong();
    if (nd) this.hop.guiSua(nd, this.xong);
  }

  protected guiDatLai(): void {
    const nd = this.dong();
    if (nd) this.hop.guiDatLai(nd, this.xong);
  }

  protected guiGanVaiTro(): void {
    const nd = this.dong();
    if (nd) this.hop.yeuCauGanVaiTro(nd, this.ganVaiTro.tapDich(), this.xong);
  }

  protected xacNhanKhoa(): void {
    const nd = this.dong();
    if (nd) this.hop.xacNhanKhoa(nd, this.xong);
  }

  /** Banner xung đột: đóng hộp và tải lại danh sách để dòng mang `version` mới (Screens/10 §Hành động dòng). */
  protected thuLaiSauXungDot(): void {
    this.hop.dongTatCa();
    this.daGhi.emit();
  }

  /** Hộp đang hiện lỗi (vd. NOT_FOUND) mà bị đóng thì dòng có thể đã đổi/mất → tải lại danh sách. */
  protected dongHop(): void {
    const coLoi = this.hop.loiHop() !== null;
    this.hop.dong();
    if (coLoi) this.daGhi.emit();
  }
}
