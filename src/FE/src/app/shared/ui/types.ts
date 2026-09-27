/**
 * Kiểu dữ liệu công khai của thư viện UI dùng bởi NHIỀU component — fe-ui-conventions.md §9.
 * Kiểu chỉ MỘT component dùng thì khai ngay trong file của component đó (như `FooterLink` ở
 * `footer.component.ts`); kiểu ở đây khai riêng vì nhiều component cần cùng một định nghĩa
 * (`Topbar`, `Menu`).
 *
 * Tệp này nằm ở `shared/ui/`, nên nó KHÔNG được import `shared/components/` (luật F24). Kiểu của
 * một component "tự dựng" — `SegmentOption` của `Input`, `NavItem` của `Sidebar`, `ToolbarChip`
 * của `Toolbar` — khai trong chính file component đó dưới `shared/components/`, không kéo lên đây.
 */

/** Khai ở `data-table.component.ts`; re-export để nơi dùng chung không phải biết đường dẫn component. */
export type { DataColumnDef } from './data-table/data-table.component';

/** Một mục trong Menu — fe-ui-conventions.md §9. */
export interface UiMenuItem {
  readonly key: string;
  readonly label: string;
  readonly icon?: string;
  readonly disabled?: boolean;
  readonly disabledReason?: string;
  readonly danger?: boolean;
  readonly group?: string;
}

/** Một nút trong cây, dùng cho TreeSelect và cho DataTable công tắc tree — fe-ui-conventions.md §9. */
export interface UiTreeNode {
  key: string;
  label: string;
  code?: string;
  children?: UiTreeNode[];
  hasChildren?: boolean; // true mà children rỗng = chưa tải
  selectable?: boolean; // false = vẫn hiện, vẫn xoè, không chọn
}

// `FilterField` KHÔNG khai ở đây. Nó là kiểu của đúng MỘT component — `FilterPanel`, mang
// `Nền: tự dựng` (Design/COMPONENTS.md §3) nên thuộc `shared/components/` — và component đó chưa
// dựng. Chữ ký gốc ở fe-ui-conventions.md §9; khai nó trong `filter-panel.component.ts` lúc dựng,
// đúng như `NavItem`, `ToolbarChip`, `FooterLink`, `SegmentOption` đang làm.
