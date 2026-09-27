import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  Injector,
  TemplateRef,
  afterNextRender,
  computed,
  effect,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { finalize } from 'rxjs';

import { AuthService } from '../../../../../core/auth/auth.service';
import { ApiFailureError } from '../../../../../core/http/api-result.model';
import { ListStateStore } from '../../../../../core/list/list-state.store';
import { ToastService } from '../../../../../core/toast/toast.service';
import { BadgeComponent } from '../../../../../shared/components/badge/badge.component';
import { ConfirmDialogComponent } from '../../../../../shared/components/confirm-dialog/confirm-dialog.component';
import { EmptyStateComponent } from '../../../../../shared/components/empty-state/empty-state.component';
import { FormRowComponent } from '../../../../../shared/components/form-row/form-row.component';
import { fieldErrorsText } from '../../../../../shared/forms/field-errors-text';
import { HasPermissionDirective } from '../../../../../shared/directives/has-permission.directive';
import { NoticeBannerComponent } from '../../../../../shared/components/notice-banner/notice-banner.component';
import { PageHeaderComponent } from '../../../../../shared/components/page-header/page-header.component';
import { SkeletonLoaderComponent } from '../../../../../shared/components/skeleton-loader/skeleton-loader.component';
import {
  ToolbarChip,
  ToolbarComponent,
} from '../../../../../shared/components/toolbar/toolbar.component';
import { ButtonComponent } from '../../../../../shared/components/button/button.component';
import {
  DataColumnDef,
  DataTableComponent,
} from '../../../../../shared/ui/data-table/data-table.component';
import { DialogComponent } from '../../../../../shared/ui/dialog/dialog.component';
import { IconButtonComponent } from '../../../../../shared/components/icon-button/icon-button.component';
import { InputComponent } from '../../../../../shared/components/input/input.component';
import { MenuComponent, UiMenuItem } from '../../../../../shared/ui/menu/menu.component';
import { CORE_SCREEN_EXT } from '../../../../config/core-screen-ext';
import { CotCore, kiemCotMoRong, sangCotDataTable } from '../../../../config/cot-mo-rong';
import { VaiTroService } from '../../services/vai-tro.service';
import { VaiTro } from '../../models/vai-tro.model';
import { HopVaiTroStore } from '../../state/hop-vai-tro.store';

/** Khoá cột Core của màn — tập đưa vào phép kiểm khoá trùng (fe-architecture.md §2.7 luật 6). */
const KHOA_COT_CORE = ['name', 'isSystem', 'userCount', 'createdAt', 'hanhDong'] as const;
type KhoaCotCore = (typeof KHOA_COT_CORE)[number];

