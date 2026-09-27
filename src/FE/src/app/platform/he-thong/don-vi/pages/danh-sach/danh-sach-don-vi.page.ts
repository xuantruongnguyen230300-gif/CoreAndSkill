import {
  ChangeDetectionStrategy,
  Component,
  TemplateRef,
  computed,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { DatePipe } from '@angular/common';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { finalize } from 'rxjs';

import { ApiFailureError } from '../../../../../core/http/api-result.model';
import { dichLoiChoMan } from '../../../../../core/http/dich-loi';
import { ListStateStore } from '../../../../../core/list/list-state.store';
import { ToastService } from '../../../../../core/toast/toast.service';
import { BadgeComponent } from '../../../../../shared/components/badge/badge.component';
import { ConfirmDialogComponent } from '../../../../../shared/components/confirm-dialog/confirm-dialog.component';
import { EmptyStateComponent } from '../../../../../shared/components/empty-state/empty-state.component';
import { PageHeaderComponent } from '../../../../../shared/components/page-header/page-header.component';
import { fieldErrorsText } from '../../../../../shared/forms/field-errors-text';
import { SkeletonLoaderComponent } from '../../../../../shared/components/skeleton-loader/skeleton-loader.component';
import { ToolbarComponent } from '../../../../../shared/components/toolbar/toolbar.component';
import { ButtonComponent } from '../../../../../shared/components/button/button.component';
import {
  DataColumnDef,
  DataTableComponent,
} from '../../../../../shared/ui/data-table/data-table.component';
import { IconButtonComponent } from '../../../../../shared/components/icon-button/icon-button.component';
import { MenuComponent, UiMenuItem } from '../../../../../shared/ui/menu/menu.component';
import { CORE_SCREEN_EXT } from '../../../../config/core-screen-ext';
import { CotCore, kiemCotMoRong, sangCotDataTable } from '../../../../config/cot-mo-rong';
import { HopQuanTriDonViComponent } from '../../components/hop-quan-tri-don-vi/hop-quan-tri-don-vi.component';
import { HopTaoDonViComponent } from '../../components/hop-tao-don-vi/hop-tao-don-vi.component';
import { DonViService } from '../../services/don-vi.service';
import { KhoiPhucTaoQuanTriStore } from '../../state/khoi-phuc-tao-quan-tri.store';
import { TaoDonViStore } from '../../state/tao-don-vi.store';
import { DonVi } from '../../models/don-vi.model';

/** Khoá cột Core của màn — tập đưa vào phép kiểm khoá trùng (fe-architecture.md §2.7 luật 6). */
const KHOA_COT_CORE = ['code', 'name', 'trangThai', 'createdAt', 'hanhDong'] as const;
type KhoaCotCore = (typeof KHOA_COT_CORE)[number];

/**
 * Design/Screens/20-don-vi.md. Guard: `systemOperatorGuard` (don-vi.routes.ts) — cờ
 * `isSystemOperator`, KHÔNG permission; không có directive song song để ẩn/hiện nút trong màn này
 * (fe-routing-guard.md §3.5): vào được màn là dùng được mọi thao tác. Một màn, ba hộp thoại, một
 * hộp xác nhận. KHÔNG có màn chi tiết — người vận hành không xem dữ liệu bên trong đơn vị.
 *
 * Quy trình của hai cụm hộp thoại ("Tạo đơn vị"; "Khôi phục quản trị" + "Tạo quản trị mới") sống ở
 * `TaoDonViStore`/`KhoiPhucTaoQuanTriStore` (state/, cấp ở `providers` của page — ADR-0049), và
 * MARKUP của chúng ở `HopTaoDonViComponent`/`HopQuanTriDonViComponent` (components/, dumb — nhận
 * giá trị qua `input()`, phát `output()`). Page là chỗ NỐI hai bên — để giữ cả *.page.ts lẫn
 * *.page.html dưới ngưỡng dòng (fe-architecture.md §5, luật F22).
 */
@Component({
  selector: 'app-danh-sach-don-vi',
  standalone: true,
  imports: [
    TranslatePipe,
    DatePipe,
    BadgeComponent,
    ConfirmDialogComponent,
    EmptyStateComponent,
    PageHeaderComponent,
    SkeletonLoaderComponent,
    ToolbarComponent,
    ButtonComponent,
    DataTableComponent,
    IconButtonComponent,
    MenuComponent,
    HopQuanTriDonViComponent,
    HopTaoDonViComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [ListStateStore, TaoDonViStore, KhoiPhucTaoQuanTriStore],
  templateUrl: './danh-sach-don-vi.page.html',
})
export class DanhSachDonViPage {
  private readonly service = inject(DonViService);
  private readonly translate = inject(TranslateService);
  private readonly toast = inject(ToastService);
  /**
   * Seam CORE_SCREEN_EXT khoá 'tenants' (fe-architecture.md §2.7): không khai ⇒ đúng bản gốc; khoá
   * trùng ⇒ ném ngay lúc dựng màn (luật 6), không đợi `columns` tính.
   */
  private readonly cotMoRong = kiemCotMoRong(
    'tenants',
    KHOA_COT_CORE,
    inject(CORE_SCREEN_EXT, { optional: true })?.tenants?.columns,
  );

  protected readonly list = inject<ListStateStore<DonVi>>(ListStateStore).connect((q) =>
    this.service.danhSach(q),
  );

  protected readonly taoHop = inject(TaoDonViStore);
  protected readonly quanTriHop = inject(KhoiPhucTaoQuanTriStore);

  protected moHopTao(): void {
    this.taoHop.moHop();
  }

  protected guiTao(): void {
    this.taoHop.gui((dv) => {
      this.toast.thanhCong(this.translate.instant('donVi.thongBao.taoThanhCong', { ten: dv.name }));
      this.list.reload();
    });
  }

  protected guiKhoiPhuc(): void {
    this.quanTriHop.guiKhoiPhuc(
      () => {
        // Không nêu gì về tài khoản đích — phản hồi không mang dữ liệu đó (ADR-0029).
        this.toast.thanhCong(this.translate.instant('donVi.thongBao.khoiPhucThanhCong'));
      },
      () => this.list.reload(), // CORE.TENANT.NOT_FOUND — đơn vị đã bị xoá, bỏ hàng cũ khỏi lưới
    );
  }

  protected guiTaoQuanTri(): void {
    this.quanTriHop.guiTaoQuanTri(
      (tenDangNhap) => {
        this.toast.thanhCong(
          this.translate.instant('donVi.thongBao.taoQuanTriThanhCong', { tenDangNhap }),
        );
      },
      () => this.list.reload(), // CORE.TENANT.NOT_FOUND — đơn vị đã bị xoá, bỏ hàng cũ khỏi lưới
    );
  }

  // ---- Cột DataTable ----
  private readonly colMa = viewChild.required<TemplateRef<{ $implicit: DonVi }>>('colMa');
  private readonly colTen = viewChild.required<TemplateRef<{ $implicit: DonVi }>>('colTen');
  private readonly colTrangThai =
    viewChild.required<TemplateRef<{ $implicit: DonVi }>>('colTrangThai');
  private readonly colNgayTao = viewChild.required<TemplateRef<{ $implicit: DonVi }>>('colNgayTao');
  private readonly colHanhDong =
    viewChild.required<TemplateRef<{ $implicit: DonVi }>>('colHanhDong');

  protected readonly columns = computed<DataColumnDef<DonVi>[]>(() => {
    const cotDuLieu: CotCore<DonVi, KhoaCotCore>[] = [
      {
        key: 'code',
        header: this.translate.instant('donVi.cot.ma'),
        sortable: true,
        cell: this.colMa(),
      },
      {
        key: 'name',
        header: this.translate.instant('donVi.cot.ten'),
        sortable: true,
        priority: 'high',
        cell: this.colTen(),
      },
      {
        key: 'trangThai',
        header: this.translate.instant('donVi.cot.trangThai'),
        cell: this.colTrangThai(),
      },
      {
        key: 'createdAt',
        header: this.translate.instant('donVi.cot.ngayTao'),
        sortable: true,
        priority: 'low',
        cell: this.colNgayTao(),
      },
    ];
    const cotHanhDong: CotCore<DonVi, KhoaCotCore> = {
      key: 'hanhDong',
      header: this.translate.instant('donVi.cot.hanhDong'),
      cell: this.colHanhDong(),
    };
    // Cột của dự án nối sau cột dữ liệu của Core; cột hành động vẫn đứng cuối (DataTable.md).
    return [
      ...cotDuLieu,
      ...sangCotDataTable(this.cotMoRong, (k) => this.translate.instant(k)),
      cotHanhDong,
    ];
  });

  // ---- Menu hành động theo hàng ----
  protected readonly mucMoMenuId = signal<string | null>(null);
  protected readonly suKienMoMenu = signal<Event | null>(null);
  /** Đơn vị đang có một thao tác ngưng/bật gửi dở — mục menu của đúng hàng đó khoá lại. */
  protected readonly dangXuLyId = signal<string | null>(null);

  protected mucMenu(dv: DonVi): UiMenuItem[] {
    const dangXuLy = this.dangXuLyId() === dv.id;
    const mucTrangThai: UiMenuItem = dv.isActive
      ? {
          key: 'ngung',
          label: this.translate.instant('donVi.menu.ngung'),
          icon: 'power-off',
          danger: true,
          disabled: dangXuLy,
        }
      : {
          key: 'bat-lai',
          label: this.translate.instant('donVi.menu.batLai'),
          icon: 'power-off',
          disabled: dangXuLy,
        };
    return [
      mucTrangThai,
      {
        key: 'khoi-phuc',
        label: this.translate.instant('donVi.menu.khoiPhuc'),
        icon: 'key',
        disabled: dangXuLy,
      },
      {
        key: 'tao-quan-tri',
        label: this.translate.instant('donVi.menu.taoQuanTri'),
        icon: 'plus',
        disabled: dangXuLy,
      },
    ];
  }

  protected onMoMenu(evt: Event, dv: DonVi): void {
    this.suKienMoMenu.set(evt);
    this.mucMoMenuId.set(this.mucMoMenuId() === dv.id ? null : dv.id);
  }

  /**
   * `p-menu` phát `openChange(false)` khi hoạt ảnh ẩn BẮT ĐẦU — có thể tới SAU khi nút hàng khác
   * đã đặt `mucMoMenuId`. Chỉ đóng khi menu vừa ẩn ĐÚNG là menu đang được ghi nhận là mở.
   */
  protected onDongMenu(id: string): void {
    if (this.mucMoMenuId() !== id) return;
    this.mucMoMenuId.set(null);
  }

  protected onChonMenu(khoa: string, dv: DonVi): void {
    this.mucMoMenuId.set(null);
    if (khoa === 'ngung') this.moXacNhanNgung(dv);
    else if (khoa === 'bat-lai') this.xuLyDatTrangThai(dv, true);
    else if (khoa === 'khoi-phuc') this.quanTriHop.moKhoiPhuc(dv);
    else if (khoa === 'tao-quan-tri') this.quanTriHop.moTaoQuanTri(dv);
  }

  // ---- Ngưng hoạt động (ConfirmDialog warning) / Bật lại hoạt động (không hỏi) ----
  protected readonly donViDangNgung = signal<DonVi | null>(null);
  protected readonly dangXacNhanNgung = signal(false);
  protected readonly loiXacNhanNgung = signal<string | null>(null);

  protected moXacNhanNgung(dv: DonVi): void {
    this.donViDangNgung.set(dv);
    this.loiXacNhanNgung.set(null);
  }

  protected dongXacNhanNgung(): void {
    if (this.dangXacNhanNgung()) return;
    this.donViDangNgung.set(null);
  }

  protected xacNhanNgung(): void {
    const dv = this.donViDangNgung();
    if (!dv) return;
    this.dangXacNhanNgung.set(true);
    this.loiXacNhanNgung.set(null);
    this.xuLyDatTrangThai(dv, false, () => this.dangXacNhanNgung.set(false));
  }

  /**
   * `isActive: true` (bật lại) không có hộp xác nhận — lỗi tự hiện qua Toast, tải lại danh sách
   * (Design/Screens/20-don-vi.md, hàng `CORE.TENANT.NOT_FOUND`). `isActive: false` (ngưng) có
   * `ConfirmDialog` đang mở — lỗi hiện trong chính hộp đó.
   */
  private xuLyDatTrangThai(dv: DonVi, isActive: boolean, xong?: () => void): void {
    this.dangXuLyId.set(dv.id);
    this.service
      .datTrangThai(dv.id, isActive)
      .pipe(
        finalize(() => {
          this.dangXuLyId.set(null);
          xong?.();
        }),
      )
      .subscribe({
        next: () => {
          if (!isActive) this.donViDangNgung.set(null);
          this.toast.thanhCong(
            this.translate.instant(
              isActive ? 'donVi.thongBao.batLaiThanhCong' : 'donVi.thongBao.ngungThanhCong',
              { ten: dv.name },
            ),
          );
          this.list.reload();
        },
        error: (err: unknown) => {
          if (isActive) {
            // Request đã tắt toast chung — toast này là thông báo duy nhất, phải mang traceId (fe-api-client.md §1.1).
            // Lớp lỗi xuyên suốt thì interceptor đã toast rồi: câu `null`, không toast lần hai.
            const cau = dichLoiHoacMatKetNoi(err, this.translate);
            if (cau !== null) {
              this.toast.loi(
                cau,
                err instanceof ApiFailureError ? (err.body?.traceId ?? null) : null,
              );
            }
            this.list.reload();
            return;
          }
          this.loiXacNhanNgung.set(cauBannerHopNgung(err, this.translate));
          // NOT_FOUND: đơn vị đã bị người khác xoá — banner trong hộp VÀ bỏ hàng cũ khỏi lưới.
          if (laKhongTimThay(err)) this.list.reload();
        },
      });
  }
}

function laKhongTimThay(err: unknown): boolean {
  return err instanceof ApiFailureError && err.body?.error.code === 'CORE.TENANT.NOT_FOUND';
}

/**
 * Câu của Toast màn tự bắn ở nhánh "bật lại" (không có hộp); `null` với lớp lỗi xuyên suốt —
 * interceptor đã toast nó kèm `traceId`.
 */
function dichLoiHoacMatKetNoi(err: unknown, translate: TranslateService): string | null {
  return err instanceof ApiFailureError
    ? dichLoiChoMan(translate, err)
    : translate.instant('loi.CORE.CLIENT.NO_CONNECTION');
}

/**
 * Câu của banner trong hộp xác nhận ngưng — khu lỗi chung của một hộp KHÔNG có form, nên câu do
 * fe-ui-conventions.md §6.2 quyết qua `fieldErrorsText`: mã con thắng mã gốc.
 */
function cauBannerHopNgung(err: unknown, translate: TranslateService): string | null {
  return err instanceof ApiFailureError
    ? fieldErrorsText(translate, err)
    : translate.instant('loi.CORE.CLIENT.NO_CONNECTION');
}
