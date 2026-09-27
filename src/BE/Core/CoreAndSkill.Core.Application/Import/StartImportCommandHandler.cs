using CoreAndSkill.Core.Application.Common;
using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Application.Configuration;
using CoreAndSkill.Core.Application.Files;
using CoreAndSkill.Core.Application.Jobs;
using CoreAndSkill.Core.Application.Tabular;
using CoreAndSkill.Core.Domain.Common;
using CoreAndSkill.Core.Domain.Jobs;
using MediatR;
using Microsoft.Extensions.Options;

namespace CoreAndSkill.Core.Application.Import;

// docs/wiki-core/be/15-import-export.md §6, docs/quy-uoc/be-cqrs-handler.md §10.3. Handler làm TRỌN
// use case: đếm dòng -> ghi tệp gốc vào kho tạm -> tạo việc -> trả jobId. Nó KHÔNG gọi
// IBackgroundJobScheduler: entity Job ghi nhận JobQueuedEvent, bộ chặn ở tầng dữ liệu chuyển thành dòng
// outbox trong CÙNG giao dịch, và tiến trình phát đẩy việc vào hàng đợi sau commit.
//
// THỨ TỰ: mọi phép kiểm (định dạng, cột, trần số dòng) TRƯỚC khi ghi bất cứ thứ gì — vượt trần thì
// không có bản ghi core.job nào được tạo và không có tệp tạm nào bị bỏ lại.
internal sealed class StartImportCommandHandler(
    IEnumerable<IImportDefinition> definitions,
    ITabularReader reader,
    IFileStorage storage,
    IJobRepository jobs,
    ICurrentUser currentUser,
    IOptions<CoreImportOptions> importOptions,
    IOptions<CoreFileOptions> fileOptions)
    : IRequestHandler<StartImportCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(StartImportCommand command, CancellationToken ct)
    {
        var callerId = currentUser.UserId
            ?? throw new InvalidOperationException("StartImportCommand chạy khi chưa xác thực.");

        var definition = definitions.FirstOrDefault(d => string.Equals(d.Type, command.Type, StringComparison.Ordinal));
        if (definition is null)
            return Result.Failure<Guid>(ImportErrors.UnknownType.WithParams(("ImportType", command.Type)));

        // Validator đã bảo đảm File khác null.
        var content = command.File!;
        if (!content.CanSeek)
            return InvalidFile(CommonErrors.Format);

        if (content.Length == 0)
            return InvalidFile(CommonErrors.Required);

        // Trần dung lượng ở tầng ứng dụng (tầng máy chủ web đã chặn trước ở UploadSizeLimitAttribute).
        var maxBytes = (long)fileOptions.Value.MaxUploadMb * 1024 * 1024;
        if (content.Length > maxBytes)
            return Result.Failure<Guid>(FileErrors.TooLarge.WithParams(("MaxBytes", maxBytes)));

        content.Position = 0;
        int rowCount;
        try
        {
            await using var source = await reader.OpenAsync(content, ct);

            var missing = definition.Columns
                .Where(c => c.Required && !source.Headers.Contains(c.Name, StringComparer.OrdinalIgnoreCase))
                .Select(c => c.Name)
                .ToList();
            if (missing.Count > 0)
                return Result.Failure<Guid>(
                    ImportErrors.ColumnsMismatch.WithParams(("MissingColumns", string.Join(", ", missing))));

            // Đếm HẾT tệp để thông điệp vượt trần nêu được số dòng thực tế (luồng N4 §4). Đọc theo
            // luồng nên rẻ; dung lượng đã bị chặn ở trên.
            rowCount = 0;
            await foreach (var _ in source.ReadRowsAsync(ct))
                rowCount++;
        }
        catch (TabularFormatException)
        {
            // Nội dung KHÔNG vào thông điệp: nó là dữ liệu người dùng (07-observability.md §5).
            return InvalidFile(CommonErrors.Format);
        }

        if (rowCount == 0)
            return InvalidFile(CommonErrors.Required);

        if (rowCount > importOptions.Value.MaxRows)
            return Result.Failure<Guid>(ImportErrors.TooManyRows.WithParams(
                ("RowCount", rowCount), ("MaxRows", importOptions.Value.MaxRows)));

        // Tệp gốc vào kho tạm TRƯỚC khi tạo việc và giữ tới khi việc kết thúc — không giữ trong bộ
        // nhớ giữa request và job (tệp tải lên không sống sót qua ranh giới request -> job).
        content.Position = 0;
        var tempKey = await storage.SaveTempAsync(content, ct);

        var job = Job.Create(definition.Type, callerId, tempKey);
        if (job.IsFailure)
            return Result.Failure<Guid>(job.Error!);

        await jobs.AddAsync(job.Value, ct);
        return job.Value.Id;
    }

    private static Result<Guid> InvalidFile(Error fieldError)
        => Result.Failure<Guid>(CommonErrors.ValidationFailed.WithFieldErrors(
            new Dictionary<string, IReadOnlyList<FieldError>>
            {
                ["File"] = [new FieldError(fieldError.Code, new Dictionary<string, string>())],
            }));
}
