using CoreAndSkill.Core.Application.Common.Cqrs;

namespace CoreAndSkill.Core.Application.Files;

// GET /api/v1/core/files/{id} — docs/contracts/files.md §2. KHÔNG phục vụ tĩnh: mọi lần tải đi qua
// đây để kiểm quyền TRƯỚC khi mở nội dung.
public sealed record GetFileQuery(Guid Id) : IQuery<FileDownload>;
