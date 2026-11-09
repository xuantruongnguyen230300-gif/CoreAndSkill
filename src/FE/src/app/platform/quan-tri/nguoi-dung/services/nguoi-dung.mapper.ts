import type { NguoiDungDto } from '../models/nguoi-dung.dto';
import type { NguoiDung } from '../models/nguoi-dung.model';

/** Tên trường DTO theo contracts/users.md §3, §4. Mapper đổi DTO sang model: ngày thành `Date`, rút đúng thứ màn cần; được phép đổi tên trường khi model cần (fe-api-client.md §4.2). */
export function mapNguoiDung(dto: NguoiDungDto): NguoiDung {
  return {
    id: dto.id,
    userName: dto.userName,
    email: dto.email,
    fullName: dto.fullName,
    roles: dto.roles,
    isLocked: dto.isLocked,
    lockoutEnd: dto.lockoutEnd ? new Date(dto.lockoutEnd) : null,
    lockedByAdmin: dto.lockedByAdmin,
    mustChangePassword: dto.mustChangePassword,
    createdAt: dto.createdAt ? new Date(dto.createdAt) : null,
    version: dto.version,
  };
}
