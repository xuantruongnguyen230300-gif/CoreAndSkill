namespace CoreAndSkill.Core.Application.Files;

// Purpose do CHÍNH Core dùng cho tệp hệ thống sinh ra — không nằm trong FilePurposeCatalog nên client
// không tải lên được với purpose này.
public static class CoreFilePurposes
{
    // Tệp kết quả của việc nền (danh sách dòng nhập lỗi). Có hạn dùng — Core:File:ResultFileRetentionDays.
    public const string JobResult = "job-result";

    // Bảng tra tệp gắn với việc nền: owner_table của bản ghi tệp kết quả.
    public const string JobOwnerTable = "core.job";
}
