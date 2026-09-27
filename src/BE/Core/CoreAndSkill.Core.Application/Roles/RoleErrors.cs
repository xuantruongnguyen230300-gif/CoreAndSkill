using CoreAndSkill.Core.Domain.Common;

namespace CoreAndSkill.Core.Application.Roles;

// Catalog lỗi — docs/contracts/roles.md.
public static class RoleErrors
{
    public static readonly Error NotFound = new(
        "CORE.ROLE.NOT_FOUND", "Không tìm thấy vai trò.", ErrorType.NotFound);

    public static readonly Error NameDuplicate = new(
        "CORE.ROLE.NAME_DUPLICATE", "Tên '{Name}' đã được dùng cho một vai trò khác.", ErrorType.Conflict);

    public static readonly Error SystemImmutable = new(
        "CORE.ROLE.SYSTEM_IMMUTABLE", "Vai trò hệ thống không sửa hoặc xoá được.", ErrorType.BusinessRule);

    // messageParams mang khoá Count — docs/contracts/roles.md §4.
    public static readonly Error InUse = new(
        "CORE.ROLE.IN_USE", "Vai trò còn {Count} người dùng.", ErrorType.BusinessRule);
}
