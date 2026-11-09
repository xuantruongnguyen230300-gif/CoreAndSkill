/**
 * Giới hạn độ dài của BE (`ReportClientErrorCommandValidator`, docs/contracts/client-errors.md §1).
 * BE TỪ CHỐI khi vượt, không cắt hộ — nên FE phải cắt TRƯỚC khi gửi, nếu không cả báo cáo bị bỏ.
 */
export const GIOI_HAN_BAO_CAO = {
  kind: 100,
  message: 500,
  stack: 4000,
  duongDan: 2000,
  traceId: 100,
  phienBanApp: 100,
} as const;

/** BE từ chối `message` rỗng; lỗi ném ra không kèm câu (`throw new Error()`) vẫn phải báo được. */
const CAU_KHONG_CO_THONG_DIEP = '(không có thông điệp)';

/** Bản dựng không có mã commit (máy dev) — BE từ chối `phienBanApp` rỗng nên không để trống. */
const PHIEN_BAN_KHONG_RO = 'khong-ro';

/** Tên `kind` cho giá trị bị ném mà không phải `Error` (chuỗi, object trần, `undefined`…). */
const KIND_KHONG_PHAI_ERROR = 'NonError';

/**
 * Hình dạng báo cáo gửi đi — khớp từng field của `POST /api/v1/core/client-errors`. Mọi đầu ra
 * (`console`, BE) nhận ĐÚNG bản này: ba ràng buộc của 10-observability.md §4.1 áp cho mọi đầu ra
 * nên chúng được thi hành ở lúc DỰNG báo cáo, không ở từng đầu ra.
 */
export interface ClientErrorReport {
  readonly kind: string;
  readonly message: string;
  readonly stack?: string;
  readonly duongDan: string;
  readonly traceId?: string;
  readonly phienBanApp: string;
}

/** Ba giá trị lấy từ ngữ cảnh lúc lỗi xảy ra — không nằm trong chính giá trị lỗi. */
export interface NguCanhBaoCao {
  /** Đường dẫn trình duyệt lúc lỗi. Hàm dựng cắt query và fragment nếu người gọi lỡ truyền. */
  readonly duongDan: string;
  readonly traceId: string | null;
  readonly phienBanApp: string;
}

const QUERY_TRONG_VAN = /(https?:\/\/[^\s?#()'"`]+)[?#][^\s()'"`]*/g;
const QUERY_TRONG_STACK = /(https?:\/\/[^\s?#()'"`]+)[?#][^\s()'"`:]*/g;

/**
 * Bỏ query và fragment khỏi mọi URL `http(s)://…` trong một đoạn văn bản — query có thể mang giá trị
 * người dùng nhập (client-errors.md §1, field `duongDan`).
 *
 * `giuViTri`: trong `stack`, URL kết thúc bằng `:dòng:cột` (`main.js?v=ab:1:2481`) mà chính hai số đó
 * là phần có ích nhất của khung — nên với `stack`, query dừng ở dấu `:` đầu tiên và `:dòng:cột` ở lại.
 * Với `message` không có vị trí nào để giữ, query bị bỏ tới hết URL. Hệ quả chấp nhận: query trong
 * `stack` có dấu `:` thì phần sau dấu đó ở lại — URL trong `stack` là URL của tệp mã, không phải
 * đường dẫn màn, và BE còn lọc lần hai.
 */
export function catQueryTrongUrl(van: string, giuViTri = false): string {
  return van.replace(giuViTri ? QUERY_TRONG_STACK : QUERY_TRONG_VAN, '$1');
}

function cat(van: string, toiDa: number): string {
  return van.length > toiDa ? van.slice(0, toiDa) : van;
}

function laObject(giaTri: unknown): giaTri is Record<string, unknown> {
  return typeof giaTri === 'object' && giaTri !== null;
}

/** `kind`: tên lỗi. Không lấy tên lớp từ object lạ — chỉ `Error` mới có tên đáng tin. */
function layKind(loi: unknown): string {
  if (loi instanceof Error && loi.name.trim() !== '') {
    return loi.name;
  }
  return loi instanceof Error ? 'Error' : KIND_KHONG_PHAI_ERROR;
}

/**
 * `message`: chỉ lấy văn bản NẰM SẴN trong giá trị lỗi — `Error.message`, chuỗi bị ném, hay trường
 * `message` kiểu chuỗi. KHÔNG `String(obj)`, KHÔNG `JSON.stringify`, KHÔNG đọc `cause`/`error`/`body`:
 * một object bị ném có thể là gói dữ liệu form, và đó đúng là thứ §4.1 cấm gửi.
 */
function layMessage(loi: unknown): string {
  if (typeof loi === 'string') {
    return loi;
  }
  if (loi instanceof Error) {
    return loi.message;
  }
  if (laObject(loi) && typeof loi['message'] === 'string') {
    return loi['message'];
  }
  return '';
}

/**
 * Dựng báo cáo từ một giá trị lỗi bất kỳ. Hàm thuần, không ném: đầu vào nào cũng ra một báo cáo
 * hợp lệ với BE (mọi field bắt buộc không rỗng, mọi field trong giới hạn độ dài).
 */
export function dungBaoCao(loi: unknown, nguCanh: NguCanhBaoCao): ClientErrorReport {
  const message = catQueryTrongUrl(layMessage(loi)).trim();
  const stackGoc = loi instanceof Error && typeof loi.stack === 'string' ? loi.stack : '';
  const stack = catQueryTrongUrl(stackGoc, true).trim();
  const duongDan = nguCanh.duongDan.split(/[?#]/)[0];
  const traceId = nguCanh.traceId?.trim() ?? '';
  const phienBanApp = nguCanh.phienBanApp.trim();

  return {
    kind: cat(layKind(loi), GIOI_HAN_BAO_CAO.kind),
    message: cat(message === '' ? CAU_KHONG_CO_THONG_DIEP : message, GIOI_HAN_BAO_CAO.message),
    ...(stack === '' ? {} : { stack: cat(stack, GIOI_HAN_BAO_CAO.stack) }),
    duongDan: cat(duongDan === '' ? '/' : duongDan, GIOI_HAN_BAO_CAO.duongDan),
    ...(traceId === '' ? {} : { traceId: cat(traceId, GIOI_HAN_BAO_CAO.traceId) }),
    phienBanApp: cat(
      phienBanApp === '' ? PHIEN_BAN_KHONG_RO : phienBanApp,
      GIOI_HAN_BAO_CAO.phienBanApp,
    ),
  };
}

/** Dấu vân tay để gộp cùng một lỗi lặp lại — cùng loại, cùng câu, cùng đầu `stack`. */
export function dauVanTay(baoCao: ClientErrorReport): string {
  return `${baoCao.kind}\n${baoCao.message}\n${(baoCao.stack ?? '').slice(0, 300)}`;
}
