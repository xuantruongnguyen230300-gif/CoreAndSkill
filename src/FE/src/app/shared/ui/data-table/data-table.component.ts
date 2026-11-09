import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  TemplateRef,
  computed,
  inject,
  input,
  linkedSignal,
  output,
  signal,
} from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';
import { TableModule } from 'primeng/table';

import { TRUY_VAN_DUOI_BP_XS } from '../../../core/theme/diem-ngat';
import { PaginationComponent } from '../pagination/pagination.component';

/** Chữ ký đầy đủ ở quy-uoc/fe-ui-conventions.md §9 — DataTable dùng thẳng, không mở rộng. */
export interface DataColumnDef<T = unknown> {
  readonly key: string;
  readonly header: string;
  readonly width?: string;
  readonly align?: 'start' | 'end';
  readonly hideBelow?: 'xs' | 'sm' | 'md' | 'lg';
  readonly sortable?: boolean;
  readonly priority?: 'high' | 'low';
  /** ĐÚNG MỘT trong `cell`/`value` — thiếu cả hai hay có cả hai thì ném lỗi lúc dựng cột. */
  readonly cell?: TemplateRef<{ $implicit: T }>;
  /** Chuỗi ĐÃ định dạng, vẽ thành chữ; cột của seam CORE_SCREEN_EXT (fe-architecture.md §2.7). */
  readonly value?: (row: T) => string;
}

/** Ngữ cảnh của `rowActionsTemplate`: `compact` là `true` dưới `$bp-xs` (DataTable.md §"Cột hành động"). */
export interface RowActionsContext<T> {
  readonly $implicit: T;
  readonly compact: boolean;
}

/** Câu của vùng báo kết quả: khoá i18n + tham số (DataTable.md §"Vùng báo kết quả"). */
interface CauKetQua {
  readonly khoa: string;
  readonly tham?: { tu: number; den: number; tong: number };
}

export type DataTableViewState = 'idle' | 'loading' | 'error' | 'empty' | 'empty-filtered';

/**
 * Design/Components/DataTable.md. Nơi DUY NHẤT trong ứng dụng được import lưới dữ liệu của
 * PrimeNG (`primeng/table`). Ghép `Pagination` (shared/ui/) BÊN TRONG ở chân khung — luật F5.
 *
 * 🚧 F3 tối giản: chỉ dựng biến thể `paged`, không công tắc nào (`selectable`/`frozen`/`tree`/
 * `grouped`/`expandable`/`summary`) — không màn F3 nào cần chúng (danh sách người dùng và vai trò
 * không có thao tác hàng loạt; ma trận phân quyền dùng `Table`, không dùng `DataTable`). Ẩn cột
 * theo `hideBelow`/`priority` xử lý bằng CSS thuần (không đo layout bằng JS).
 */
