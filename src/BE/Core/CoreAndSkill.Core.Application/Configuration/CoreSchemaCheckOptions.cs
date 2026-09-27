using System.ComponentModel.DataAnnotations;

namespace CoreAndSkill.Core.Application.Configuration;

// Phép kiểm migration lúc khởi động (luật E8) — docs/database/script-runbook.md §5.1. Khoá `Core:SchemaCheck:*`.
//
// Phép kiểm cần kết nối được database. Không kết nối được thì thử lại trong một khoảng có hạn, hết hạn thì tiến trình từ chối
// khởi động — "không kiểm được" không bao giờ được thành "coi như khớp" (§5.3).
public sealed class CoreSchemaCheckOptions
{
    public const string SectionName = "Core:SchemaCheck";

    // Tổng thời gian chờ database nhận kết nối, tính từ lần thử đầu. Hết khoảng này mà vẫn chưa kết nối được thì từ chối khởi
    // động. 0 = thử đúng một lần, không chờ.
    [Range(0, 600)]
    public int ConnectWaitSeconds { get; init; } = 60;

    // Khoảng nghỉ giữa hai lần thử. Lần nghỉ cuối được cắt cho vừa ConnectWaitSeconds.
    [Range(1, 60)]
    public int ConnectRetryIntervalSeconds { get; init; } = 5;
}
