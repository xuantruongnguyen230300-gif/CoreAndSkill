/** Hình dạng trên dây — contracts/users.md §3, §4. Chỉ services/ được import (fe-api-client.md §4.1). */
export interface VaiTroRutGonDto {
  readonly id: string;
  readonly name: string;
  readonly isSystem: boolean;
}

export interface NguoiDungDto {
  readonly id: string;
  readonly userName: string;
  readonly email: string | null;
  readonly fullName: string;
  readonly roles: readonly VaiTroRutGonDto[];
  readonly isLocked: boolean;
  readonly lockoutEnd: string | null;
  readonly lockedByAdmin: boolean;
  readonly mustChangePassword: boolean;
  readonly createdAt: string | null;
  readonly version: string;
}

/** contracts/users.md §5 — payload tạo mới. */
export interface TaoNguoiDungPayload {
  readonly userName: string;
  readonly email: string;
  readonly fullName: string;
  readonly tempPassword: string;
  readonly roleIds: readonly string[];
}

/** contracts/users.md §6 — KHÔNG có `userName`, KHÔNG đụng vai trò. */
export interface CapNhatNguoiDungPayload {
  readonly email: string;
  readonly fullName: string;
  readonly version: string;
}

/** contracts/users.md §7 — THAY THẾ toàn bộ tập vai trò. `version` là token của TÀI KHOẢN ĐÍCH (ADR-0082). */
export interface GanVaiTroPayload {
  readonly roleIds: readonly string[];
  readonly version: string;
}

/** contracts/users.md §8, §9 — dùng chung cho lock/unlock/reset-password. */
export interface VersionedPayload {
  readonly version: string;
}

export interface DatLaiMatKhauPayload {
  readonly tempPassword: string;
  readonly version: string;
}
