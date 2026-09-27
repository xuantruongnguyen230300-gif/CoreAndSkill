/** Hình dạng trên dây — contracts/roles.md §1, §3, §5. Chỉ services/ được import (fe-api-client.md §4.1). */
export interface VaiTroDto {
  readonly id: string;
  readonly name: string;
  readonly isSystem: boolean;
  readonly userCount: number;
  readonly createdAt: string | null;
  /** Token đồng thời — gửi lại nguyên chuỗi khi đổi tên (roles.md §3). */
  readonly version: string;
}
