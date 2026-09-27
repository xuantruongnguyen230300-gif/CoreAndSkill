import {
  ChangeDetectionStrategy,
  Component,
  TemplateRef,
  computed,
  input,
  output,
} from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';

import { NoticeBannerComponent } from '../notice-banner/notice-banner.component';
import { ButtonComponent } from '../button/button.component';

/** Định nghĩa cột của `Table` — cell LUÔN vào qua template, không có bộ vẽ mặc định theo chuỗi. */
export interface TableColumnDef<T = unknown> {
  readonly key: string;
  readonly header: string;
  readonly align?: 'start' | 'end';
  readonly width?: string;
  readonly cell: TemplateRef<{ $implicit: T; column: TableColumnDef<T> }>;
}

interface NhomHang<T> {
  readonly nhan: string;
  readonly hang: readonly T[];
}

/**
 * Design/Components/Table.md. Tự dựng, không bọc PrimeNG (§Nền) — bảng tĩnh, không sắp xếp,
 * không phân trang, không ảo hoá dòng; những thứ đó thuộc `DataTable`.
 *
 * 🚧 F3 tối giản: dựng đủ cho ma trận phân quyền (Design/Screens/12-ma-tran-phan-quyen.md) —
 * `groupBy` gom nhóm, `stickyHeader`, biến thể `bordered`, ba trạng thái loading/error/empty.
 * `cardLayout` (dạng thẻ dưới `$bp-xs`) CHƯA dựng — màn duy nhất dùng `Table` ở F3 đã tắt nó
 * (`cardLayout = false`, giữ dạng lưới ở mọi khổ theo đúng spec màn đó).
 */
@Component({
  selector: 'app-table',
  standalone: true,
  imports: [NgTemplateOutlet, NoticeBannerComponent, ButtonComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './table.component.html',
  styleUrl: './table.component.scss',
})
export class TableComponent<T> {
  readonly columns = input.required<readonly TableColumnDef<T>[]>();
  readonly rows = input<readonly T[]>([]);
  readonly caption = input.required<string>();
  readonly captionVisible = input(false);
  readonly rowKeyField = input.required<string>();
  readonly groupBy = input<string | null>(null);
  readonly emptyGroupLabel = input<string | null>(null);
  readonly variant = input<'default' | 'bordered' | 'zebra'>('default');
  readonly size = input<'sm' | 'md' | 'lg'>('md');
  readonly stickyHeader = input(false);
  readonly loading = input(false);
  /** Tiêu đề hàng lỗi — `heading` của `NoticeBanner` (Table.md §API). Chuỗi ĐÃ dịch. */
  readonly errorHeading = input<string | null>(null);
  /** Thân hàng lỗi. Một trong `errorHeading` / `errorMessage` có giá trị thì vẽ hàng lỗi thay cho thân. */
  readonly errorMessage = input<string | null>(null);
  /** F3 tối giản: chỉ hỗ trợ `false` (giữ dạng lưới) — xem ghi chú đầu file. */
  readonly cardLayout = input(true);
  readonly retryLabel = input<string>('');
  readonly emptyLabel = input<string>('');

  readonly retry = output<void>();

  /** Lần đầu — chưa có dữ liệu để giữ lại: thay thân bằng skeleton (Table.md §Trạng thái `loading`). */
  protected readonly dangTaiLanDau = computed(() => this.loading() && this.rows().length === 0);
  /** Tải lại trên dữ liệu cũ: GIỮ hàng cũ, phủ scrim + khoá thao tác — không thay bằng skeleton
   *  (cùng luật với DataTable.md §Trạng thái `loading`, ca "đổi trang/sắp xếp lại"). */
  protected readonly dangTaiLai = computed(() => this.loading() && this.rows().length > 0);

  protected readonly khoaHang = (_i: number, hang: T): unknown => this.layKhoa(hang);

  private layKhoa(hang: T): unknown {
    return (hang as Record<string, unknown>)[this.rowKeyField()];
  }

  protected readonly nhomHang = computed<readonly NhomHang<T>[]>(() => {
    const khoaNhom = this.groupBy();
    const ds = this.rows();
    if (khoaNhom === null) {
      return [{ nhan: '', hang: ds }];
    }
    const ketQua: NhomHang<T>[] = [];
    let nhomHienTai: NhomHang<T> | null = null;
    for (const hang of ds) {
      const giaTri = (hang as Record<string, unknown>)[khoaNhom];
      const nhan =
        giaTri === null || giaTri === undefined || giaTri === ''
          ? (this.emptyGroupLabel() ?? '')
          : String(giaTri);
      if (!nhomHienTai || nhomHienTai.nhan !== nhan) {
        nhomHienTai = { nhan, hang: [] };
        ketQua.push(nhomHienTai);
      }
      (nhomHienTai.hang as T[]).push(hang);
    }
    return ketQua;
  });

  protected readonly coNhom = computed(() => this.groupBy() !== null);

  /**
   * Trạng thái `error` bật khi MỘT TRONG HAI input có giá trị (Table.md §Trạng thái): màn đặt tiêu đề
   * cố định, còn thân có thể rỗng — lớp lỗi xuyên suốt đã toast chi tiết (fe-api-client.md §2.2).
   */
  protected readonly coLoi = computed(() => !!this.errorHeading() || !!this.errorMessage());
}
