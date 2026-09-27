namespace CoreAndSkill.Core.Application.Permissions;

// Danh mục khoá quyền do module cấp — định nghĩa gốc, docs/quy-uoc/be-architecture.md §1.1.
// Core giữ CƠ CHẾ phân quyền; tập KHOÁ là dữ liệu: mỗi module cấp khoá của mình qua seam này, Core
// cấp khoá của chính nó qua cùng seam (danh mục: docs/database/schema-core.md §5.2).
public sealed record PermissionResourceDefinition(
    string Key, // core.permission_resource.key
    string NameKey, // khoá dịch — cột name_key
    string? ModuleKey, // cột module_key; null ⇒ tài nguyên của Core
    int DisplayOrder); // cột display_order

public sealed record PermissionDefinition(
    string Code, // core.permission.code — "<ResourceKey>.<Action>", chữ thường
    string ResourceKey, // Key của một PermissionResourceDefinition trong CÙNG nguồn
    string Action, // cột action
    string NameKey, // khoá dịch — cột name_key
    int DisplayOrder); // cột display_order

public interface IPermissionCatalogSource
{
    IReadOnlyCollection<PermissionResourceDefinition> GetResources();
    IReadOnlyCollection<PermissionDefinition> GetPermissions();
}
