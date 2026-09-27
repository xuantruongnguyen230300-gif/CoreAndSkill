import { HttpResponse } from '@angular/common/http';

/**
 * Tải tệp xuất về máy — fe-api-client.md §6.4, ADR-0062.
 *
 * Endpoint xuất đòi `X-XSRF-TOKEN` dù là `GET`, mà điều hướng không mang được header: không
 * `window.open`, không `<a href>` trỏ thẳng API, không `<form>`. Tệp vì thế đi qua bộ nhớ trình
 * duyệt dưới dạng `Blob`, và FE tự kích hoạt tải. Feature KHÔNG viết lại đoạn này.
 */

const HEADER_TEN_TEP = 'Content-Disposition';

/** `filename*=<charset>'<lang>'<tên đã percent-encode>` — RFC 5987, nơi tên có dấu đi qua. */
const MAU_RFC5987 = /filename\*\s*=\s*[^']*'[^']*'([^;]+)/i;
/** `filename="…"` — bản ASCII bỏ dấu cho client cũ. Không khớp `filename*=` vì sau tên là `*`. */
const MAU_NHAY = /filename\s*=\s*"([^"]*)"/i;
const MAU_TRAN = /filename\s*=\s*([^;]+)/i;

/**
 * Tên tệp đọc từ một chuỗi `Content-Disposition`: `filename*` TRƯỚC (giải mã percent-encoding),
 * `filename` sau. Đọc nhầm thứ tự thì tệp ra tên không dấu — đúng cú pháp, sai câu chữ, không ai báo.
 * Trả `null` khi chuỗi không mang tên tệp nào.
 */
export function tenTepTuContentDisposition(header: string): string | null {
  const rfc5987 = MAU_RFC5987.exec(header);
  if (rfc5987) {
    return decodeURIComponent(rfc5987[1].trim());
  }
  const nhay = MAU_NHAY.exec(header);
  if (nhay) {
    return nhay[1].trim();
  }
  const tran = MAU_TRAN.exec(header);
  return tran ? tran[1].trim() : null;
}

/**
 * Tên tệp của một phản hồi xuất. **Header đọc ra `null` là LỖI, không phải lý do đặt tên dự phòng**:
 * API ở origin khác nên `headers.get` trả `null` khi BE chưa khai `Content-Disposition` vào
 * `Access-Control-Expose-Headers` (be-api-controller.md §7.3) — không lỗi, không cảnh báo. Đặt tên
 * dự phòng là giấu đúng lỗi đó; ném thì nó lộ ra ở lần dùng đầu tiên trên máy dev.
 */
export function tenTepTuPhanHoi(res: HttpResponse<Blob>): string {
  const header = res.headers.get(HEADER_TEN_TEP);
  if (header === null) {
    throw new Error(
      `Phản hồi xuất không đọc được header ${HEADER_TEN_TEP}. API khác origin nên BE phải khai ` +
        'header này trong Access-Control-Expose-Headers (be-api-controller.md §7.3).',
    );
  }
  const ten = tenTepTuContentDisposition(header);
  if (ten === null || ten === '') {
    throw new Error(`Header ${HEADER_TEN_TEP} không mang tên tệp: ${header}`);
  }
  return ten;
}

/**
 * Kích hoạt tải một blob về máy: object URL, thẻ `<a download>` tạm trong DOM, click, rồi
 * `revokeObjectURL`. `URL.createObjectURL` KHÔNG tự thu hồi — quên thu hồi là rò bộ nhớ đúng bằng
 * cỡ tệp, mỗi lần bấm; vì vậy việc thu hồi và gỡ thẻ nằm trong `finally`.
 */
export function kichHoatTai(blob: Blob, tenTep: string): void {
  const url = URL.createObjectURL(blob);
  const the = document.createElement('a');
  the.href = url;
  the.download = tenTep;
  the.rel = 'noopener';
  // Thẻ phải nằm trong DOM lúc click — Firefox bỏ qua click của một thẻ rời.
  document.body.appendChild(the);
  try {
    the.click();
  } finally {
    the.remove();
    URL.revokeObjectURL(url);
  }
}

/** Lưu phản hồi của một endpoint xuất về máy: tên từ `Content-Disposition`, rồi kích hoạt tải. */
export function luuTepXuat(res: HttpResponse<Blob>): void {
  const blob = res.body;
  if (blob === null) {
    throw new Error('Phản hồi xuất có thân phản hồi rỗng — không có gì để lưu.');
  }
  kichHoatTai(blob, tenTepTuPhanHoi(res));
}
