using CoreAndSkill.Core.Domain.Common;

namespace CoreAndSkill.Core.Application.Users;

// Luật chung cho roleIds của cả hai lệnh mang danh sách vai trò — §5 tạo người dùng, §7 gán vai trò (docs/contracts/users.md).
// Một chỗ cho cả hai: hai lệnh lệch nhau ở đây là lệch hợp đồng.
internal static class UserRoleAssignmentRules
{
    // Trần số phần tử của roleIds — docs/contracts/users.md "Ghi chú — trần 50 phần tử của roleIds": hàng rào KỸ THUẬT (kích
    // thước payload, khối lượng của phép kiểm leo thang đặc quyền theo tập), không phải luật nghiệp vụ.
    public const int MaxRoleIds = 50;

    // roleIds trùng là 400 CORE.USER.DUPLICATE_ROLE_ENTRY, không phải "tự lọc trùng" — docs/contracts/users.md §7. Kiểm ở
    // HANDLER, trước khi chạm dữ liệu, không ở validator: ValidationBehavior bọc mọi lỗi validator thành mã gốc
    // CORE.VALIDATION.FAILED, còn hợp đồng đòi mã gốc DUPLICATE_ROLE_ENTRY.
    public static Result EnsureNoDuplicateRoleIds(IReadOnlyCollection<Guid> roleIds)
        => roleIds.Distinct().Count() != roleIds.Count
            ? Result.Failure(UserErrors.DuplicateRoleEntry)
            : Result.Success();
}
