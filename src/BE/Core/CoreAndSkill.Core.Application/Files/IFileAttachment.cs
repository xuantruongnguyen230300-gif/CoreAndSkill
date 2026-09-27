using CoreAndSkill.Core.Domain.Common;

namespace CoreAndSkill.Core.Application.Files;

// Bước 5 của luồng N2 (docs/luong/N2-dinh-kem-tep.md): handler nghiệp vụ của MODULE gọi khi lưu bản
// ghi chủ, để gắn tệp đã tải lên vào nó. Đây là cửa DUY NHẤT đổi owner_table / owner_id — module không
// chạm bảng core.file.
public interface IFileAttachment
{
    Task<Result> AttachAsync(Guid fileId, string ownerTable, Guid ownerId, CancellationToken ct);
}

// Gắn được khi tệp thuộc quyền người gọi: tệp CHƯA gắn thì phải do chính người gọi tải lên; tệp đã gắn
// đúng chủ này thì gắn lại là hợp lệ (idempotent). Không có điều kiện này, ai biết id một tệp (UUID v7
// không phải bí mật) cũng gắn được nó vào bản ghi của mình rồi đọc nó qua quyền của bản ghi đó.
internal sealed class FileAttachment(IFileRepository files, FileAccessPolicy access) : IFileAttachment
{
    public async Task<Result> AttachAsync(Guid fileId, string ownerTable, Guid ownerId, CancellationToken ct)
    {
        var file = await files.FindByIdAsync(fileId, ct);

        if (file is null || !(file.IsAttached ? await access.CanWriteAsync(file, ct) : access.IsUploader(file)))
            return Result.Failure(FileErrors.NotFound);

        return file.AttachTo(ownerTable, ownerId);
    }
}
