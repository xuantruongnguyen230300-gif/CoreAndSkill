// ADR-0117 QC2, dòng "Mã lỗi": mọi `code` trong dữ liệu giả lỗi phải có trong `docs/contracts/`.
// Hạ tầng đọc các card lúc chạy, trích mã trong code span; mã không có là đỏ — không bịa mã.
// Trích được 0 mã cũng là đỏ (thư mục sai chỗ hay card đổi khuôn thì không được xanh im lặng).
import { readdirSync, readFileSync } from 'node:fs';
import { join, resolve } from 'node:path';

export const THU_MUC_CONTRACTS = resolve(__dirname, '../../../../docs/contracts');

/** Mã dạng `MODULE.NHOM.TEN` (>= 3 đoạn, chữ hoa) nằm trong một code span. */
const MA_TRONG_CODE_SPAN = /`([A-Z][A-Z0-9_]*(?:\.[A-Z][A-Z0-9_]*){2,})`/g;
/** Hàng bảng lỗi: | `code` | `Type` | 401 | ... — nguồn của cặp type/HTTP khi card có khai. */
const HANG_BANG_LOI =
  /^\|\s*`([A-Z][A-Z0-9_]*(?:\.[A-Z][A-Z0-9_]*){2,})`\s*\|\s*`([A-Za-z]+)`\s*\|\s*(\d{3})\s*\|/gm;

export interface DanhMucMaLoi {
  /** Mọi mã tìm thấy trong code span. */
  readonly ma: ReadonlySet<string>;
  /** Cặp type/HTTP do bảng lỗi của card khai; mã không có hàng bảng thì không có khoá. */
  readonly khaiBao: ReadonlyMap<
    string,
    readonly { readonly type: string; readonly status: number }[]
  >;
}

export function docDanhMucMaLoi(thuMuc: string = THU_MUC_CONTRACTS): DanhMucMaLoi {
  const ma = new Set<string>();
  const khaiBao = new Map<string, { type: string; status: number }[]>();
  for (const ten of readdirSync(thuMuc).filter((t) => t.endsWith('.md'))) {
    const noiDung = readFileSync(join(thuMuc, ten), 'utf8');
    for (const m of noiDung.matchAll(MA_TRONG_CODE_SPAN)) ma.add(m[1]!);
    for (const m of noiDung.matchAll(HANG_BANG_LOI)) {
      const ds = khaiBao.get(m[1]!) ?? [];
      ds.push({ type: m[2]!, status: Number(m[3]) });
      khaiBao.set(m[1]!, ds);
    }
  }
  if (ma.size === 0) {
    throw new Error(
      `Trích được 0 mã lỗi từ ${thuMuc} — thư mục sai chỗ hoặc card đổi khuôn; không coi là xanh.`,
    );
  }
  return { ma, khaiBao };
}

let macDinh: DanhMucMaLoi | undefined;

/** Ném lỗi nếu `code` không có trong card (dùng cho mã theo ô nhập, không có type/HTTP riêng). */
export function kiemTraCoMa(
  code: string,
  danhMuc: DanhMucMaLoi = (macDinh ??= docDanhMucMaLoi()),
): void {
  if (!danhMuc.ma.has(code)) {
    throw new Error(`Mã lỗi "${code}" không có trong docs/contracts/ — không được bịa mã.`);
  }
}

/** Ném lỗi nếu `code` không có trong card, hoặc `type`/`status` lệch với hàng bảng lỗi của card. */
export function kiemTraMaLoi(
  code: string,
  type: string,
  status: number,
  danhMuc: DanhMucMaLoi = (macDinh ??= docDanhMucMaLoi()),
): void {
  kiemTraCoMa(code, danhMuc);
  const khai = danhMuc.khaiBao.get(code);
  if (khai && !khai.some((k) => k.type === type && k.status === status)) {
    const dung = khai.map((k) => `${k.type}/${k.status}`).join(' hoặc ');
    throw new Error(
      `Mã lỗi "${code}" khai ${dung} ở docs/contracts/, dữ liệu giả ghi ${type}/${status}.`,
    );
  }
}
