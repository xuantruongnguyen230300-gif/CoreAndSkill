// Dữ liệu giả phiên đăng nhập và menu. Card nguồn: docs/contracts/auth.md §3, §5 (`PhienDto` — login và me
// cùng một hình dạng) và docs/contracts/meta-menu.md §1 (menu PHẲNG, mảng đã sắp: mục gốc trước, rồi
// `displayOrder`, rồi `code`). Mỗi hàm tạo một ca rõ tên để spec màn gọi; envelope chỉ dựng qua `ha-tang/envelope`.
import type { PhienDto } from '../../src/app/core/auth/phien.dto';
import type { MenuItemDto } from '../../src/app/core/menu/menu.dto';
import type { CaGia } from '../ha-tang/api-gia';
import { thanhCong } from '../ha-tang/envelope';
import { loiHeThongKhongLuong } from './chung';

/** Quyền của một quản trị đơn vị nghiệp vụ có đủ ba màn quản trị — khoá lấy từ docs/database/schema-core.md §5.2. */
export const QUYEN_QUAN_TRI_DON_VI: readonly string[] = [
  'core.menu.read',
  'core.user.read',
  'core.user.write',
  'core.user.lock',
  'core.user.reset-password',
  'core.user.role.assign',
  'core.user.export',
  'core.role.read',
  'core.role.write',
  'core.permission.read',
  'core.permission.write',
];

export interface TuyChonPhien {
  /** `true` ⇒ FE điều hướng sang màn đổi mật khẩu bắt buộc. Mặc định `false`. */
  readonly mustChangePassword?: boolean;
  /** Ghi đè tập quyền hiệu lực (ví dụ chỉ quyền đọc người dùng để thử guard). */
  readonly permissions?: readonly string[];
  /** Ghi đè họ tên — dùng chuỗi dài để thử tràn ở topbar và trang chủ. */
  readonly fullName?: string;
  readonly email?: string | null;
  readonly tenantName?: string;
}

/** Quản trị của một đơn vị NGHIỆP VỤ: `isSystemOperator = false`, có quyền quản trị. */
export function phienQuanTriDonVi(tuyChon: TuyChonPhien = {}): PhienDto {
  return {
    id: '0192f3c1-8a4e-7d21-9f10-2b7c5e0a1234',
    userName: 'an.nv',
    email: tuyChon.email === undefined ? 'an.nv@don-vi-mau.example' : tuyChon.email,
    fullName: tuyChon.fullName ?? 'Nguyễn Văn An',
    roles: ['Quản trị hệ thống'],
    permissions: tuyChon.permissions ?? QUYEN_QUAN_TRI_DON_VI,
    mustChangePassword: tuyChon.mustChangePassword ?? false,
    isSystemOperator: false,
    sessionMinutes: 30,
    preferredLanguage: 'vi',
    tenantCode: 'DON-VI-MAU',
    tenantName: tuyChon.tenantName ?? 'Đơn vị mẫu',
  };
}

/**
 * Tài khoản vận hành ĐƠN VỊ HỆ THỐNG: `isSystemOperator = true`, không vai trò, không quyền nào —
 * cờ vận hành không nằm trong `permissions` (auth.md §3, dòng `isSystemOperator`).
 */
export function phienVanHanhHeThong(tuyChon: TuyChonPhien = {}): PhienDto {
  return {
    id: '0192f3c1-9b5f-7e32-8a21-3c8d6f1b2345',
    userName: 'van.hanh',
    email: tuyChon.email === undefined ? 'van.hanh@he-thong.example' : tuyChon.email,
    fullName: tuyChon.fullName ?? 'Trần Thị Vận Hành',
    roles: [],
    permissions: tuyChon.permissions ?? [],
    mustChangePassword: tuyChon.mustChangePassword ?? false,
    isSystemOperator: true,
    sessionMinutes: 30,
    preferredLanguage: 'vi',
    tenantCode: 'HE-THONG',
    tenantName: tuyChon.tenantName ?? 'Đơn vị hệ thống',
  };
}

/** `GET /core/auth/me` 200 với phiên cho trước. */
export function meDaDangNhap(phien: PhienDto): CaGia {
  return { phuongThuc: 'GET', duongDan: '/core/auth/me', phanHoi: thanhCong(phien) };
}

/** `me` của quản trị đơn vị nghiệp vụ, `mustChangePassword = false`. */
export function meQuanTriDonVi(tuyChon: TuyChonPhien = {}): CaGia {
  return meDaDangNhap(phienQuanTriDonVi(tuyChon));
}

/** `me` của tài khoản vận hành hệ thống, `mustChangePassword = false`. */
export function meVanHanhHeThong(tuyChon: TuyChonPhien = {}): CaGia {
  return meDaDangNhap(phienVanHanhHeThong(tuyChon));
}

