/** Model màn hình — fe-api-client.md §4. */
export interface HoSo {
  readonly userName: string;
  readonly email: string | null;
  readonly hoTen: string;
  readonly soDienThoai: string | null;
  readonly ngonNguUaThich: string | null;
  readonly coDacQuyen: boolean;
  readonly version: string;
}
