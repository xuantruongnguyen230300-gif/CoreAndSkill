using System.ComponentModel.DataAnnotations;

namespace CoreAndSkill.Core.Application.Configuration;

// Chu kỳ quét dọn dữ liệu quá hạn — docs/wiki-core/be/trien-khai/04-b3-van-hanh.md §2, §3. Đây là
// tham số KỸ THUẬT (bao lâu quét một lần), KHÔNG phải chính sách lưu giữ (giữ bao lâu — thứ đó là
// quyết định nghiệp vụ chưa chốt, docs/wiki-core/be/10-data-retention.md §3, §8). Không có
// IStaleDataCleaner nào đăng ký ở v1 nên giá trị này chưa có tác dụng quan sát được — vẫn khai qua
// cấu hình (không hardcode) để không phải sửa lược đồ khi có cleaner đầu tiên.
public sealed class CoreBackgroundJobOptions
{
    public const string SectionName = "Core:BackgroundJobs";

    [Range(1, 24 * 30)]
    public int StaleDataCleanupIntervalHours { get; init; } = 24;
}
