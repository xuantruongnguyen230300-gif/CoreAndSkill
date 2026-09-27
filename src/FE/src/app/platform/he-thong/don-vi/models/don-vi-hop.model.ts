import type { FormControl, FormGroup } from '@angular/forms';

/**
 * Hình dạng form và câu lỗi của các hộp thoại khu đơn vị — chỗ MỘT nơi khai để store (`state/`) và
 * component hộp (`components/`) cùng nói về nó mà component không phải import store (ADR-0049 điều 4).
 * Chỉ là kiểu: không logic, không DTO.
 */

export type TenTruongTaoDonVi =
  'code' | 'name' | 'adminUserName' | 'adminEmail' | 'adminFullName' | 'adminTempPassword';

export type TaoDonViForm = FormGroup<Record<TenTruongTaoDonVi, FormControl<string>>>;
export type LoiTruongTaoDonVi = Readonly<Record<TenTruongTaoDonVi, string | null>>;

export type LoaiHopQuanTri = 'khoi-phuc' | 'tao-quan-tri' | null;

export type TenTruongKhoiPhuc = 'userName' | 'tempPassword' | 'goLaiMa';
export type TenTruongTaoQuanTri = 'userName' | 'email' | 'fullName' | 'tempPassword' | 'goLaiMa';

export type KhoiPhucForm = FormGroup<Record<TenTruongKhoiPhuc, FormControl<string>>>;
export type TaoQuanTriForm = FormGroup<Record<TenTruongTaoQuanTri, FormControl<string>>>;

/** Câu lỗi từng ô của cả hai hộp quản trị, tính sẵn — một giá trị để `input()` của component nhận. */
export interface LoiTruongQuanTri {
  readonly khoiPhuc: Readonly<Record<TenTruongKhoiPhuc, string | null>>;
  readonly taoQuanTri: Readonly<Record<TenTruongTaoQuanTri, string | null>>;
}
