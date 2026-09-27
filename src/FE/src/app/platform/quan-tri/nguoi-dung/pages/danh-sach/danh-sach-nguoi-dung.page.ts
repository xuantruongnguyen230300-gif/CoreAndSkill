import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  TemplateRef,
  computed,
  effect,
  inject,
  untracked,
  viewChild,
} from '@angular/core';
import { takeUntilDestroyed, toObservable, toSignal } from '@angular/core/rxjs-interop';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { EMPTY, Observable, catchError, map, of, switchMap, tap } from 'rxjs';

import { AuthService } from '../../../../../core/auth/auth.service';
import { GridQuery } from '../../../../../core/list/grid-query';
import { ListStateStore } from '../../../../../core/list/list-state.store';
import { ToastService } from '../../../../../core/toast/toast.service';
import { BadgeComponent } from '../../../../../shared/components/badge/badge.component';
import { ConfirmDialogComponent } from '../../../../../shared/components/confirm-dialog/confirm-dialog.component';
import { EmptyStateComponent } from '../../../../../shared/components/empty-state/empty-state.component';
import { HasPermissionDirective } from '../../../../../shared/directives/has-permission.directive';
import { PageHeaderComponent } from '../../../../../shared/components/page-header/page-header.component';
import { SkeletonLoaderComponent } from '../../../../../shared/components/skeleton-loader/skeleton-loader.component';
import {
  ToolbarChip,
  ToolbarComponent,
} from '../../../../../shared/components/toolbar/toolbar.component';
import { AutocompleteComponent } from '../../../../../shared/ui/autocomplete/autocomplete.component';
import { ButtonComponent } from '../../../../../shared/components/button/button.component';
import {
  DataColumnDef,
  DataTableComponent,
} from '../../../../../shared/ui/data-table/data-table.component';
import {
  InputComponent,
  SegmentOption,
} from '../../../../../shared/components/input/input.component';
import { CORE_SCREEN_EXT } from '../../../../config/core-screen-ext';
import { CotCore, kiemCotMoRong, sangCotDataTable } from '../../../../config/cot-mo-rong';
import { VaiTroService } from '../../../vai-tro/services/vai-tro.service';
import { TraCuuVaiTroStore } from '../../../vai-tro/state/tra-cuu-vai-tro.store';
import { NguoiDungService } from '../../services/nguoi-dung.service';
import { TaoNguoiDungStore } from '../../state/tao-nguoi-dung.store';
import { HopTaoNguoiDungComponent } from '../../components/hop-tao-nguoi-dung/hop-tao-nguoi-dung.component';
import { NguoiDung } from '../../models/nguoi-dung.model';

/** Khoá cột Core của màn — tập đưa vào phép kiểm khoá trùng (fe-architecture.md §2.7 luật 6). */
const KHOA_COT_CORE = [
  'userName',
  'fullName',
  'email',
  'vaiTro',
  'trangThai',
  'createdAt',
] as const;
type KhoaCotCore = (typeof KHOA_COT_CORE)[number];

/** Tên của vai trò đang lọc, kèm `id` mà tên đó thuộc về — chip chỉ vẽ khi `id` khớp URL. */
interface VaiTroDangLoc {
  readonly id: string;
  readonly ten: string;
}

