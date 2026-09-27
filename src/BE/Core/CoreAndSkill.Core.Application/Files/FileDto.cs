namespace CoreAndSkill.Core.Application.Files;

// Đúng bốn field của docs/contracts/files.md §1 — storage_key và owner_* không đi trên dây.
public sealed record FileDto(Guid Id, string OriginalName, string ContentType, long SizeBytes);

// Kết quả của GET /files/{id}: chính nội dung tệp, không phải envelope. Stream do NGƯỜI NHẬN đóng.
public sealed record FileDownload(Stream Content, string ContentType, string FileName);
