using CoreAndSkill.Core.Domain.Common;

namespace CoreAndSkill.Core.Application.Permissions;

// Catalog lỗi — docs/contracts/permissions.md §6.
public static class PermissionErrors
{
    // Payload thiếu bất kỳ quyền nào của danh mục — chặn TRƯỚC khi ghi (§6 "Ghi chú"). Card khai
    // fieldErrors["Entries"]: gắn sẵn vào chính Error để mọi chỗ trả mã này đều mang nó.
    public static readonly Error EntriesIncomplete = OnEntries(new Error(
        "CORE.PERMISSION.ENTRIES_INCOMPLETE", "Danh sách quyền gửi lên thiếu so với danh mục.", ErrorType.Validation));

    // Dùng qua DuplicateEntryOf(...) — card khai fieldErrors["Entries"] và messageParams nêu đích danh id bị lặp.
    public static readonly Error DuplicateEntry = new(
        "CORE.PERMISSION.DUPLICATE_ENTRY", "Mã quyền '{PermissionId}' xuất hiện nhiều hơn một lần.", ErrorType.Validation);

    public static readonly Error NotFound = new(
        "CORE.PERMISSION.NOT_FOUND", "Mã quyền '{PermissionId}' không tồn tại.", ErrorType.BusinessRule);

    public static readonly Error RoleNotFound = new(
        "CORE.PERMISSION.ROLE_NOT_FOUND", "Vai trò '{RoleId}' không tồn tại.", ErrorType.BusinessRule);

    public static readonly Error SystemRoleCannotLoseWrite = new(
        "CORE.PERMISSION.SYSTEM_ROLE_CANNOT_LOSE_WRITE",
        "Không được thu hồi quyền phân quyền khỏi một vai trò hệ thống.", ErrorType.BusinessRule);

    public static readonly Error VersionMismatch = new(
        "CORE.PERMISSION.VERSION_MISMATCH", "Ma trận đã bị người khác thay đổi. Tải lại rồi thử lại.", ErrorType.Conflict);

    // Khoá "Entries" KHÔNG chỉ số — lỗi thuộc quan hệ giữa các phần tử, không quy được cho một dòng (card §6 "Ghi chú").
    public static Error DuplicateEntryOf(Guid permissionId)
        => OnEntries(DuplicateEntry.WithParams(("PermissionId", permissionId)));

    private static Error OnEntries(Error error)
        => error.WithFieldErrors(new Dictionary<string, IReadOnlyList<FieldError>>(StringComparer.Ordinal)
        {
            ["Entries"] = [new FieldError(error.Code, error.Params)],
        });
}
