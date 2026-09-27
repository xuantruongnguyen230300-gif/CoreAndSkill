using System.ComponentModel.DataAnnotations;

namespace CoreAndSkill.Core.Application.Configuration;

// Giới hạn số dòng của MỘT lần xuất đồng bộ — docs/wiki-core/be/15-import-export.md §5.3. Chung cho
// mọi đơn vị. Xuất chạy nền chưa có ở v1, nên trần này là bắt buộc: nó giữ cho một lần xuất không
// treo cả tiến trình.
public sealed class CoreExportOptions
{
    public const string SectionName = "Core:Export";

    [Range(1, 1_000_000)]
    public int MaxRows { get; init; } = 50_000;
}
