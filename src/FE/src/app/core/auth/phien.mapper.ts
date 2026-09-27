import { NguoiDungHienTai } from './nguoi-dung-hien-tai.model';
import { PhienDto } from './phien.dto';

/** DTO → model. Hàm thuần, không inject gì (fe-api-client.md §4.2). */
export function sangNguoiDungHienTai(dto: PhienDto): NguoiDungHienTai {
  return {
    id: dto.id,
    userName: dto.userName,
    email: dto.email,
    fullName: dto.fullName,
    roles: dto.roles,
    mustChangePassword: dto.mustChangePassword,
    isSystemOperator: dto.isSystemOperator,
    sessionMinutes: dto.sessionMinutes,
    preferredLanguage: dto.preferredLanguage,
    tenantCode: dto.tenantCode,
    tenantName: dto.tenantName,
    quyen: dto.permissions,
  };
}
