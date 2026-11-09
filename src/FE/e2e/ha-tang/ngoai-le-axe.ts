// Tệp ngoại lệ axe DUY NHẤT của lưới giao diện (ADR-0117 QC5). Mỗi mục là một cặp *quy tắc × phần tử* kèm màn,
// lý do, ngày và căn cứ (dòng spec Design hay issue của thư viện). Không tắt quy tắc toàn cục, không loại trừ cả
// vùng. Mục không khớp vi phạm nào trong lượt chạy là ĐỎ (ngoại lệ mồ côi — `ap-dung-ngoai-le` ở `axe.ts`):
// lỗi đã sửa thì mục phải gỡ, không nằm lại che lỗi sau.
//
// Chữ vô hiệu ở theme tối cố ý dưới 4.5:1 (DESIGN.md §2.2) KHÔNG cần mục ở đây: axe bỏ qua phần tử mang thuộc tính
// vô hiệu; axe vẫn báo thì phần tử đó không vô hiệu thật — đó là lỗi, không phải ngoại lệ.

export interface NgoaiLeAxe {
  /** Mã quy tắc axe, ví dụ `color-contrast`. */
  readonly quyTac: string;
  /** Bộ chọn CSS của phần tử bị báo — khớp theo `target` mà axe trả. */
  readonly boChon: string;
  /** Mã màn ở tên tệp spec (`01-dang-nhap`...). */
  readonly man: string;
  /** Giới hạn theo project (`rong-sang`...) nếu vi phạm chỉ có ở một số khung nhìn/theme; bỏ trống = mọi project. */
  readonly project?: readonly string[];
  readonly lyDo: string;
  /** YYYY-MM-DD. */
  readonly ngay: string;
  /** Dòng spec Design hoặc issue của thư viện — bắt buộc, không để trống. */
  readonly canCu: string;
}

export const NGOAI_LE_AXE: readonly NgoaiLeAxe[] = [];
