namespace CoreAndSkill.Core.Application.Files;

// Quyền của tệp là quyền của BẢN GHI CHỦ — docs/contracts/files.md §4. Tệp không có ma trận quyền
// riêng ("một hệ quyền thứ hai chỉ cho tệp là hai hệ quyền sẽ lệch"), nên chỉ bản ghi chủ mới biết
// ai đọc được nó: mỗi bản ghi chủ khai MỘT checker cho bảng của mình.
//
// Core khai checker cho bảng của chính nó (core.job — JobFileOwnerAccessChecker); module khai cho
// bảng module. Bảng chủ KHÔNG có checker nào ⇒ tệp gắn vào đó không ai đọc được: từ chối là mặc định,
// mở là phải khai (cùng khuôn deny-by-default của ma trận quyền).
public interface IFileOwnerAccessChecker
{
    // Khoá owner_table mà checker này phụ trách, dạng `schema.table`.
    string OwnerTable { get; }

    Task<bool> CanReadAsync(Guid ownerId, CancellationToken ct);

    Task<bool> CanWriteAsync(Guid ownerId, CancellationToken ct);
}
