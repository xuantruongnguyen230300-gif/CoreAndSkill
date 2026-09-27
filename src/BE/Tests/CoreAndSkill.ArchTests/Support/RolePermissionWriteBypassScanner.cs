namespace CoreAndSkill.ArchTests.Support;

// Luật S17 (docs/RULES.md §6): không ghi core.role_permission bằng đường bỏ qua ChangeTracker — AuditLogInterceptor không
// thấy các đường đó nên lần ghi không để lại dòng nhật ký nào (docs/contracts/permissions.md §6 mục "Nhật ký kiểm toán",
// luật 3). Phép dò và phần ngoài tầm của nó ở ChangeTrackerBypassScanner — dùng chung với S21; tệp này chỉ khai đích.
internal static class RolePermissionWriteBypassScanner
{
    public static IReadOnlyList<string> Scan(string source, string filePath)
        => ChangeTrackerBypassScanner.Scan(source, filePath, ChangeTrackerBypassTarget.RolePermission);
}
