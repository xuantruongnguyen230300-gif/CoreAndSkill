/**
 * DTO phiên — CÙNG hình dạng cho `login` (200) và `me` (200), contracts/auth.md §3, §5.
 * Chỉ `services/` được import kiểu này (luật F10, fe-api-client.md §4.1).
 */
export interface PhienDto {
  readonly id: string;
  readonly userName: string;
  readonly email: string | null;
  readonly fullName: string;
  readonly roles: readonly string[];
  readonly permissions: readonly string[];
  readonly mustChangePassword: boolean;
  readonly isSystemOperator: boolean;
  readonly sessionMinutes: number;
  readonly preferredLanguage: string | null;
  readonly tenantCode: string;
  readonly tenantName: string;
}

/** Request của `POST /api/v1/core/auth/login` — contracts/auth.md §3. */
export interface DangNhapPayload {
  readonly tenantCode: string;
  readonly userName: string;
  readonly password: string;
}
