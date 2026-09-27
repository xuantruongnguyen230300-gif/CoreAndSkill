/**
 * Request chung của `POST /api/v1/core/auth/change-password` và
 * `POST /api/v1/core/auth/change-password-required` — contracts/auth.md §6, §7. Phản chiếu 1:1 hình
 * dạng trên dây; lệch thì card thắng. Chỉ `services/` (ở đây là `doi-mat-khau.service.ts`) được
 * import kiểu này (luật F10, fe-api-client.md §4.1).
 */
export interface DoiMatKhauPayload {
  readonly currentPassword: string;
  readonly newPassword: string;
}
