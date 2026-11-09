// Mảnh dùng chung của dữ liệu giả: khuôn danh sách phân trang và các phản hồi lỗi chung.
// Card nguồn: docs/contracts/README.md §8 (danh sách: `items`, `page`, `pageSize`, `totalCount`) và
// docs/contracts/auth.md §11 (mã lỗi dùng chung — `type` và HTTP khai ở đó).
import type { PagedList } from '../../src/app/core/http/paged.model';
import { thanhCong, thatBai } from '../ha-tang/envelope';
import type { PhanHoi } from '../ha-tang/envelope';

export interface TuyChonTrang {
  /** Tổng số bản ghi của cả danh sách. Mặc định bằng số phần tử của trang. */
  readonly totalCount?: number;
  readonly page?: number;
  readonly pageSize?: number;
}

/** Phản hồi 200 của một endpoint danh sách. */
export function trangDanhSach<T>(items: readonly T[], tuyChon: TuyChonTrang = {}): PhanHoi {
  const data: PagedList<T> = {
    items,
    page: tuyChon.page ?? 1,
    pageSize: tuyChon.pageSize ?? 20,
    totalCount: tuyChon.totalCount ?? items.length,
  };
  return thanhCong(data);
}

/** 500 `CORE.SYSTEM.UNEXPECTED` — lỗi tải có chủ ý của mọi màn. Ca dùng nó phải khai `loiCoChuY: true`. */
export function loiHeThongKhongLuong(): PhanHoi {
  return thatBai('CORE.SYSTEM.UNEXPECTED', { type: 'Unexpected', status: 500 });
}
