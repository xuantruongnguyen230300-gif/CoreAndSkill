import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  inject,
  signal,
} from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { finalize } from 'rxjs';

import { AuthService } from '../../../../../core/auth/auth.service';
import { ApiFailureError } from '../../../../../core/http/api-result.model';
import { dichLoiChoMan } from '../../../../../core/http/dich-loi';
import { BadgeComponent } from '../../../../../shared/components/badge/badge.component';
import { CardComponent } from '../../../../../shared/components/card/card.component';
import { ConfirmDialogComponent } from '../../../../../shared/components/confirm-dialog/confirm-dialog.component';
import { EmptyStateComponent } from '../../../../../shared/components/empty-state/empty-state.component';
import { HasPermissionDirective } from '../../../../../shared/directives/has-permission.directive';
import { NoticeBannerComponent } from '../../../../../shared/components/notice-banner/notice-banner.component';
import { PageHeaderComponent } from '../../../../../shared/components/page-header/page-header.component';
import { ButtonComponent } from '../../../../../shared/components/button/button.component';
import { NguoiDungService } from '../../services/nguoi-dung.service';
import { GanVaiTroChonStore } from '../../state/gan-vai-tro-chon.store';
import { HopChiTietNguoiDungStore } from '../../state/hop-chi-tiet-nguoi-dung.store';
import { NguoiDung } from '../../models/nguoi-dung.model';
import { HopDatLaiMatKhauComponent } from '../../components/hop-dat-lai-mat-khau/hop-dat-lai-mat-khau.component';
import { HopGanVaiTroComponent } from '../../components/hop-gan-vai-tro/hop-gan-vai-tro.component';
import { HopSuaNguoiDungComponent } from '../../components/hop-sua-nguoi-dung/hop-sua-nguoi-dung.component';
import { TheThongTinNguoiDungComponent } from '../../components/the-thong-tin-nguoi-dung/the-thong-tin-nguoi-dung.component';
import { TheVaiTroNguoiDungComponent } from '../../components/the-vai-tro-nguoi-dung/the-vai-tro-nguoi-dung.component';

/** Design/Screens/10-nguoi-dung.md §Chi tiết. Guard: permissionGuard('core.user.read'). */
@Component({
  selector: 'app-chi-tiet-nguoi-dung',
  standalone: true,
  imports: [
    NgTemplateOutlet,
    TranslatePipe,
    BadgeComponent,
    CardComponent,
    ConfirmDialogComponent,
    EmptyStateComponent,
    HasPermissionDirective,
    NoticeBannerComponent,
    PageHeaderComponent,
    ButtonComponent,
    HopDatLaiMatKhauComponent,
    HopGanVaiTroComponent,
    HopSuaNguoiDungComponent,
    TheThongTinNguoiDungComponent,
    TheVaiTroNguoiDungComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [GanVaiTroChonStore, HopChiTietNguoiDungStore],
  templateUrl: './chi-tiet-nguoi-dung.page.html',
})
export class ChiTietNguoiDungPage {
  private readonly service = inject(NguoiDungService);
  protected readonly auth = inject(AuthService);
  private readonly translate = inject(TranslateService);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);

  private readonly id = this.route.snapshot.paramMap.get('id') ?? '';

  protected readonly dangTai = signal(true);
  protected readonly khongTimThay = signal(false);
  /**
   * Tải chi tiết hỏng (lý do khác `NOT_FOUND`) → `Card` `error` cho cả hai thẻ: tiêu đề cố định LUÔN có
   * khi khác `null`; `than` là câu `dichLoiChoMan` — `null` với lớp lỗi xuyên suốt, interceptor đã
   * toast nó kèm traceId (Design/Screens/10-nguoi-dung.md §Trạng thái màn chi tiết).
   */
  protected readonly loiTai = signal<{ readonly than: string | null } | null>(null);
  protected readonly nguoiDung = signal<NguoiDung | null>(null);

  protected readonly laChinhMinh = computed(() => this.auth.nguoiDung()?.id === this.id);
  protected readonly coQuyenGanVaiTro = computed(
    () => this.auth.coQuyen('core.user.role.assign') && this.auth.coQuyen('core.role.read'),
  );

  constructor() {
    this.taiChiTiet();
  }

  // `xong` của store gọi lại hàm này sau mỗi lần ghi: lần ghi xong khi màn đã huỷ thì
  // `takeUntilDestroyed` hoàn tất ngay, không bắn GET (fe-api-client.md §6.1).
  private taiChiTiet(): void {
    this.dangTai.set(true);
    this.khongTimThay.set(false);
    this.loiTai.set(null);
    this.service
      .chiTiet(this.id)
      .pipe(
        finalize(() => this.dangTai.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (nd) => this.nguoiDung.set(nd),
        error: (err: unknown) => {
          if (err instanceof ApiFailureError && err.body?.error.code === 'CORE.USER.NOT_FOUND') {
            this.khongTimThay.set(true);
            return;
          }
          this.loiTai.set({ than: cauLoiTai(err, this.translate) });
        },
      });
  }

  protected thuLaiTaiChiTiet(): void {
    this.taiChiTiet();
  }

  // ---- Hộp thoại: trạng thái chung và mọi luồng ghi nằm ở HopChiTietNguoiDungStore; page chỉ nối ----
  protected readonly hop = inject(HopChiTietNguoiDungStore);

  protected thuLaiSauXungDot(): void {
    this.hop.dongTatCa();
    this.taiChiTiet();
  }

  protected moHopSua(): void {
    const nd = this.nguoiDung();
    if (nd) this.hop.moHopSua(nd);
  }

  protected guiSua(): void {
    const nd = this.nguoiDung();
    if (nd) this.hop.guiSua(nd, () => this.taiChiTiet());
  }

  protected guiDatLai(): void {
    const nd = this.nguoiDung();
    if (nd) this.hop.guiDatLai(nd, () => this.taiChiTiet());
  }

  // ---- Gán vai trò ----
  protected readonly ganVaiTro = inject(GanVaiTroChonStore);

  protected moHopGanVaiTro(): void {
    const nd = this.nguoiDung();
    if (!nd) return;
    this.ganVaiTro.moHop(nd.roles, this.laChinhMinh());
    this.hop.moHopGanVaiTro();
  }

  protected guiGanVaiTro(): void {
    const nd = this.nguoiDung();
    if (nd) this.hop.yeuCauGanVaiTro(nd, this.ganVaiTro.tapDich(), () => this.taiChiTiet());
  }

  // ---- Khoá / mở khoá ----
  protected xacNhanKhoa(): void {
    const nd = this.nguoiDung();
    if (nd) this.hop.xacNhanKhoa(nd, () => this.taiChiTiet());
  }

  protected moKhoa(): void {
    const nd = this.nguoiDung();
    if (nd) this.hop.moKhoa(nd, () => this.taiChiTiet());
  }
}

/** Thân khối lỗi tải chi tiết: `null` với lớp lỗi xuyên suốt (đã toast), còn lại câu dịch theo mã. */
function cauLoiTai(err: unknown, translate: TranslateService): string | null {
  return err instanceof ApiFailureError
    ? dichLoiChoMan(translate, err)
    : translate.instant('loi.CORE.CLIENT.NO_CONNECTION');
}
