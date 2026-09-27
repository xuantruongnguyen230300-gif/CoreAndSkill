import { HoSoDto } from '../models/ho-so.dto';
import { HoSo } from '../models/ho-so.model';

export function mapHoSo(dto: HoSoDto): HoSo {
  return {
    userName: dto.userName,
    email: dto.email,
    hoTen: dto.fullName,
    soDienThoai: dto.phoneNumber,
    ngonNguUaThich: dto.preferredLanguage,
    coDacQuyen: dto.hasPermissionBypass,
    version: dto.version,
  };
}
