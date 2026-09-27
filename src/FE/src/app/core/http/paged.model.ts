/**
 * Định nghĩa gốc: quy-uoc/fe-api-client.md §5.1. Khớp 1:1 phần `data` của mọi endpoint danh sách —
 * hợp đồng ở contracts/README.md §8. KHÔNG có `totalPages` — số trang do FE tự tính (§2.2 của
 * wiki-core/fe/11-grid-and-metadata.md).
 */
export interface PagedList<T> {
  readonly items: readonly T[];
  readonly page: number;
  readonly pageSize: number;
  readonly totalCount: number;
}

/** Tham số danh sách gửi LÊN DÂY. Tên field khớp đúng chuỗi query param của hợp đồng. */
export interface PageQuery {
  readonly page: number;
  readonly pageSize: number;
  readonly sortBy?: string;
  readonly sortDescending?: boolean;
  readonly searchText?: string;
  /** Bộ lọc riêng của endpoint — mỗi khoá thành một tham số rời, tên theo card. */
  readonly filters?: Readonly<Record<string, string>>;
}
