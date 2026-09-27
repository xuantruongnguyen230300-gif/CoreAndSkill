import type { MaTranPhanQuyenDto } from '../models/phan-quyen.dto';
import type { MaTranPhanQuyen } from '../models/phan-quyen.model';

/** Tên trường theo contracts/permissions.md §5 — không đổi tên ở đây (fe-api-client.md §4.2). */
export function mapMaTranPhanQuyen(dto: MaTranPhanQuyenDto): MaTranPhanQuyen {
  return {
    roles: dto.roles,
    rows: dto.rows,
    version: dto.version,
  };
}
