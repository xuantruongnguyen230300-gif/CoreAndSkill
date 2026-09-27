using CoreAndSkill.Core.Domain.Common;

namespace CoreAndSkill.Core.Domain.Files;

// Siêu dữ liệu một tệp — docs/database/schema-core.md §9.7, docs/wiki-core/be/14-file-storage.md §3.
// Nội dung nằm ở kho lưu (IFileStorage); bản ghi này là nguồn sự thật THỨ HAI, và hai nguồn sẽ lệch
// nhau — §5 của file cơ chế nói về việc đó.
//
// Quyền của tệp là quyền của BẢN GHI CHỦ (docs/contracts/files.md §4): OwnerTable/OwnerId rỗng tới khi
// bản ghi chủ được lưu (docs/luong/N2-dinh-kem-tep.md bước 5), sau đó trỏ tới nó bằng id trần, không FK.
// Xoá là xoá mềm: gỡ liên kết, còn tệp vật lý do job đối soát dọn sau một khoảng an toàn (§5.2).
public sealed class StoredFile : BaseEntity, ITenantScoped
{
    public Guid TenantId { get; init; }

    public string? OwnerTable { get; private set; }
    public Guid? OwnerId { get; private set; }

    // Tên người dùng đặt: chỉ để hiển thị và đặt tên lúc tải về, không bao giờ dùng để dựng đường dẫn.
    public string OriginalName { get; private set; } = string.Empty;

    // Do HỆ THỐNG xác định từ nội dung, không lấy từ client.
    public string ContentType { get; private set; } = string.Empty;

    public long SizeBytes { get; private set; }

    // Khoá trong kho lưu, hệ thống sinh — không phải đường dẫn tuyệt đối, không bao giờ trả ra ngoài.
    public string StorageKey { get; private set; } = string.Empty;

    public string? Purpose { get; private set; }

    private StoredFile()
    {
        // EF Core cần ctor không tham số.
    }

    public static Result<StoredFile> Create(
        string originalName, string contentType, long sizeBytes, string storageKey, string purpose)
        => Result.Success(new StoredFile
        {
            OriginalName = originalName,
            ContentType = contentType,
            SizeBytes = sizeBytes,
            StorageKey = storageKey,
            Purpose = purpose,
        });

    public bool IsAttached => OwnerTable is not null;

    // Gắn tệp vào bản ghi chủ. Gắn lại đúng chủ cũ là hợp lệ (chạy lại một lệnh không nhân đôi);
    // gắn sang chủ KHÁC là từ chối — một tệp không đổi chủ, nếu không quyền đọc của nó đổi theo mà
    // không ai hay.
    public Result AttachTo(string ownerTable, Guid ownerId)
    {
        if (IsAttached && (OwnerTable != ownerTable || OwnerId != ownerId))
            return Result.Failure(FileDomainErrors.AlreadyAttached);

        OwnerTable = ownerTable;
        OwnerId = ownerId;
        return Result.Success();
    }

    // Gỡ liên kết — xoá mềm. Tệp vật lý KHÔNG bị xoá ở đây (14-file-storage.md §5.1: xoá bản ghi trước,
    // tệp sau, nghiêng về phía để lại rác).
    public void Remove() => IsDeleted = true;
}
