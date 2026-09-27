namespace CoreAndSkill.Core.Application.Permissions;

// Tra ma trận quyền trong DB — docs/quy-uoc/be-api-controller.md §4.2, §4.3.
// Cờ has_permission_bypass là nhánh DUY NHẤT bỏ qua ma trận (docs/adr/0021-hai-co-dac-quyen-la-hai-cot-loai-tru.md):
// tập quyền hiệu lực của tài khoản mang cờ là toàn bộ danh mục hiện có. Ở B1 danh mục quyền
// (core.permission) chưa được B2 seed nội dung — GetEffectivePermissionsAsync trả tập theo đúng dữ
// liệu đang có trong DB, kể cả khi đó là tập rỗng.
public interface IPermissionChecker
{
    Task<bool> HasPermissionAsync(Guid userId, string permissionKey, CancellationToken ct);

    Task<IReadOnlySet<string>> GetEffectivePermissionsAsync(Guid userId, CancellationToken ct);

    // Tập quyền mà TỪNG vai trò trong danh sách cấp, bằng MỘT truy vấn theo tập — không đi qua cờ has_permission_bypass
    // (cờ đó là thuộc tính của TÀI KHOẢN, không của vai trò). Mọi id được hỏi đều có mặt trong kết quả; vai trò không tồn
    // tại hoặc không cấp khoá nào nhận tập rỗng. Dùng cho luật chống leo thang đặc quyền — docs/contracts/users.md §2 Luật 1.
    Task<IReadOnlyDictionary<Guid, IReadOnlySet<string>>> GetPermissionsForRolesAsync(
        IReadOnlyCollection<Guid> roleIds, CancellationToken ct);
}
