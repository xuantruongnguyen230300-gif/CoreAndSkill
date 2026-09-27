/**
 * Model MÀN HÌNH của menu — cây đã dựng, tối đa hai cấp (Sidebar.md §Kích thước). Không `extends`
 * DTO (fe-api-client.md §4.2): kế thừa đưa hình dạng dây (`parentId`, `displayOrder`) tới màn mà
 * luật F10 không thấy. Thứ tự anh em là thứ tự máy chủ trả — mapper giữ nguyên, không sắp lại
 * (contracts/meta-menu.md §1).
 */
export interface MenuNode {
  readonly id: string;
  readonly code: string;
  /** Khoá i18n — `Sidebar` nhận nhãn ĐÃ dịch, shell dịch khi đổi sang NavItem. */
  readonly labelKey: string;
  readonly icon: string | null;
  /** `null` = mục chỉ để nhóm, không điều hướng. */
  readonly route: string | null;
  readonly children: readonly MenuNode[];
}
