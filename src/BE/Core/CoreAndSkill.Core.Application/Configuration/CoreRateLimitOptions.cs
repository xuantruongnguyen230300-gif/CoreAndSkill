using System.ComponentModel.DataAnnotations;

namespace CoreAndSkill.Core.Application.Configuration;

// Ngưỡng giới hạn tần suất — mặc định là các con số đã chốt ở docs/quy-uoc/be-api-controller.md §6.1, §6.2.
// Cửa sổ tính bằng giây. Khai ngoài miền thì tiến trình không khởi động (be-architecture.md §4).
public sealed class CoreRateLimitOptions
{
    public const string SectionName = "Core:RateLimit";

    // Hàng rào 1 — đăng nhập, theo IP.
    // Có trần: tối đa 100 lần mỗi phút (quyết định 2026-10-06); cửa sổ tối thiểu 60 giây để không nới bằng cách rút cửa sổ.
    [Range(1, 100)]
    public int LoginPermitLimit { get; init; } = 5;

    [Range(60, 3600)]
    public int LoginWindowSeconds { get; init; } = 60;

    // Hàng rào 2 — giới hạn nền cho mọi request, cửa sổ trượt.
    [Range(1, 100_000)]
    public int GlobalPermitLimit { get; init; } = 200;

    [Range(1, 3600)]
    public int GlobalWindowSeconds { get; init; } = 60;

    [Range(1, 60)]
    public int GlobalSegmentsPerWindow { get; init; } = 6;

    // Hàng rào 4 — báo cáo lỗi từ FE, theo IP.
    [Range(1, 10_000)]
    public int ClientErrorsPermitLimit { get; init; } = 20;

    [Range(1, 3600)]
    public int ClientErrorsWindowSeconds { get; init; } = 60;
}
