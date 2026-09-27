import type { PageQuery } from '../http/paged.model';

/**
 * Định nghĩa gốc: quy-uoc/fe-architecture.md §2.8. Truy vấn của MỘT màn danh sách — tên trường là
 * TÊN TRÊN DÂY, không có bước đổi tên ở giữa (luật 2 của §2.8).
 */
export interface GridQuery extends PageQuery {
  /** Bộ lọc riêng của endpoint — mỗi khoá một tham số rời, tên theo card ở contracts/. */
  readonly filters: Readonly<Record<string, string>>;
}
