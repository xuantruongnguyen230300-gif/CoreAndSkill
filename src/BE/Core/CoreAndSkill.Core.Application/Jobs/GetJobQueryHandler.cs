using System.Text.Json;
using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Domain.Common;
using MediatR;

namespace CoreAndSkill.Core.Application.Jobs;

internal sealed class GetJobQueryHandler(IJobRepository jobs, ICurrentUser currentUser)
    : IRequestHandler<GetJobQuery, Result<JobDto>>
{
    public async Task<Result<JobDto>> Handle(GetJobQuery query, CancellationToken ct)
    {
        var job = await jobs.FindForReadAsync(query.Id, ct);

        // Việc của NGƯỜI KHÁC gộp với "không có" (luật M7): gộp hai ca là cố ý, để không xác nhận một
        // định danh việc có tồn tại.
        if (job is null || currentUser.UserId is not { } callerId || job.CreatedByUserId != callerId)
            return Result.Failure<JobDto>(JobErrors.NotFound);

        return new JobDto(
            job.Id,
            job.Type,
            job.Status,
            job.Progress,
            job.CreatedAt,
            job.FinishedAt,
            ParseOrNull(job.ResultJson),
            job.ResultFileId,
            ParseOrNull(job.ErrorJson));
    }

    private static JsonElement? ParseOrNull(string? json)
    {
        if (string.IsNullOrEmpty(json))
            return null;

        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
