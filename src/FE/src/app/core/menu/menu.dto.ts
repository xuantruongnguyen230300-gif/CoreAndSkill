/**
 * Hình dạng PHẲNG của `GET /api/v1/core/meta/menu` (contracts/meta-menu.md §1). Phản chiếu 1:1 —
 * lệch thì card BE thắng, sửa ở đây. Chỉ `menu.service.ts` và mapper import kiểu này (luật F10).
 */
export interface MenuItemDto {
  readonly id: string;
  readonly parentId: string | null;
  readonly code: string;
  /** Khoá i18n, KHÔNG phải nhãn (meta-menu.md §1.2). */
  readonly labelKey: string;
  readonly icon: string | null;
  readonly route: string | null;
  readonly displayOrder: number;
}