@Component({
  selector: 'app-data-table',
  standalone: true,
  imports: [NgTemplateOutlet, TableModule, PaginationComponent, TranslatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './data-table.component.html',
  styleUrl: './data-table.component.scss',
})
export class DataTableComponent<T> {
  readonly rows = input<readonly T[]>([]);
  readonly columns = input.required<readonly DataColumnDef<T>[]>();
  readonly caption = input.required<string>();
  readonly rowKey = input.required<string>();
  readonly state = input<DataTableViewState>('idle');

  readonly totalRecords = input(0);
  readonly page = input(1);
  readonly pageSize = input(20);
  readonly pageSizeOptions = input<readonly number[]>([10, 20, 50, 100]);
  readonly paginationAriaLabel = input.required<string>();

  readonly sortBy = input<string | null>(null);
  readonly sortDescending = input(false);

  readonly emptyTemplate = input<TemplateRef<{ $implicit: 'empty' | 'empty-filtered' }> | null>(
    null,
  );
  readonly loadingTemplate = input<TemplateRef<unknown> | null>(null);
  readonly errorTemplate = input<TemplateRef<unknown> | null>(null);
  /** Ô của cột hành động, một lần mỗi dòng; `null` = không có cột (DataTable.md §"Cột hành động"). */
  readonly rowActionsTemplate = input<TemplateRef<RowActionsContext<T>> | null>(null);
  /** Tên cột hành động, ĐÃ dịch, ẩn thị giác trong `th`. Bắt buộc khác rỗng khi có `rowActionsTemplate`. */
  readonly rowActionsLabel = input('');

  readonly pageChange = output<{ page: number; pageSize: number }>();
  readonly sortChange = output<{ sortBy: string; sortDescending: boolean }>();

  /**
   * Dưới `$bp-xs` (dạng thẻ) màn nhận `compact: true` để gom hành động về một nút ⋯. Theo dõi bằng
   * `matchMedia` — giá trị này phải vào ngữ cảnh template nên CSS thuần không đủ.
   */
  protected readonly compact = signal(false);

  constructor() {
    const mql = window.matchMedia(TRUY_VAN_DUOI_BP_XS);
    this.compact.set(mql.matches);
    const khiDoi = (e: MediaQueryListEvent): void => this.compact.set(e.matches);
    mql.addEventListener('change', khiDoi);
    inject(DestroyRef).onDestroy(() => mql.removeEventListener('change', khiDoi));
  }

  protected readonly dangTai = computed(
    () => this.state() === 'loading' && this.rows().length === 0,
  );
  protected readonly dangTaiLai = computed(
    () => this.state() === 'loading' && this.rows().length > 0,
  );
  protected readonly rong = computed(
    () => this.state() === 'empty' || this.state() === 'empty-filtered',
  );

  /**
   * Câu cho vùng `role="status"`. Đổi khi `state` rời `'loading'`; đang `'loading'` thì giữ câu cũ
   * (`aria-busy` đã báo). `linkedSignal` để có giá trị ngay từ lần vẽ đầu.
   */
  protected readonly cauKetQua = linkedSignal<
    { state: DataTableViewState; page: number; pageSize: number; tong: number },
    CauKetQua | null
  >({
    source: () => ({
      state: this.state(),
      page: this.page(),
      pageSize: this.pageSize(),
      tong: this.totalRecords(),
    }),
    computation: (nguon, truoc) => {
      switch (nguon.state) {
        case 'loading':
          return truoc?.value ?? null;
        case 'error':
          return { khoa: 'chung.bang.khongTaiDuoc' };
        case 'empty':
        case 'empty-filtered':
          return { khoa: 'chung.bang.khongCoKetQua' };
        case 'idle': {
          const dau = (nguon.page - 1) * nguon.pageSize;
          return {
            khoa: 'chung.bang.ketQua',
            tham: {
              tu: nguon.tong === 0 ? 0 : dau + 1,
              den: Math.min(dau + nguon.pageSize, nguon.tong),
              tong: nguon.tong,
            },
          };
        }
      }
    },
  });

  protected readonly tieuDeBang = computed(() => this.caption());

  /**
   * Cột hợp lệ (fe-ui-conventions.md §9): mỗi cột có ĐÚNG MỘT trong `cell`/`value`. Ném lỗi lúc
   * dựng cột chứ không vẽ ô rỗng — một cột thiếu cả hai là lỗi lập trình, im lặng thì không ai thấy.
   */
  /** Có cột hành động khi màn truyền template; thiếu tên cột thì ném lỗi như cột thiếu `cell`/`value`. */
  protected readonly coCotHanhDong = computed(() => {
    if (this.rowActionsTemplate() === null) return false;
    if (this.rowActionsLabel() === '') {
      throw new Error('DataTable: rowActionsLabel bắt buộc khác rỗng khi có rowActionsTemplate.');
    }
    return true;
  });

  /** Số cột của `colspan` ở thân bảng: cột hành động tính vào. */
  protected readonly soCot = computed(
    () => this.cotHopLe().length + (this.coCotHanhDong() ? 1 : 0),
  );

  protected readonly cotHopLe = computed<readonly DataColumnDef<T>[]>(() => {
    for (const cot of this.columns()) {
      const coCell = cot.cell !== undefined;
      const coValue = cot.value !== undefined;
      if (coCell === coValue) {
        throw new Error(
          `DataTable: cột "${cot.key}" phải có ĐÚNG MỘT trong hai: cell hoặc value ` +
            '(fe-ui-conventions.md §9).',
        );
      }
    }
    return this.columns();
  });

  /**
   * Tên truy cập của bảng đặt THẲNG lên `<table>` qua pass-through `pt.table` của PrimeNG
   * (DataTable.md §Accessibility, dòng "Tiêu đề bảng"). Không dùng `<ng-template #caption>`:
   * PrimeNG vẽ slot đó trong một `<div>` NGOÀI `<table>`, nên một `<caption>` đặt ở đó là phần tử
   * lạc chỗ và không đặt tên cho bảng.
   */
  protected readonly ptBang = computed(() => ({
    table: { 'aria-label': this.tieuDeBang() },
    // Khung `.data-table__khung` là vùng cuộn DUY NHẤT. Bản PrimeNG đang cài cho `tableContainer`
    // `overflow: auto` — thành vùng cuộn thứ hai và `th` dính đỉnh tính theo nó (không cuộn dọc nên
    // không bao giờ dính). `visible` trả vai trò cuộn cho khung.
    tableContainer: { style: { overflow: 'visible' } },
  }));

  /**
   * `[value]` của `p-table` đòi mảng có thể sửa — `rows()` là readonly. `error` thì thân bảng là
   * slot lỗi BẤT KỂ `rows` (DataTable.md §Trạng thái): nguồn tải có thể còn giữ dòng của trang
   * trước, mà PrimeNG chỉ vẽ `#emptymessage` khi `value` rỗng — nên `state` quyết, không phải `rows`.
   */
  protected readonly dsHangMutable = computed(() =>
    this.state() === 'error' ? [] : [...this.rows()],
  );

  /**
   * Hàm track dòng, truyền vào `p-table` qua `[rowTrackBy]` (DataTable.md §API, dòng `rowKey`).
   * PrimeNG mặc định track theo THAM CHIẾU (`(index, item) => item`), mà mỗi lần tải lại mapper
   * dựng object mới — không có hàm này thì toàn bộ `<tbody>` dựng lại và focus trên nút hành động
   * của dòng mất theo. `[dataKey]` nhận cùng khoá để mọi tính năng dòng của thư viện dùng chung một
   * định danh.
   */
  protected readonly khoaHang = (_i: number, hang: T): unknown =>
    (hang as Record<string, unknown>)[this.rowKey()];

  protected ariaSort(cot: DataColumnDef<T>): 'ascending' | 'descending' | 'none' {
    if (!cot.sortable) return 'none';
    if (this.sortBy() !== cot.key) return 'none';
    return this.sortDescending() ? 'descending' : 'ascending';
  }

  protected sapXep(cot: DataColumnDef<T>): void {
    if (!cot.sortable) return;
    const dangCungCot = this.sortBy() === cot.key;
    const desc = dangCungCot ? !this.sortDescending() : false;
    this.sortChange.emit({ sortBy: cot.key, sortDescending: desc });
  }

  protected onPageChanged(page: number): void {
    this.pageChange.emit({ page, pageSize: this.pageSize() });
  }

  protected onPageSizeChanged(evt: { pageSize: number; page: number }): void {
    this.pageChange.emit(evt);
  }
}
