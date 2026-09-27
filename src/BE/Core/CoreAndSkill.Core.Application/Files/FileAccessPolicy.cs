using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Domain.Files;

namespace CoreAndSkill.Core.Application.Files;

// Ai được đọc / gỡ một tệp — MỘT chỗ, dùng cho cả tải về, gỡ liên kết và gắn vào bản ghi chủ.
// docs/wiki-core/be/14-file-storage.md §3.1, §4; docs/luong/N2-dinh-kem-tep.md §6.
//
//   - Tệp ĐÃ gắn bản ghi chủ: quyền của bản ghi chủ, hỏi checker của bảng đó. Không có checker ⇒ từ chối.
//   - Tệp CHƯA gắn (giữa bước tải lên và bước lưu bản ghi chủ): chưa có bản ghi nào để hỏi quyền, nên
//     chỉ NGƯỜI TẢI LÊN đọc và gỡ được. Nếu không, mọi người cùng đơn vị đoán được id là tải được tệp
//     người khác vừa đưa lên.
//
// Người gọi PHẢI trả `CORE.FILE.NOT_FOUND` (404) khi bị từ chối — không phải 403: 403 xác nhận tệp
// có tồn tại (luật M7).
public sealed class FileAccessPolicy(ICurrentUser currentUser, IEnumerable<IFileOwnerAccessChecker> checkers)
{
    public Task<bool> CanReadAsync(StoredFile file, CancellationToken ct)
        => CheckAsync(file, (checker, ownerId) => checker.CanReadAsync(ownerId, ct));

    public Task<bool> CanWriteAsync(StoredFile file, CancellationToken ct)
        => CheckAsync(file, (checker, ownerId) => checker.CanWriteAsync(ownerId, ct));

    private async Task<bool> CheckAsync(StoredFile file, Func<IFileOwnerAccessChecker, Guid, Task<bool>> ask)
    {
        if (file.OwnerTable is null || file.OwnerId is null)
            return IsUploader(file);

        var checker = checkers.FirstOrDefault(c =>
            string.Equals(c.OwnerTable, file.OwnerTable, StringComparison.Ordinal));

        return checker is not null && await ask(checker, file.OwnerId.Value);
    }

    // created_by giữ TÊN ĐĂNG NHẬP (schema-core.md §3.2), duy nhất trong một đơn vị; người dùng ngoài
    // đơn vị không bao giờ tới được tệp này vì bộ lọc đơn vị đã loại nó từ truy vấn.
    public bool IsUploader(StoredFile file)
        => currentUser.UserName is { Length: > 0 } name
           && string.Equals(file.CreatedBy, name, StringComparison.Ordinal);
}
