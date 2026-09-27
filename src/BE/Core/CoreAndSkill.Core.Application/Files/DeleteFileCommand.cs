using CoreAndSkill.Core.Application.Common.Cqrs;

namespace CoreAndSkill.Core.Application.Files;

// DELETE /api/v1/core/files/{id} — docs/contracts/files.md §3: gỡ liên kết. Tệp vật lý KHÔNG xoá trong
// request; job đối soát dọn sau một khoảng an toàn (14-file-storage.md §5.1, §5.2).
public sealed record DeleteFileCommand(Guid Id) : ICommand;
