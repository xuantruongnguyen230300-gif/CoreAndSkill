/** Model màn hình — hình dạng của MÀN HÌNH, không phải của dây (fe-api-client.md §4). */
export interface DonVi {
  readonly id: string;
  readonly code: string;
  readonly name: string;
  readonly isActive: boolean;
  readonly createdAt: Date | null;
}
