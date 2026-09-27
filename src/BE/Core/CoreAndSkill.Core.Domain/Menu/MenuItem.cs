using CoreAndSkill.Core.Domain.Common;

namespace CoreAndSkill.Core.Domain.Menu;

// Cây điều hướng động, đúng MỘT cấp cha–con — docs/database/schema-core.md §6.1.
// Ba luật ở đó không diễn đạt được bằng constraint, và HAI trong ba không nhìn thấy được từ một entity
// đơn lẻ: "cây một cấp" cần biết cha có cha hay không, "mục cha route = null" cần biết mục có con hay
// không. Cả hai vì thế kiểm trên TẬP mục — ở TenantSeedValidator.ValidateMenuItems (Infrastructure). Đường
// ghi duy nhất hiện có là TenantProvisioningService.ApplySeedAsync; phép kiểm chạy lúc khởi động
// (TenantSeedValidationHostedService) VÀ ở đầu TenantProvisioningService.CreateTenantAsync, trước dòng ghi
// đầu tiên — đường `core bootstrap` không khởi động hosted service nào. Luật thứ ba, "code bất biến", được
// ép bằng chính việc lớp này không có lối đổi Code.
//
// Menu ở v1 là DỮ LIỆU SEED — không có endpoint quản trị (docs/contracts/meta-menu.md §2), nên seed là
// đường ghi duy nhất. Nguồn của Core (CoreTenantSeedSource) khai menu của chính Core — danh sách thật
// đọc ở chính lớp đó, không chép lại ở đây.
public sealed class MenuItem : BaseEntity, ITenantScoped
{
    public Guid TenantId { get; init; }

    public string Code { get; private set; } = string.Empty;
    public string LabelKey { get; private set; } = string.Empty;
    public string? Icon { get; private set; }
    public string? Route { get; private set; }
    public Guid? ParentId { get; private set; }
    public int DisplayOrder { get; private set; }
    public Guid? RequiredPermissionId { get; private set; }
    public string? ModuleKey { get; private set; }

    private MenuItem()
    {
        // EF Core cần ctor không tham số.
    }

    // Dạng mã Create LƯU — cũng là dạng ux_menu_item_tenant_code_active so. Mọi phép so mã menu TRƯỚC khi ghi (so trùng,
    // tra ParentCode — TenantSeedValidator, TenantProvisioningService.ApplySeedAsync) đi qua hàm này, không tự chuẩn hoá
    // lại: so trên chuỗi thô thì "bao-cao" và "bao-cao " lọt qua phép kiểm rồi vỡ unique ở SaveChangesAsync.
    public static string NormalizeCode(string code) => code.Trim();

    public static Result<MenuItem> Create(
        string code,
        string labelKey,
        string? icon,
        string? route,
        Guid? parentId,
        int displayOrder,
        Guid? requiredPermissionId,
        string? moduleKey)
        => Result.Success(new MenuItem
        {
            Code = NormalizeCode(code),
            LabelKey = labelKey.Trim(),
            Icon = icon,
            Route = route,
            ParentId = parentId,
            DisplayOrder = displayOrder,
            RequiredPermissionId = requiredPermissionId,
            ModuleKey = moduleKey,
        });
}
