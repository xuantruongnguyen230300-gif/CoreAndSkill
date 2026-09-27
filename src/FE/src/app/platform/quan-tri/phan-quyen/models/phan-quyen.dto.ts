/** Hình dạng trên dây — contracts/permissions.md §5, §6. Chỉ services/ được import (fe-api-client.md §4.1). */
export interface MaTranVaiTroDto {
  readonly id: string;
  readonly name: string;
  readonly isSystem: boolean;
}

export interface MaTranHangDto {
  readonly permissionId: string;
  readonly code: string;
  readonly resourceKey: string;
  readonly resourceNameKey: string;
  readonly nameKey: string;
  readonly grantedRoleIds: readonly string[];
}

export interface MaTranPhanQuyenDto {
  readonly roles: readonly MaTranVaiTroDto[];
  readonly rows: readonly MaTranHangDto[];
  readonly version: string;
}

/** contracts/permissions.md §6 — request. */
export interface CapNhatMaTranEntry {
  readonly permissionId: string;
  readonly roleIds: readonly string[];
}

export interface CapNhatMaTranPayload {
  readonly version: string;
  readonly entries: readonly CapNhatMaTranEntry[];
}

export interface CapNhatMaTranResultDto {
  readonly version: string;
}
