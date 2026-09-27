/** Model màn hình — hình dạng của MÀN HÌNH, không phải của dây (fe-api-client.md §4). */
export interface VaiTroRutGon {
  readonly id: string;
  readonly name: string;
  readonly isSystem: boolean;
}

export interface NguoiDung {
  readonly id: string;
  readonly userName: string;
  readonly email: string | null;
  readonly fullName: string;
  readonly roles: readonly VaiTroRutGon[];
  readonly isLocked: boolean;
  readonly lockoutEnd: Date | null;
  readonly lockedByAdmin: boolean;
  readonly mustChangePassword: boolean;
  readonly createdAt: Date | null;
  /** Token đồng thời — gửi lại nguyên chuỗi ở mọi thao tác ghi (wiki-core/be/06-concurrency-control.md §6.3). */
  readonly version: string;
}