/** Design/Screens/10-nguoi-dung.md. Guard: permissionGuard('core.user.read') (nguoi-dung.routes.ts). */
@Component({
  selector: 'app-danh-sach-nguoi-dung',
  standalone: true,
  imports: [
    RouterLink,
    ReactiveFormsModule,
    TranslatePipe,
    DatePipe,
    AutocompleteComponent,
    BadgeComponent,
    ConfirmDialogComponent,
    EmptyStateComponent,
    HasPermissionDirective,
    PageHeaderComponent,
    SkeletonLoaderComponent,
    ToolbarComponent,
    ButtonComponent,
    DataTableComponent,
    HopTaoNguoiDungComponent,
    InputComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [ListStateStore, TaoNguoiDungStore],
  templateUrl: './danh-sach-nguoi-dung.page.html',
  styleUrl: './danh-sach-nguoi-dung.page.scss',
})
export class DanhSachNguoiDungPage {
  private readonly service = inject(NguoiDungService);
  private readonly vaiTroService = inject(VaiTroService);
  protected readonly auth = inject(AuthService);
  private readonly translate = inject(TranslateService);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);
  /**
   * Seam CORE_SCREEN_EXT khoá 'users' (fe-architecture.md §2.7): không khai ⇒ đúng bản gốc; khoá
   * trùng ⇒ ném ngay lúc dựng màn (luật 6), không đợi `columns` tính.
   */
  private readonly cotMoRong = kiemCotMoRong(
    'users',
    KHOA_COT_CORE,
    inject(CORE_SCREEN_EXT, { optional: true })?.users?.columns,
  );

  protected readonly coQuyenXemVaiTro = computed(() => this.auth.coQuyen('core.role.read'));
  protected readonly coQuyenGanVaiTro = computed(
    () => this.auth.coQuyen('core.user.role.assign') && this.coQuyenXemVaiTro(),
  );

  /**
   * Thiếu `core.role.read` thì `roleId` trên URL KHÔNG gửi lên (Design/Screens/10-nguoi-dung.md) —
   * kể cả khi Back đưa lại URL mang nó: page được tái dùng, lệnh gỡ trong constructor không chạy lại.
   */
  protected readonly list = inject<ListStateStore<NguoiDung>>(ListStateStore).connect((q) =>
    this.service.danhSach(this.coQuyenXemVaiTro() ? q : boLocVaiTro(q)),
  );

  // ---- Chip đơn vị + chip điều kiện ----
  protected readonly readonlyChips = computed<readonly ToolbarChip[]>(() => {
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

  /** `roleId` đang lọc — ĐỌC từ URL; thiếu `core.role.read` thì coi như không lọc (xem `list`). */
  protected readonly roleIdDangLoc = computed(() =>
    this.coQuyenXemVaiTro() ? this.giaTriLocVaiTro() : null,
  );

  /**
   * SUY từ `roleId` trên URL mỗi lần URL đổi, không nạp một lần lúc dựng: Back/Forward tái dùng page
   * (fe-architecture.md §2.8 luật 1). `switchMap` huỷ lượt tra của `roleId` cũ; `toSignal` huỷ đăng
   * ký khi page bị huỷ (fe-api-client.md §6.1).
   */
  private readonly vaiTroDangLoc = toSignal(
    toObservable(this.roleIdDangLoc).pipe(
      switchMap((id) => (id ? this.traTenVaiTro(id) : of(null))),
    ),
    { initialValue: null },
  );

  protected readonly chips = computed<readonly ToolbarChip[]>(() => {
    const q = this.list.query();
    const ds: ToolbarChip[] = [];
    if (q.searchText) {
      ds.push({
        key: 'searchText',
        label: this.translate.instant('nguoiDung.timKiem.nhan'),
        value: q.searchText,
        removable: true,
      });
    }
    // So `id`: trong lúc tra tên của `roleId` mới, tên của lần lọc trước không được hiện dưới nó.
    const vaiTro = this.vaiTroDangLoc();
    if (vaiTro && vaiTro.id === this.roleIdDangLoc()) {
      ds.push({
        key: 'roleId',
        label: this.translate.instant('nguoiDung.loc.vaiTro'),
        value: vaiTro.ten,
        removable: true,
      });
    }
    if (q.filters['status']) {
      const nhan =
        q.filters['status'] === 'active'
          ? this.translate.instant('nguoiDung.trangThai.hoatDong')
          : this.translate.instant('nguoiDung.trangThai.daKhoa');
      ds.push({
        key: 'status',
        label: this.translate.instant('nguoiDung.loc.trangThai'),
        value: nhan,
        removable: true,
      });
    }
    return ds;
  });

  constructor() {
    // Bỏ tham số roleId khỏi URL nếu thiếu quyền core.role.read (Design/Screens/10-nguoi-dung.md).
    if (!this.coQuyenXemVaiTro() && this.list.query().filters['roleId']) {
      this.list.setFilters({ roleId: null });
    }
    this.filterTrangThai.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((v) => this.list.setFilters({ status: v === '' ? null : v }));
    // Ô trạng thái giữ giá trị trong FormControl, không bind thẳng vào URL được: kéo nó theo URL mỗi
    // lần URL đổi (luật 1). `emitEvent: false` — đồng bộ không được quay lại ghi URL thêm một bước.
    effect(() => {
      const trenUrl = this.list.query().filters['status'] ?? '';
      untracked(() => {
        if (this.filterTrangThai.value !== trenUrl) {
          this.filterTrangThai.setValue(trenUrl, { emitEvent: false });
        }
      });
    });
  }

  /**
   * Tên đã có trong gợi ý của ô lọc (người dùng vừa chọn từ đó) thì dùng luôn; chưa có (mở từ liên
   * kết, Back tới vai trò ngoài lượt tìm gần nhất) thì tra `GET /roles/{id}`. Tra hỏng → không vẽ
   * chip, không toast (Design/Screens/10-nguoi-dung.md); `catchError` nằm TRONG `switchMap`.
   *
   * Tên tra được dùng cho chip VÀ ô lọc (cùng spec): Autocomplete chỉ biết nhãn của khoá từng có
   * trong `options()`, nên `seed` vai trò vừa tra vào gợi ý — thiếu bước này ô lọc hiện id thô.
   */
  private traTenVaiTro(id: string): Observable<VaiTroDangLoc> {
    const daCo = this.timVaiTroLoc.options().find((o) => o.key === id);
    if (daCo) return of({ id, ten: daCo.label });
    return this.vaiTroService.chiTiet(id).pipe(
      tap((vt) => this.timVaiTroLoc.seed([vt])),
      map((vt) => ({ id, ten: vt.name })),
      catchError(() => EMPTY),
    );
  }

  protected onChipRemoved(key: string): void {
    if (key === 'searchText') {
      this.list.setSearchText('');
    } else {
      this.list.setFilters({ [key]: null });
    }
  }

  protected onFiltersCleared(): void {
    this.list.setSearchText('');
    this.list.setFilters({ roleId: null, status: null });
  }

  // ---- Ô lọc trạng thái ----
  protected readonly tuyChonTrangThai: readonly SegmentOption[] = [
    { value: '', label: this.translate.instant('nguoiDung.trangThai.tatCa') },
    { value: 'active', label: this.translate.instant('nguoiDung.trangThai.hoatDong') },
    { value: 'locked', label: this.translate.instant('nguoiDung.trangThai.daKhoa') },
  ];
  protected readonly filterTrangThai = new FormControl(this.list.query().filters['status'] ?? '', {
    nonNullable: true,
  });

  /** `Record` không đánh dấu khoá vắng mặt là `undefined` trong kiểu khai báo — ép kiểu tường
   *  minh ở ĐÚNG một chỗ để giữ đường lùi `?? null` khi khoá thật sự vắng lúc chạy. */
  protected giaTriLocVaiTro(): string | null {
    return (this.list.query().filters['roleId'] as string | undefined) ?? null;
  }

  // ---- Ô lọc vai trò (Autocomplete single) ----
  protected readonly timVaiTroLoc = new TraCuuVaiTroStore(this.vaiTroService, this.translate);

  /** Chỉ ghi URL — nhãn chip tự theo `roleId` mới (`vaiTroDangLoc`), page không giữ bản thứ hai. */
  protected onChonVaiTroLoc(gt: string | readonly string[]): void {
    this.list.setFilters({ roleId: typeof gt === 'string' ? gt || null : null });
  }

  // ---- Cột DataTable ----
  private readonly colTen = viewChild.required<TemplateRef<{ $implicit: NguoiDung }>>('colTen');
  private readonly colHoTen = viewChild.required<TemplateRef<{ $implicit: NguoiDung }>>('colHoTen');
  private readonly colEmail = viewChild.required<TemplateRef<{ $implicit: NguoiDung }>>('colEmail');
  private readonly colVaiTro =
    viewChild.required<TemplateRef<{ $implicit: NguoiDung }>>('colVaiTro');
  private readonly colTrangThai =
    viewChild.required<TemplateRef<{ $implicit: NguoiDung }>>('colTrangThai');
  private readonly colNgayTao =
    viewChild.required<TemplateRef<{ $implicit: NguoiDung }>>('colNgayTao');

  /** Cột của dự án nối SAU "Ngày tạo" (luật 2; Design/Screens/10-nguoi-dung.md §Điểm mở rộng). */
  protected readonly columns = computed<DataColumnDef<NguoiDung>[]>(() => {
    const cotCore: CotCore<NguoiDung, KhoaCotCore>[] = [
      {
        key: 'userName',
        header: this.translate.instant('nguoiDung.cot.tenDangNhap'),
        sortable: true,
        priority: 'high',
        cell: this.colTen(),
      },
      {
        key: 'fullName',
        header: this.translate.instant('nguoiDung.cot.hoTen'),
        sortable: true,
        cell: this.colHoTen(),
      },
      {
        key: 'email',
        header: this.translate.instant('nguoiDung.cot.email'),
        sortable: true,
        hideBelow: 'md',
        cell: this.colEmail(),
      },
      {
        key: 'vaiTro',
        header: this.translate.instant('nguoiDung.cot.vaiTro'),
        hideBelow: 'sm',
        cell: this.colVaiTro(),
      },
      {
        key: 'trangThai',
        header: this.translate.instant('nguoiDung.cot.trangThai'),
        cell: this.colTrangThai(),
      },
      {
        key: 'createdAt',
        header: this.translate.instant('nguoiDung.cot.ngayTao'),
        sortable: true,
        priority: 'low',
        cell: this.colNgayTao(),
      },
    ];
    return [...cotCore, ...sangCotDataTable(this.cotMoRong, (k) => this.translate.instant(k))];
  });

  // ---- Hộp thoại "Thêm người dùng" — trạng thái và quy trình nằm ở TaoNguoiDungStore ----
  protected readonly tao = inject(TaoNguoiDungStore);

  protected tenVaiTro(nd: NguoiDung): string {
    return nd.roles.map((r) => r.name).join(', ');
  }

  protected guiTao(): void {
    this.tao.gui((tenDangNhap) => {
      this.toast.thanhCong(
        this.translate.instant('nguoiDung.thongBao.taoThanhCong', { tenDangNhap }),
      );
      this.list.reload();
    });
  }
}

/** Truy vấn gửi lên khi thiếu `core.role.read`: mọi bộ lọc giữ nguyên, trừ `roleId`. */
function boLocVaiTro(q: GridQuery): GridQuery {
  return {
    ...q,
    filters: Object.fromEntries(Object.entries(q.filters).filter(([k]) => k !== 'roleId')),
  };
}
