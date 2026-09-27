import type { DonViDto } from '../models/don-vi.dto';
import type { DonVi } from '../models/don-vi.model';

/** Tên trường theo contracts/tenants.md §1 — không đổi tên ở đây (fe-api-client.md §4.2). */
export function mapDonVi(dto: DonViDto): DonVi {
  return {
    id: dto.id,
    code: dto.code,
    name: dto.name,
    isActive: dto.isActive,
    createdAt: dto.createdAt ? new Date(dto.createdAt) : null,
  };
}