/** `me` của quản trị đơn vị mang mật khẩu tạm: `mustChangePassword = true` (auth.md §1.2). */
export function meQuanTriBatBuocDoiMatKhau(): CaGia {
  return meQuanTriDonVi({ mustChangePassword: true });
}

/** `me` của tài khoản vận hành mang mật khẩu tạm: `mustChangePassword = true`. */
export function meVanHanhBatBuocDoiMatKhau(): CaGia {
  return meVanHanhHeThong({ mustChangePassword: true });
}

/** `POST /core/auth/login` 200 — cùng hình dạng với `me` (auth.md §3). */
export function dangNhapThanhCong(phien: PhienDto = phienQuanTriDonVi()): CaGia {
  return { phuongThuc: 'POST', duongDan: '/core/auth/login', phanHoi: thanhCong(phien) };
}

/** `POST /core/auth/logout` 200 (auth.md §4). */
export function dangXuatThanhCong(): CaGia {
  return { phuongThuc: 'POST', duongDan: '/core/auth/logout', phanHoi: thanhCong(null) };
}

const GOC = 'quan-tri';

/** Menu mà quản trị đơn vị nghiệp vụ thấy — đúng tập Core seed (CoreTenantSeedSource, meta-menu.md §1). */
export function menuQuanTriDonViDto(): readonly MenuItemDto[] {
  return [
    {
      id: '0192f3e0-0001-7000-8000-000000000001',
      parentId: null,
      code: 'trang-chu',
      labelKey: 'menu.trang-chu',
      icon: 'pi-home',
      route: '/trang-chu',
      displayOrder: 10,
    },
    {
      id: '0192f3e0-0001-7000-8000-000000000002',
      parentId: null,
      code: GOC,
      labelKey: 'menu.quan-tri',
      icon: 'pi-cog',
      route: null,
      displayOrder: 90,
    },
    {
      id: '0192f3e0-0001-7000-8000-000000000003',
      parentId: '0192f3e0-0001-7000-8000-000000000002',
      code: 'quan-tri-nguoi-dung',
      labelKey: 'menu.quan-tri-nguoi-dung',
      icon: 'pi-users',
      route: '/quan-tri/nguoi-dung',
      displayOrder: 10,
    },
    {
      id: '0192f3e0-0001-7000-8000-000000000004',
      parentId: '0192f3e0-0001-7000-8000-000000000002',
      code: 'quan-tri-vai-tro',
      labelKey: 'menu.quan-tri-vai-tro',
      icon: 'pi-shield',
      route: '/quan-tri/vai-tro',
      displayOrder: 20,
    },
    {
      id: '0192f3e0-0001-7000-8000-000000000005',
      parentId: '0192f3e0-0001-7000-8000-000000000002',
      code: 'quan-tri-phan-quyen',
      labelKey: 'menu.quan-tri-phan-quyen',
      icon: 'pi-key',
      route: '/quan-tri/phan-quyen',
      displayOrder: 30,
    },
  ];
}

/**
 * Menu của tài khoản vận hành hệ thống: chỉ mục `trang-chu` — mục không đòi quyền hay vai trò nào hiện cho mọi
 * người đã đăng nhập (meta-menu.md §1.4 bước 3); ba mục quản trị đòi quyền mà người vận hành không có.
 * Màn `/he-thong/don-vi` không có mục menu (Design/Screens/20-don-vi.md, ADR-0079).
 */
export function menuVanHanhHeThongDto(): readonly MenuItemDto[] {
  return menuQuanTriDonViDto().filter((m) => m.code === 'trang-chu');
}

function caMenu(items: readonly MenuItemDto[]): CaGia {
  return { phuongThuc: 'GET', duongDan: '/core/meta/menu', phanHoi: thanhCong(items) };
}

export function menuQuanTriDonVi(): CaGia {
  return caMenu(menuQuanTriDonViDto());
}

export function menuVanHanhHeThong(): CaGia {
  return caMenu(menuVanHanhHeThongDto());
}

/** Menu rỗng — người dùng đăng nhập được nhưng chưa mục nào hiện cho họ. */
export function menuRong(): CaGia {
  return caMenu([]);
}

/** Lỗi tải menu có chủ ý: 500 `CORE.SYSTEM.UNEXPECTED` (auth.md §11) — Sidebar hiện trạng thái lỗi kèm Thử lại. */
export function menuLoiTai(): CaGia {
  return {
    phuongThuc: 'GET',
    duongDan: '/core/meta/menu',
    phanHoi: loiHeThongKhongLuong(),
    loiCoChuY: true,
  };
}
