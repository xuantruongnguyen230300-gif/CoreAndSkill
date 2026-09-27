/** Hình dạng trên dây — contracts/tenants.md §1. Chỉ services/ được import (fe-api-client.md §4.1). */
export interface DonViDto {
  readonly id: string;
  readonly code: string;
  readonly name: string;
  readonly isActive: boolean;
  readonly createdAt: string | null;
}

/** contracts/tenants.md §2 — payload tạo đơn vị CỘNG tài khoản quản trị đầu tiên (một transaction). */
export interface TaoDonViPayload {
  readonly code: string;
  readonly name: string;
  readonly adminUserName: string;
  readonly adminEmail: string;
  readonly adminFullName: string;
  readonly adminTempPassword: string;
}

/** contracts/tenants.md §4 — mở lại cửa quản trị của MỘT đơn vị đã có. */
export interface KhoiPhucQuanTriPayload {
  readonly userName: string;
  readonly tempPassword: string;
}

/** contracts/tenants.md §6 — tài khoản quản trị MỚI cho một đơn vị đã có. */
export interface TaoQuanTriMoiPayload {
  readonly userName: string;
  readonly email: string;
  readonly fullName: string;
  readonly tempPassword: string;
}
