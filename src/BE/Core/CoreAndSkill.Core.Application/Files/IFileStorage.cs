using CoreAndSkill.Core.Domain.Common;

namespace CoreAndSkill.Core.Application.Files;

public sealed record StoredObject(string Key, DateTimeOffset LastWriteUtc);

// Seam lưu trữ tệp — docs/wiki-core/be/14-file-storage.md §1. Application không biết tệp nằm ở đĩa,
// ổ mạng hay dịch vụ đối tượng: đổi nơi lưu là đổi MỘT hiện thực (§8).
//
// Chỉ những thao tác thật sự dùng. Lỗi hạ tầng không dự kiến được (đĩa đầy, mất quyền ghi) là
// exception và đi tới IExceptionHandler (500); chỉ ca dự kiến được — không có tệp — là Result.
//
// Khoá do HỆ THỐNG sinh (`<purpose>/<năm>/<tháng>/<guid>.<đuôi>`), không bao giờ dựa vào tên client
// gửi, không phải đường dẫn tuyệt đối và không trả ra ngoài API.
public interface IFileStorage
{
    // extension do chỗ gọi suy từ KIỂU ĐÃ XÁC ĐỊNH (không từ tên client), gồm dấu chấm, có thể rỗng.
    Task<string> SaveAsync(Stream content, string purpose, string extension, CancellationToken ct);

    Task<Result<Stream>> OpenAsync(string key, CancellationToken ct);

    // Idempotent: xoá một khoá không có là thành công.
    Task DeleteAsync(string key, CancellationToken ct);

    Task<bool> ExistsAsync(string key, CancellationToken ct);

    // Mọi tệp lâu dài (không gồm tệp tạm) — cho job đối soát (§5.2).
    IAsyncEnumerable<StoredObject> ListAsync(CancellationToken ct);

    // ---- Kho TẠM: tách khỏi tệp lâu dài để dọn được mà không sợ chạm nhầm (§6). ----

    Task<string> SaveTempAsync(Stream content, CancellationToken ct);

    Task<Result<Stream>> OpenTempAsync(string tempKey, CancellationToken ct);

    Task DeleteTempAsync(string tempKey, CancellationToken ct);

    // Trả số tệp tạm đã xoá.
    Task<int> PurgeTempAsync(DateTimeOffset olderThan, CancellationToken ct);
}
