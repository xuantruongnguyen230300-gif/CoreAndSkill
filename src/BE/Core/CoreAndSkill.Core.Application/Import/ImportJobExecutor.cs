using System.Text.Json;
using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Application.Configuration;
using CoreAndSkill.Core.Application.Files;
using CoreAndSkill.Core.Application.Jobs;
using CoreAndSkill.Core.Application.Tabular;
using CoreAndSkill.Core.Domain.Common;
using CoreAndSkill.Core.Domain.Files;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoreAndSkill.Core.Application.Import;

// Bộ chạy MỌI loại việc nhập — docs/wiki-core/be/15-import-export.md §3, §4, §6. Việc phụ trách mọi Type
// mà một IImportDefinition đã khai.
//
// CHÍNH SÁCH LỖI: "ghi phần đúng" — dòng đúng được ghi, dòng lỗi báo lại (§4.1). Nhập một phần vẫn là
// `succeeded`; việc chỉ `failed` khi tệp không đọc được. Chính sách này được nói cho người dùng qua
// contracts/exports.md §2 — cái không hợp lệ là không chọn hoặc không nói.
//
// MỖI DÒNG là một đơn vị độc lập:
//   khoá tự nhiên trùng? -> bỏ qua (DUPLICATE_ROW)
//   định nghĩa xử lý dòng -> sai? báo lỗi + HUỶ THEO DÕI
//   lưu dòng -> DB từ chối? báo lỗi + HUỶ THEO DÕI (IImportRowWriter tự làm)
//   HUỶ THEO DÕI sau MỌI dòng (đúng cũng vậy): bộ theo dõi giữ mọi entity đã đi qua nó, nên nó phình
//   theo số dòng đã xử lý (§6.1).
// Ghi từng dòng là "lô" cỡ 1: đổi lấy việc một dòng hỏng không thể kéo dòng khác — và bẫy §4.2 không
// còn chỗ ẩn. Với tệp tối đa Core:Import:MaxRows dòng, việc chạy nền chấp nhận được độ trễ đó.
//
// Ngoại lệ KHÔNG lường trước (mất kết nối DB, lỗi trong định nghĩa) KHÔNG bị nuốt ở từng dòng: nó làm
// việc `failed` (JobRunner log + chỉ số + thông báo). Các dòng đã ghi trước đó GIỮ NGUYÊN, và nhập lại
// cùng tệp không nhân đôi nhờ khoá tự nhiên.
internal sealed class ImportJobExecutor(
    IEnumerable<IImportDefinition> definitions,
    ITabularReader reader,
    ITabularWriterFactory writerFactory,
    IFileStorage storage,
    IFileRepository files,
    IImportRowWriter rowWriter,
    IUnitOfWork unitOfWork,
    IOptions<CoreImportOptions> importOptions,
    ILogger<ImportJobExecutor> logger)
    : IJobExecutor
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private const int ProgressEveryRows = 50;

    public bool Handles(string jobType)
        => definitions.Any(d => string.Equals(d.Type, jobType, StringComparison.Ordinal));

    public async Task<JobOutcome> ExecuteAsync(JobExecutionContext context, CancellationToken ct)
    {
        var definition = definitions.First(d => string.Equals(d.Type, context.JobType, StringComparison.Ordinal));

        if (context.Input is null)
            return JobOutcome.Failure(ImportErrors.SourceUnreadable);

        var opened = await storage.OpenTempAsync(context.Input, ct);
        if (opened.IsFailure)
            return JobOutcome.Failure(ImportErrors.SourceUnreadable);

        try
        {
            await using var stream = opened.Value;
            return await RunAsync(definition, context, stream, ct);
        }
        finally
        {
            // Tệp gốc giữ tới khi việc KẾT THÚC. Xoá hỏng không làm việc hỏng: job bảo trì dọn theo
            // hạn dùng — nhưng để lại dấu vết, vì tệp chứa dữ liệu người dùng nằm lại là điều đáng biết.
            try
            {
                await storage.DeleteTempAsync(context.Input, CancellationToken.None);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                logger.LogWarning("Việc {JobId}: không xoá được tệp tạm — job bảo trì sẽ dọn theo hạn dùng.", context.JobId);
            }
        }
    }

    private async Task<JobOutcome> RunAsync(
        IImportDefinition definition, JobExecutionContext context, Stream stream, CancellationToken ct)
    {
        var total = await CountRowsAsync(stream, ct);
        if (total is null)
            return JobOutcome.Failure(ImportErrors.SourceUnreadable);

        stream.Position = 0;
        ITabularSource source;
        try
        {
            source = await reader.OpenAsync(stream, ct);
        }
        catch (TabularFormatException)
        {
            return JobOutcome.Failure(ImportErrors.SourceUnreadable);
        }

        var failures = new List<FailedRow>();
        var succeeded = 0;
        var processed = 0;
        var seenKeys = new HashSet<string>(StringComparer.Ordinal);

        await using (source)
        {
            try
            {
                await foreach (var row in source.ReadRowsAsync(ct))
                {
                    processed++;

                    var failure = await ProcessRowAsync(definition, row, seenKeys, ct);
                    if (failure is null)
                        succeeded++;
                    else
                        failures.Add(failure);

                    // Sau MỌI dòng — kể cả dòng đúng (§6.1). Không có dòng này thì entity dòng lỗi rò
                    // sang dòng sau (§4.2) và bộ theo dõi phình theo số dòng.
                    rowWriter.DiscardTrackedChanges();

                    if (processed % ProgressEveryRows == 0)
                        await context.ReportProgressAsync(Percent(processed, total.Value));
                }
            }
            catch (TabularFormatException)
            {
                // Tệp hỏng GIỮA CHỪNG (đếm được lúc đầu nhưng đọc lại hỏng): các dòng đã ghi giữ nguyên,
                // việc `failed` để người dùng biết tệp không đọc trọn được.
                logger.LogWarning("Việc {JobId}: tệp không đọc trọn được sau {Rows} dòng.", context.JobId, processed);
                return JobOutcome.Failure(ImportErrors.SourceUnreadable);
            }
        }

        return await BuildOutcomeAsync(context, total.Value, succeeded, failures, ct);
    }

    // Trả null khi tệp không đọc được — việc `failed`.
    private async Task<int?> CountRowsAsync(Stream stream, CancellationToken ct)
    {
        try
        {
            await using var source = await reader.OpenAsync(stream, ct);
            var count = 0;
            await foreach (var _ in source.ReadRowsAsync(ct))
                count++;

            return count;
        }
        catch (TabularFormatException)
        {
            return null;
        }
    }

    // null = dòng thành công đã được lưu.
    private async Task<FailedRow?> ProcessRowAsync(
        IImportDefinition definition, TabularRow row, HashSet<string> seenKeys, CancellationToken ct)
    {
        var key = definition.NaturalKey(row);
        if (seenKeys.Contains(key) || await definition.NaturalKeyExistsAsync(key, ct))
            return Fail(row, ImportErrors.DuplicateRow, field: null);

        var result = await definition.ImportRowAsync(row, ct);
        if (!result.IsSuccess)
            return Fail(row, result.Error!, result.Field);

        var saved = await rowWriter.SaveRowAsync(ct);
        if (saved.IsFailure)
            return Fail(row, saved.Error!, field: null);

        seenKeys.Add(key);
        return null;
    }

    private static FailedRow Fail(TabularRow row, Error error, string? field)
    {
        var value = field is not null && row.Values.TryGetValue(field, out var raw) ? raw : null;
        return new FailedRow(
            row.Number,
            error.Code,
            field,
            MessageTemplateRenderer.Render(error.MessageTemplate, error.Params),
            value);
    }

    private async Task<JobOutcome> BuildOutcomeAsync(
        JobExecutionContext context, int total, int succeeded, List<FailedRow> failures, CancellationToken ct)
    {
        Guid? resultFileId = null;
        if (failures.Count > 0)
            resultFileId = await SaveFailureFileAsync(context.JobId, failures, ct);

        var embedded = failures
            .Take(importOptions.Value.MaxFailedRowsInResult)
            .Select(f => new { row = f.Row, code = f.Code, field = f.Field, message = f.Message })
            .ToList();

        var resultJson = JsonSerializer.Serialize(new
        {
            totalRows = total,
            succeeded,
            failedCount = failures.Count,
            failed = embedded,
        }, JsonOptions);

        return JobOutcome.Success(resultJson, resultFileId);
    }

    // Tệp kết quả: danh sách ĐẦY ĐỦ dòng lỗi, tải về được (§4.3 "với tệp lớn, danh sách lỗi trên màn
    // hình là không đủ"). Gắn vào việc (owner core.job) nên quyền tải là quyền của người khởi tạo việc.
    private async Task<Guid> SaveFailureFileAsync(Guid jobId, List<FailedRow> failures, CancellationToken ct)
    {
        await using var buffer = new MemoryStream();
        await using (var writer = await writerFactory.CreateAsync(
            TabularFormat.Csv, buffer, ["Row", "Code", "Field", "Message", "Value"], ct))
        {
            foreach (var failure in failures)
            {
                await writer.WriteRowAsync(
                    [failure.Row.ToString(System.Globalization.CultureInfo.InvariantCulture), failure.Code, failure.Field, failure.Message, failure.Value],
                    ct);
            }

            await writer.CompleteAsync(ct);
        }

        buffer.Position = 0;
        var key = await storage.SaveAsync(buffer, CoreFilePurposes.JobResult, ".csv", ct);

        var file = StoredFile.Create(
            "import-result.csv", FileContentDetector.Csv, buffer.Length, key, CoreFilePurposes.JobResult).Value;
        file.AttachTo(CoreFilePurposes.JobOwnerTable, jobId);

        await files.AddAsync(file, ct);
        // Lưu NGAY: khoá ngoại job.result_file_id đòi bản ghi tệp có trước khi việc ghi trạng thái cuối.
        await unitOfWork.SaveChangesAsync(ct);
        rowWriter.DiscardTrackedChanges();

        return file.Id;
    }

    private static int Percent(int processed, int total)
        => total <= 0 ? 0 : Math.Min(99, (int)(processed * 100L / total));

    private sealed record FailedRow(int Row, string Code, string? Field, string Message, string? Value);
}
