using CoreAndSkill.Core.Application.Common.Cqrs;

namespace CoreAndSkill.Core.Application.Files;

// POST /api/v1/core/files — docs/contracts/files.md §1. Application không biết IFormFile (ASP.NET
// Core — luật A2): controller đưa vào MỘT Stream seekable và tên client gửi. Cả ba đều nullable để
// validator trả CORE.VALIDATION.REQUIRED thay vì để controller tự phán đoán "thiếu tệp".
public sealed record UploadFileCommand(Stream? File, string? FileName, string? Purpose) : ICommand<FileDto>;
