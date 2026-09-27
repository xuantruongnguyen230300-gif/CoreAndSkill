import type { VaiTroDto } from '../models/vai-tro.dto';
import type { VaiTro } from '../models/vai-tro.model';

/** Tên trường theo contracts/roles.md §1, §5 — không đổi tên ở đây (fe-api-client.md §4.2). */
export function mapVaiTro(dto: VaiTroDto): VaiTro {
  return {
    id: dto.id,
    name: dto.name,
    isSystem: dto.isSystem,
    userCount: dto.userCount,
    createdAt: dto.createdAt ? new Date(dto.createdAt) : null,
    version: dto.version,
  };
}
