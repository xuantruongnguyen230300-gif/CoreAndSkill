using System.ComponentModel.DataAnnotations;

namespace CoreAndSkill.Core.Application.Configuration;

// Tiến trình phát Outbox — docs/wiki-core/be/12-notifications.md §2.3-§2.5. Khoá `Core:Outbox:*`.
public sealed class CoreOutboxOptions
{
    public const string SectionName = "Core:Outbox";

    // Nhịp quét: vài giây một lần là đủ ở quy mô này.
    [Range(1, 300)]
    public int PollSeconds { get; init; } = 5;

    // Kích thước lô: vừa phải; lô lớn giữ giao dịch lâu.
    [Range(1, 500)]
    public int BatchSize { get; init; } = 20;

    // Sau chừng này lần thử hỏng, dòng chuyển sang `dead` (§2.5).
    [Range(1, 20)]
    public int MaxAttempts { get; init; } = 5;

    // Readiness trả Degraded khi dòng `pending` cũ nhất quá chừng này phút (§2.5).
    [Range(1, 24 * 60)]
    public int DegradedPendingAgeMinutes { get; init; } = 15;

    // Dòng đã phát được xoá sau chừng này ngày — không dọn thì bảng phình và tiến trình chậm dần (§2.3).
    [Range(1, 365)]
    public int DoneRetentionDays { get; init; } = 7;
}
