/** Model màn hình — hình dạng của MÀN HÌNH, không phải của dây (fe-api-client.md §4). */
export interface MaTranVaiTro {
  readonly id: string;
  readonly name: string;
  readonly isSystem: boolean;
}

export interface MaTranHang {
  readonly permissionId: string;
  readonly code: string;
  readonly resourceKey: string;
  readonly resourceNameKey: string;
  readonly nameKey: string;
  readonly grantedRoleIds: readonly string[];
}

export interface MaTranPhanQuyen {
  readonly roles: readonly MaTranVaiTro[];
  readonly rows: readonly MaTranHang[];
  readonly version: string;
}
