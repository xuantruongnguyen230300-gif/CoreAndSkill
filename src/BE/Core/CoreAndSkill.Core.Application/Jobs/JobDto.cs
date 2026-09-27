using System.Text.Json;

namespace CoreAndSkill.Core.Application.Jobs;

// Hình dạng trả của GET /api/v1/core/jobs/{id} — docs/contracts/jobs.md §1.
public sealed record JobDto(
    Guid Id,
    string Kind,
    string Status,
    int Progress,
    DateTimeOffset? CreatedAt,
    DateTimeOffset? FinishedAt,
    JsonElement? Result,
    Guid? ResultFileId,
    JsonElement? Error);
