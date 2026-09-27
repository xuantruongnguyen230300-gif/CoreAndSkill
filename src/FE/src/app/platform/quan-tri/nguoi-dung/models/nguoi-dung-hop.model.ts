import type { FormControl, FormGroup } from '@angular/forms';

import type { Option } from '../../../../shared/ui/autocomplete/autocomplete.component';

/**
 * Hình dạng form và giá trị các hộp thoại khu người dùng — MỘT nơi khai để page và các component
 * hộp (`components/`) cùng nói về nó mà component không phải import store (ADR-0049 điều 4).
 * Chỉ là kiểu: không logic, không DTO.
 */

export type SuaNguoiDungForm = FormGroup<{
  email: FormControl<string>;
  fullName: FormControl<string>;
}>;
export interface LoiTruongSuaNguoiDung {
  readonly email: string | null;
  readonly fullName: string | null;
}

/** Hộp thoại nào đang mở ở màn chi tiết — `null` là không hộp nào. */
export type LoaiHopChiTiet = 'sua' | 'gan-vai-tro' | 'dat-lai' | null;

export type DatLaiMatKhauForm = FormGroup<{ tempPassword: FormControl<string> }>;

export type TenTruongTaoNguoiDung = 'userName' | 'email' | 'fullName' | 'tempPassword';
export type TaoNguoiDungForm = FormGroup<
  Record<TenTruongTaoNguoiDung, FormControl<string>> & { roleIds: FormControl<readonly string[]> }
>;
/** Câu lỗi từng ô của hộp tạo — gồm cả ô "Vai trò" (`fieldErrors["RoleIds"]`, users.md §5). */
export type LoiTruongTaoNguoiDung = Readonly<
  Record<TenTruongTaoNguoiDung | 'roleIds', string | null>
>;

/** Trạng thái một ô chọn vai trò, gói thành MỘT giá trị để `input()` của component hộp nhận. */
export interface VaiTroChon {
  readonly options: readonly Option[];
  readonly loading: boolean;
  /** Câu lỗi của lần tìm gần nhất (`TraCuuVaiTroStore.error`) — hộp hiện nó thay cho danh sách; `null` khi không lỗi. */
  readonly error: string | null;
  readonly selected: readonly string[];
}