/** Design/Screens/11-vai-tro.md. Guard: permissionGuard('core.role.read') (vai-tro.routes.ts). */
@Component({
  selector: 'app-danh-sach-vai-tro',
  standalone: true,
  imports: [
    RouterLink,
    ReactiveFormsModule,
    TranslatePipe,
    DatePipe,
    BadgeComponent,
    ConfirmDialogComponent,
    EmptyStateComponent,
    FormRowComponent,
    HasPermissionDirective,
    NoticeBannerComponent,
    PageHeaderComponent,
    SkeletonLoaderComponent,
    ToolbarComponent,
    ButtonComponent,
    DataTableComponent,
    DialogComponent,
    IconButtonComponent,
    InputComponent,
    MenuComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [ListStateStore, HopVaiTroStore],
  templateUrl: './danh-sach-vai-tro.page.html',
  styleUrl: './danh-sach-vai-tro.page.scss',
})
export class DanhSachVaiTroPage {
  private readonly service = inject(VaiTroService);
  protected readonly auth = inject(AuthService);
  private readonly translate = inject(TranslateService);
  private readonly toast = inject(ToastService);
  /**
   * Seam CORE_SCREEN_EXT khoá 'roles' (fe-architecture.md §2.7): không khai ⇒ đúng bản gốc; khoá
   * trùng ⇒ ném ngay lúc dựng màn (luật 6), không đợi `columns` tính.
   */
  private readonly cotMoRong = kiemCotMoRong(
    'roles',
    KHOA_COT_CORE,
    inject(CORE_SCREEN_EXT, { optional: true })?.roles?.columns,
  );

  protected readonly list = inject<ListStateStore<VaiTro>>(ListStateStore).connect((q) =>
    this.service.danhSach(q),
  );

  /**
   * Vai trò vừa đổi tên, theo `id`, kèm `version` nó thay. Dòng của lần tải cũ còn mang `version` cũ
   * thì hiện bản đã đổi (roles.md §3: FE thay ngay, không chờ GET); lần tải sau mang `version` khác
   * nên bản ghi đè tự hết tác dụng — không có gì phải dọn.
   */
  private readonly daDoiTen = signal<ReadonlyMap<string, { cu: string; moi: VaiTro }>>(new Map());
  protected readonly rows = computed<readonly VaiTro[]>(() => {
    const doi = this.daDoiTen();
    return this.list.rows().map((r) => {
      const d = doi.get(r.id);
      return d && d.cu === r.version ? d.moi : r;
    });
  });

  protected readonly chipDonVi = computed<readonly ToolbarChip[]>(() => {
    const ten = this.auth.nguoiDung()?.tenantName;
    return ten
      ? [
          {
            key: 'don-vi',
            label: this.translate.instant('chung.loc.donViHienHanh', { tenDonVi: ten }),
            value: null,
            removable: false,
            lockReason: this.translate.instant('chung.loc.lyDoDonVi'),
          },
        ]
      : [];
  });

  // ---- Cột DataTable ----
  private readonly colTen = viewChild.required<TemplateRef<{ $implicit: VaiTro }>>('colTen');
  private readonly colLoai = viewChild.required<TemplateRef<{ $implicit: VaiTro }>>('colLoai');
  private readonly colSoNguoiDung =
    viewChild.required<TemplateRef<{ $implicit: VaiTro }>>('colSoNguoiDung');
  private readonly colNgayTao =
    viewChild.required<TemplateRef<{ $implicit: VaiTro }>>('colNgayTao');
  private readonly colHanhDong =
    viewChild.required<TemplateRef<{ $implicit: VaiTro }>>('colHanhDong');

  protected readonly columns = computed<DataColumnDef<VaiTro>[]>(() => {
    const cotDuLieu: CotCore<VaiTro, KhoaCotCore>[] = [
      {
        key: 'name',
        header: this.translate.instant('vaiTro.cot.ten'),
        sortable: true,
        priority: 'high',
        cell: this.colTen(),
      },
      { key: 'isSystem', header: this.translate.instant('vaiTro.cot.loai'), cell: this.colLoai() },
      {
        key: 'userCount',
        header: this.translate.instant('vaiTro.cot.soNguoiDung'),
        align: 'end',
        cell: this.colSoNguoiDung(),
      },
      {
        key: 'createdAt',
        header: this.translate.instant('vaiTro.cot.ngayTao'),
        sortable: true,
        priority: 'low',
        cell: this.colNgayTao(),
      },
    ];
    const cotHanhDong: CotCore<VaiTro, KhoaCotCore>[] = this.auth.coQuyen('core.role.write')
      ? [
          {
            key: 'hanhDong',
            header: this.translate.instant('vaiTro.cot.hanhDong'),
            cell: this.colHanhDong(),
          },
        ]
      : [];
    // Cột của dự án nối sau cột dữ liệu của Core; cột hành động vẫn đứng cuối (DataTable.md).
    return [
      ...cotDuLieu,
      ...sangCotDataTable(this.cotMoRong, (k) => this.translate.instant(k)),
      ...cotHanhDong,
    ];
  });

  // ---- Menu hành động theo hàng ----
  protected readonly mucMoMenuId = signal<string | null>(null);
  protected readonly suKienMoMenu = signal<Event | null>(null);

  protected mucMenu(vt: VaiTro): UiMenuItem[] {
    const lyDoHeThong = this.translate.instant('vaiTro.menu.lyDoHeThong');
    const lyDoDangDung = this.translate.instant('vaiTro.menu.lyDoDangDung', {
      soNguoi: vt.userCount,
    });
    return [
      {
        key: 'doi-ten',
        label: this.translate.instant('vaiTro.menu.doiTen'),
        icon: 'pencil',
        disabled: vt.isSystem,
        disabledReason: vt.isSystem ? lyDoHeThong : undefined,
      },
      {
        key: 'xoa',
        label: this.translate.instant('vaiTro.menu.xoa'),
        icon: 'trash',
        danger: true,
        disabled: vt.isSystem || vt.userCount > 0,
        disabledReason: vt.isSystem ? lyDoHeThong : vt.userCount > 0 ? lyDoDangDung : undefined,
      },
    ];
  }

  protected onMoMenu(evt: Event, vt: VaiTro): void {
    this.suKienMoMenu.set(evt);
    this.mucMoMenuId.set(this.mucMoMenuId() === vt.id ? null : vt.id);
  }

  /**
   * `p-menu` phát `openChange(false)` khi hoạt ảnh ẩn BẮT ĐẦU — có thể tới SAU khi nút hàng khác
   * đã đặt `mucMoMenuId`. Chỉ đóng khi menu vừa ẩn ĐÚNG là menu đang được ghi nhận là mở.
   */
  protected onDongMenu(id: string): void {
    if (this.mucMoMenuId() !== id) return;
    this.mucMoMenuId.set(null);
  }

  protected onChonMenu(khoa: string, vt: VaiTro): void {
    this.mucMoMenuId.set(null);
    if (khoa === 'doi-ten') {
      this.hop.moHopSua(vt);
    } else if (khoa === 'xoa') {
      this.moHopXoa(vt);
    }
  }

  // ---- Hộp thoại tạo / đổi tên: trạng thái và luồng ghi ở HopVaiTroStore; page chỉ nối ----
  protected readonly hop = inject(HopVaiTroStore);
  private readonly oTen = viewChild('oTen', { read: ElementRef<HTMLElement> });

  constructor() {
    // Tải lại sau xung đột nạp xong → focus về ô tên: nút Tải lại vừa rời đi cùng banner nên focus
    // rơi mất (Screens/11 §Trạng thái). Chờ render để ô đã mang giá trị mới.
    const injector = inject(Injector);
    effect(() => {
      if (this.hop.lanNapLai() === 0) return;
      afterNextRender(() => this.oTen()?.nativeElement.querySelector('input')?.focus(), {
        injector,
      });
    });
  }

  /**
   * Ghi xong: đổi tên thì thay dòng ngay bằng bản mang `version` mới; cả hai ca đều tải lại. Arrow
   * function vì template truyền nó làm callback cho store — `this` phải là page.
   */
  protected readonly xongHop = (daDoi: VaiTro | null): void => {
    const cu = daDoi && this.list.rows().find((r) => r.id === daDoi.id)?.version;
    if (daDoi && cu) this.daDoiTen.update((m) => new Map(m).set(daDoi.id, { cu, moi: daDoi }));
    this.list.reload();
  };
  protected readonly taiLaiDanhSach = (): void => this.list.reload();

  // ---- Xoá ----
  protected readonly vaiTroDangXoa = signal<VaiTro | null>(null);
  protected readonly dangXoa = signal(false);
  protected readonly loiXoa = signal<string | null>(null);

  protected moHopXoa(vt: VaiTro): void {
    this.vaiTroDangXoa.set(vt);
    this.loiXoa.set(null);
  }

  protected dongHopXoa(): void {
    if (this.dangXoa()) return;
    this.vaiTroDangXoa.set(null);
  }

  protected xacNhanXoa(): void {
    const vt = this.vaiTroDangXoa();
    if (!vt) return;
    this.dangXoa.set(true);
    this.loiXoa.set(null);
    this.service
      .xoa(vt.id)
      .pipe(finalize(() => this.dangXoa.set(false)))
      .subscribe({
        next: () => {
          this.vaiTroDangXoa.set(null);
          this.toast.thanhCong(
            this.translate.instant('vaiTro.thongBao.xoaThanhCong', { ten: vt.name }),
          );
          this.list.reload();
        },
        error: (err: unknown) => {
          // Hộp xác nhận không có form: câu của banner do fe-ui-conventions.md §6.2 quyết.
          this.loiXoa.set(
            err instanceof ApiFailureError
              ? fieldErrorsText(this.translate, err)
              : this.translate.instant('loi.CORE.CLIENT.NO_CONNECTION'),
          );
        },
      });
  }
}
