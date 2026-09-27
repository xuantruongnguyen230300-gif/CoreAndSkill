using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Application.Common.Logging;
using CoreAndSkill.Core.Application.Diagnostics;
using CoreAndSkill.Core.Domain.Common;
using Microsoft.Extensions.Logging;

namespace CoreAndSkill.Core.Application.Jobs;

// Chạy MỘT việc nền — điểm vào duy nhất của mọi loại việc theo yêu cầu. Được đẩy vào hàng đợi bởi bên
// nhận outbox của JobQueuedEvent (JobQueuedOutboxHandler) qua IBackgroundJobScheduler, chạy trong một
// DI scope mới với đơn vị/người kích hoạt đã mở lại (docs/quy-uoc/be-cqrs-handler.md §10.3 ràng buộc 5).
//
// Ba tính chất phải giữ:
//   1. IDEMPOTENT. Outbox bảo đảm "ít nhất một lần", nên cùng một việc có thể tới đây hai lần. Bước
//      queued -> running là câu UPDATE có điều kiện; lần tới sau thấy việc không còn `queued` thì bỏ
//      qua — không chạy đôi.
//   2. KHÔNG NUỐT LỖI. Ngoại lệ của executor: log Error, tăng core.job.failed, đánh dấu việc `failed`
//      kèm mã CORE.JOB.UNEXPECTED. Việc kết thúc theo cách nào cũng để lại dấu vết (dòng job +
//      dòng outbox JobFinished -> thông báo tới người khởi tạo).
//   3. Việc kết thúc phát MỘT sự kiện trong CÙNG giao dịch với việc ghi trạng thái cuối (Job.Complete /
//      Job.Fail ghi nhận sự kiện; OutboxInterceptor chuyển thành dòng outbox).
public sealed class JobRunner(
    IJobRepository jobs,
    IUnitOfWork unitOfWork,
    IEnumerable<IJobExecutor> executors,
    TimeProvider timeProvider,
    ILogger<JobRunner> logger)
{
    public async Task RunAsync(Guid jobId, string? input, CancellationToken ct)
    {
        if (!await jobs.TryClaimAsync(jobId, timeProvider.GetUtcNow(), ct))
        {
            logger.LogWarning("Việc {JobId} không còn ở trạng thái queued (đã chạy, hoặc không thuộc đơn vị này) — bỏ qua.", jobId);
            return;
        }

        var job = await jobs.FindForReadAsync(jobId, ct);
        if (job is null)
        {
            // Vừa nhặt được xong lại không đọc được: không thể xảy ra trừ khi dòng bị xoá giữa hai câu.
            logger.LogError("Việc {JobId} vừa được nhặt nhưng không đọc lại được.", jobId);
            return;
        }

        var jobType = job.Type;
        logger.LogInformation("Việc {JobId} loại {JobType} bắt đầu.", jobId, jobType);

        var outcome = await ExecuteAsync(jobId, jobType, input, ct);

        var recorded = await unitOfWork.ExecuteInTransactionAsync(async innerCt =>
        {
            var tracked = await jobs.FindByIdAsync(jobId, innerCt);
            if (tracked is null)
                return new TransactionOutcome<bool>(false, ShouldCommit: false);

            var now = timeProvider.GetUtcNow();
            var transition = outcome.IsSuccess
                ? tracked.Complete(now, outcome.ResultJson, outcome.ResultFileId)
                : tracked.Fail(now, JobJson.ErrorOf(outcome.Error!));

            return new TransactionOutcome<bool>(transition.IsSuccess, ShouldCommit: transition.IsSuccess);
        }, ct);

        if (!recorded)
        {
            // Không im lặng: việc đã chạy xong nhưng trạng thái cuối không ghi được (dòng biến mất,
            // hoặc không còn `running`). Người dùng sẽ thấy việc treo ở `running` — dòng log này là dấu vết.
            logger.LogError(
                "Việc {JobId} loại {JobType} đã chạy xong nhưng KHÔNG ghi được trạng thái cuối — cần người can thiệp.",
                jobId, jobType);
            CoreMetrics.BackgroundJobFailed.Add(1);
            return;
        }

        logger.LogInformation(
            "Việc {JobId} loại {JobType} kết thúc: {Status}.", jobId, jobType, outcome.IsSuccess ? "succeeded" : "failed");
    }

    private async Task<JobOutcome> ExecuteAsync(Guid jobId, string jobType, string? input, CancellationToken ct)
    {
        var executor = executors.FirstOrDefault(e => e.Handles(jobType));
        if (executor is null)
        {
            logger.LogError("Việc {JobId} loại {JobType}: không có bộ chạy nào — cần người can thiệp.", jobId, jobType);
            CoreMetrics.BackgroundJobFailed.Add(1);
            return JobOutcome.Failure(JobErrors.NoExecutor.WithParams(("JobType", jobType)));
        }

        var context = new JobExecutionContext(
            jobId, jobType, input, progress => jobs.UpdateProgressAsync(jobId, progress, ct));

        try
        {
            return await executor.ExecuteAsync(context, ct);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            // Lọc theo TOKEN, không theo kiểu: "đang tắt" chỉ là token của chính lượt chạy đã huỷ — khi đó ngoại lệ đi
            // tiếp, việc giữ `running` cho JobRecoveryHostedService. Một OperationCanceledException khi token CHƯA huỷ
            // (HttpClient.Timeout ném TaskCanceledException bọc TimeoutException) là lỗi của executor như mọi lỗi khác.
            //
            // Chỉ kiểu ngoại lệ + thông điệp ĐÃ LỌC — thông điệp của thư viện ngoài có thể mang giá trị
            // đầu vào (07-observability.md §5). Không kèm ngăn xếp: ngoại lệ nguyên bản không được
            // chuyển thẳng vào log có lưu trữ.
            logger.LogError(
                "Việc {JobId} loại {JobType} lỗi không lường trước: {Failure} — cần người can thiệp.",
                jobId, jobType, FailureText.Describe(ex));
            CoreMetrics.BackgroundJobFailed.Add(1);
            return JobOutcome.Failure(JobErrors.Unexpected);
        }
    }
}
