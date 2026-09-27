import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { PaginatorModule, PaginatorState } from 'primeng/paginator';

/**
 * Design/Components/Pagination.md. Bọc PrimeNG (`p-paginator`) — tính dãy số trang có dấu lược,
 * đồng bộ phân trang phía máy chủ (§Nền). `DataTable` dùng component này BÊN TRONG, ở chân khung.
 *
 * 🚧 F3 tối giản: chỉ dựng biến thể `full`, cỡ `md` — đúng nhu cầu duy nhất (`DataTable` biến thể
 * `paged`). `compact`/`simple` và cỡ `sm` chưa màn nào cần. Chuỗi "x–y trên tổng z" tự dịch qua
 * khoá `chung.phanTrang.khoang` — cùng khoá cho mọi màn danh sách, không lặp lại ở từng trang.
 */
@Component({
  selector: 'app-pagination',
  standalone: true,
  imports: [PaginatorModule, TranslatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './pagination.component.html',
  styleUrl: './pagination.component.scss',
})
export class PaginationComponent {
  readonly page = input(1);
  readonly pageSize = input.required<number>();
  readonly pageSizeOptions = input.required<readonly number[]>();
  readonly totalRecords = input(0);
  readonly disabled = input(false);
  readonly loading = input(false);
  readonly ariaLabel = input.required<string>();

  readonly pageChanged = output<number>();
  readonly pageSizeChanged = output<{ pageSize: number; page: number }>();

  /** PrimeNG `Paginator` đếm `first`/trang từ 0 — quy đổi ở ĐÚNG MỘT chỗ, không lọt ra ngoài. */
  protected readonly first = computed(() => (this.page() - 1) * this.pageSize());

  /** `[rowsPerPageOptions]` của PrimeNG đòi mảng có thể sửa — `pageSizeOptions()` là readonly. */
  protected readonly tuyChonSoDongMutable = computed(() => [...this.pageSizeOptions()]);

  protected readonly thamSoKhoang = computed(() => {
    const tong = this.totalRecords();
    const dau = tong === 0 ? 0 : this.first() + 1;
    const cuoi = Math.min(this.first() + this.pageSize(), tong);
    return { tu: dau, den: cuoi, tong };
  });

  protected onPageChange(state: PaginatorState): void {
    const rows = state.rows ?? this.pageSize();
    const trangMoi = Math.floor((state.first ?? 0) / rows) + 1;
    if (rows !== this.pageSize()) {
      this.pageSizeChanged.emit({ pageSize: rows, page: 1 });
      return;
    }
    if (trangMoi !== this.page()) {
      this.pageChanged.emit(trangMoi);
    }
  }
}
