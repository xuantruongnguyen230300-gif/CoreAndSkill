using System.ComponentModel.DataAnnotations;

namespace CoreAndSkill.Core.Application.Configuration;

// Lưu trữ tệp — docs/wiki-core/be/14-file-storage.md §2, §6, §7. Khoá `Core:File:*`.
//
// RootPath KHÔNG có giá trị mặc định và KHÔNG nằm ở appsettings.json: mỗi môi trường một chỗ, và
// thiếu hay không dùng được thì tiến trình KHÔNG khởi động (luật A8, §4 của be-architecture.md) — đó
// là lúc rẻ nhất để phát hiện, chứ không phải ở lần tải tệp đầu tiên. Phép kiểm sâu hơn (thư mục có
// thật, ghi được, không nằm trong thư mục ứng dụng) ở CoreFileOptionsValidator, Core.Infrastructure.
public sealed class CoreFileOptions
{
    public const string SectionName = "Core:File";

    [Required(AllowEmptyStrings = false)]
    public string RootPath { get; init; } = default!;

    // Trần dung lượng MỘT tệp, dùng ở cả tầng máy chủ web (UploadSizeLimitAttribute) lẫn tầng ứng
    // dụng (handler). Mỗi `purpose` có thể siết chặt hơn, không nới hơn.
    [Range(1, 1024)]
    public int MaxUploadMb { get; init; } = 20;

    // Tệp tạm (tệp gốc của việc nhập, chờ việc chạy) có HẠN DÙNG — §6.
    [Range(1, 24 * 30)]
    public int TempRetentionHours { get; init; } = 24;

    // Khoảng an toàn trước khi job đối soát xoá một tệp "không có bản ghi": nếu không, job sẽ xoá đúng
    // tệp vừa được tải lên trong lúc giao dịch chưa kịp commit — §5.2.
    [Range(1, 24 * 30)]
    public int OrphanGraceHours { get; init; } = 24;

    // Tệp tải lên mà không bao giờ được gắn vào bản ghi chủ (bỏ form giữa chừng) bị gỡ sau hạn này — §3.1,
    // §5.2. Phải dài hơn một lần mở form lâu nhất: quá hạn thì lần gắn tới sau đó không còn tìm thấy tệp.
    [Range(1, 24 * 30)]
    public int UnattachedRetentionHours { get; init; } = 24;

    // Tệp kết quả của việc nền (ví dụ danh sách dòng nhập lỗi) là tệp tạm có hạn dùng.
    [Range(1, 365)]
    public int ResultFileRetentionDays { get; init; } = 7;

    // Chu kỳ job bảo trì tệp (dọn tệp tạm, đối soát hai chiều).
    [Range(1, 24 * 7)]
    public int MaintenanceIntervalHours { get; init; } = 6;
}
