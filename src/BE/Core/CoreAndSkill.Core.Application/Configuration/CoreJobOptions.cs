using System.ComponentModel.DataAnnotations;

namespace CoreAndSkill.Core.Application.Configuration;

// Việc chạy nền theo yêu cầu — docs/wiki-core/be/15-import-export.md §6. Khoá `Core:Jobs:*`; nhóm
// `Core:BackgroundJobs:*` (CoreBackgroundJobOptions) là chu kỳ của job ĐỊNH KỲ, khác nhóm này.
public sealed class CoreJobOptions
{
    public const string SectionName = "Core:Jobs";

    // Số việc chạy đồng thời; việc thứ (N+1) trở đi đứng ở `queued` cho tới khi có chỗ.
    [Range(1, 32)]
    public int MaxConcurrent { get; init; } = 2;
}
