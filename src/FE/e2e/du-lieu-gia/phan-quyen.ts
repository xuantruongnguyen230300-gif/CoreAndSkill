// Dữ liệu giả của màn ma trận phân quyền. Card nguồn: docs/contracts/permissions.md §5
// (`GET /core/permissions/matrix`): `roles` liệt kê MỌI vai trò, `rows` liệt kê MỌI quyền, `version` luôn có.
import type {
  MaTranHangDto,
  MaTranPhanQuyenDto,
  MaTranVaiTroDto,
} from '../../src/app/platform/quan-tri/phan-quyen/models/phan-quyen.dto';
import type { CaGia } from '../ha-tang/api-gia';
import { thanhCong } from '../ha-tang/envelope';
import { loiHeThongKhongLuong } from './chung';

const DUONG_DAN = '/core/permissions/matrix';

const QT = '0192f3c2-1111-7000-8000-000000000001';
const NV = '0192f3c2-1111-7000-8000-000000000002';
const KT = '0192f3c2-1111-7000-8000-000000000003';

const VAI_TRO: readonly MaTranVaiTroDto[] = [
  { id: QT, name: 'Quản trị hệ thống', isSystem: true },
  { id: NV, name: 'Nhân viên', isSystem: false },
  { id: KT, name: 'Kế toán trưởng', isSystem: false },
  {
    id: '0192f3c2-1111-7000-8000-000000000004',
    name: 'Chuyên viên phụ trách thẩm định hồ sơ đề nghị cấp phép hoạt động tại các cơ sở trực thuộc',
    isSystem: false,
  },
];

function hang(
  chiSo: number,
  taiNguyen: string,
  hanhDong: string,
  cap: readonly string[],
): MaTranHangDto {
  return {
    permissionId: `0192f3d0-0001-7000-8000-${String(chiSo).padStart(12, '0')}`,
    code: `${taiNguyen}.${hanhDong}`,
    resourceKey: taiNguyen,
    resourceNameKey: `resource.${taiNguyen}`,
    nameKey: `permission.${taiNguyen}.${hanhDong}`,
    grantedRoleIds: cap,
  };
}

/** Danh mục quyền của Core (schema-core.md §5.2); một quyền chưa cấp cho ai, vài quyền cấp cho nhiều vai trò. */
function hangMau(): readonly MaTranHangDto[] {
  return [
    hang(1, 'core.user', 'read', [QT, NV, KT]),
    hang(2, 'core.user', 'write', [QT]),
    hang(3, 'core.user', 'lock', [QT]),
    hang(4, 'core.user', 'reset-password', []),
    hang(5, 'core.user', 'export', [QT, KT]),
    hang(6, 'core.user.role', 'assign', [QT]),
    hang(7, 'core.role', 'read', [QT, NV]),
    hang(8, 'core.role', 'write', [QT]),
    hang(9, 'core.permission', 'read', [QT]),
    hang(10, 'core.permission', 'write', [QT]),
    hang(11, 'core.menu', 'read', [QT, NV, KT]),
  ];
}

function caMaTran(dto: MaTranPhanQuyenDto): CaGia {
  return { phuongThuc: 'GET', duongDan: DUONG_DAN, phanHoi: thanhCong(dto) };
}

/** Ma trận có dữ liệu: bốn vai trò (một tên dài), mười một quyền, một quyền chưa cấp cho ai. */
export function maTranPhanQuyenCoDuLieu(): CaGia {
  return caMaTran({ roles: VAI_TRO, rows: hangMau(), version: 'sha256:7f3a1c9e' });
}

/** Ma trận không có vai trò nào: `roles: []`; `rows` vẫn đủ danh mục quyền (permissions.md §5). */
export function maTranPhanQuyenKhongCoVaiTro(): CaGia {
  return caMaTran({
    roles: [],
    rows: hangMau().map((h) => ({ ...h, grantedRoleIds: [] })),
    version: 'sha256:00000000',
  });
}

/** Lỗi tải ma trận có chủ ý: 500 `CORE.SYSTEM.UNEXPECTED`. */
export function maTranPhanQuyenLoiTai(): CaGia {
  return {
    phuongThuc: 'GET',
    duongDan: DUONG_DAN,
    phanHoi: loiHeThongKhongLuong(),
    loiCoChuY: true,
  };
}
