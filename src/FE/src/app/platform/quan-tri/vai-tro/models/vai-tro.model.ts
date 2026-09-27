/** Model màn hình — hình dạng của MÀN HÌNH, không phải của dây (fe-api-client.md §4). */
export interface VaiTro {
  readonly id: string;
  readonly name: string;
  readonly isSystem: boolean;
  readonly userCount: number;
  readonly createdAt: Date | null;
  /** Token đồng thời, chuỗi mờ — không suy diễn, không so thứ tự; chỉ gửi lại khi ghi. */
  readonly version: string;
}
