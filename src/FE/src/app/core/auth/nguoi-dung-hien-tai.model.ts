/** Model màn hình — hình dạng của MÀN HÌNH, không phải của dây (fe-api-client.md §4). */
export interface NguoiDungHienTai {
  readonly id: string;
  readonly userName: string;
  readonly email: string | null;
  readonly fullName: string;
  readonly roles: readonly string[];
  readonly mustChangePassword: boolean;
  readonly isSystemOperator: boolean;
  readonly sessionMinutes: number;
  readonly preferredLanguage: string | null;
  readonly tenantCode: string;
  readonly tenantName: string;
  /** Đổi tên `permissions` → `quyen`: đây là model, không phải DTO. */
  readonly quyen: readonly string[];
}
