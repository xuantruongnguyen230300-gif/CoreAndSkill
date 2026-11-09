namespace CoreAndSkill.Core.Application.Common;

// Thân data của 201 khi tạo tài khoản/vai trò: trả bản ghi thay vì chuỗi trần để hình dạng dây khớp đúng { "id": "…" } của
// card (docs/contracts/users.md §5, roles.md §2) — cùng khuôn UpdatePermissionMatrixResultDto.
public sealed record CreatedIdDto(Guid Id);
