using CoreAndSkill.Core.Application.Common.Cqrs;

namespace CoreAndSkill.Core.Application.Import;

// POST /api/v1/<khu>/<tài nguyên>/import — docs/contracts/exports.md §2. Controller của module đưa
// vào loại nhập (IImportDefinition.Type), MỘT Stream seekable và tên tệp; trả về jobId
// (docs/contracts/jobs.md §1). Nhập LUÔN chạy nền — không có đường đồng bộ để chọn nhầm.
public sealed record StartImportCommand(string Type, Stream? File, string? FileName) : ICommand<Guid>;
