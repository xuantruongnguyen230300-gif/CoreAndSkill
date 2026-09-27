using CoreAndSkill.Core.Domain.Common;

namespace CoreAndSkill.Core.Domain.Notifications;

// Nội dung một thông báo trong ứng dụng — docs/database/schema-core.md §7.1.
//
// LƯU KHOÁ + THAM SỐ, KHÔNG LƯU CÂU ĐÃ GHÉP (docs/wiki-core/be/12-notifications.md §5): câu chữ ghép
// lúc hiển thị, theo ngôn ngữ của người XEM. Lưu câu đã ghép thì đổi ngôn ngữ không đổi được thông
// báo cũ, và sửa lỗi chính tả không sửa được cái đã gửi. Thuộc tính không có chỗ nào chứa câu chữ —
// đó là ý đồ.
public sealed class Notification : BaseEntity, ITenantScoped
{
    public const string DefaultSeverity = "info";

    public Guid TenantId { get; init; }

    // Khoá dịch, ví dụ "core.job.succeeded".
    public string Code { get; private set; } = string.Empty;

    // jsonb — tham số theo TÊN (luật R7), ví dụ {"UserName":"an.nv"}.
    public string? Params { get; private set; }

    // CHƯA DÙNG ở v1 (schema-core.md §7.1, contracts/notifications.md §1): severity luôn 'info',
    // link_route luôn rỗng cho tới khi có card khai chúng.
    public string Severity { get; private set; } = DefaultSeverity;
    public string? LinkRoute { get; private set; }

    public string? ModuleKey { get; private set; }

    private Notification()
    {
        // EF Core cần ctor không tham số.
    }

    public static Result<Notification> Create(string code, string? paramsJson, string? moduleKey)
        => Result.Success(new Notification
        {
            Code = code.Trim(),
            Params = paramsJson,
            ModuleKey = moduleKey,
        });
}
