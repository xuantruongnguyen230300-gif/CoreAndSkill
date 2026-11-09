/** Hình dạng của `environment` — mọi tệp môi trường (kể cả tệp của bản cài) phải khớp. */
export interface MoiTruong {
  readonly production: boolean;
  readonly apiBaseUrl: string;
  readonly tenantCode: string;
}

/** Từ chối chuỗi rỗng ở mức kiểu: literal `''` thành `never`, mọi chuỗi khác đi qua. */
type KhacRong<T extends string> = T extends '' ? never : T;

/**
 * Hàm khai báo giá trị cho tệp môi trường của bản cài (ADR-0116 QC4, lớp 2): `tenantCode` để
 * rỗng thì LỖI BIÊN DỊCH, build `production` hỏng. Chỉ bắt chuỗi rỗng viết thẳng (literal) — mã
 * sai mà không rỗng, hay tệp viết tay không qua hàm này, thì không lớp nào bắt.
 */
export function khaiBaoMoiTruong<const T extends string>(
  moiTruong: Omit<MoiTruong, 'tenantCode'> & { readonly tenantCode: T & KhacRong<T> },
): MoiTruong {
  return moiTruong;
}
