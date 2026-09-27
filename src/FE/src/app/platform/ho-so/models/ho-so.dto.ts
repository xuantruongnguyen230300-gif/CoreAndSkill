/** Hình dạng trên dây — contracts/profile.md §1. Chỉ `services/` được import (luật F10). */
export interface HoSoDto {
  readonly userName: string;
  readonly email: string | null;
  readonly fullName: string;
  readonly phoneNumber: string | null;
  readonly preferredLanguage: string | null;
  readonly hasPermissionBypass: boolean;
  readonly version: string;
}

/** Request của `PUT /api/v1/core/profile` — contracts/profile.md §2. */
export interface CapNhatHoSoPayload {
  readonly fullName: string;
  readonly phoneNumber: string | null;
  readonly preferredLanguage: string | null;
  readonly version: string;
}
