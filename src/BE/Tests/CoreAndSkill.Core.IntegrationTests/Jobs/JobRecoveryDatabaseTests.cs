using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Application.Jobs;
using CoreAndSkill.Core.Domain.Jobs;
using CoreAndSkill.Core.Infrastructure.Jobs;
using CoreAndSkill.Core.IntegrationTests.Support;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Jobs;

// CHƯA CHẠY THỬ TRONG MÔI TRƯỜNG NÀY — không có Docker daemon lúc viết, nên các test dưới đây CHƯA
// TỪNG được thực thi một lần nào: chúng biên dịch được, chỉ vậy thôi. Đừng đọc sự tồn tại của chúng
// là bằng chứng chúng xanh. CI là nơi chạy chúng lần ĐẦU TIÊN.
// Gỡ khối chú thích này ngay khi có người chạy thật và thấy kết quả.
//
// Khôi phục việc dở dang lúc khởi động — docs/quy-uoc/be-cqrs-handler.md §10.3 ràng buộc 5. Hàng đợi việc nằm trong bộ
// nhớ nên tiến trình dừng giữa chừng làm mất nó; một việc `running` hay `queued`-mà-không-còn-dòng-outbox-chờ sẽ không
// bao giờ được nhặt lại. Không có phép khôi phục này thì người dùng chờ vô hạn một việc đã chết.
// Cần Docker daemon — dựng PostgreSQL thật qua Testcontainers (PostgresFixture). Máy không có
// Docker: loại nhóm này bằng `--filter "Category!=RequiresDocker"`.
// Xem docs/wiki-core/be/04-testing-strategy.md §4.2. CI không bao giờ dùng filter đó — CI luôn có Docker daemon.
[Trait("Category", "RequiresDocker")]
[Collection(PostgresCollection.Name)]
public sealed class JobRecoveryDatabaseTests(PostgresFixture db) : IAsyncLifetime
{
    private B4DockerHost _host = null!;
    private TestTenant _tenant = null!;

    public async Task InitializeAsync()
    {
        await db.ResetAsync();
        _host = new B4DockerHost(db.ConnectionString);
        _tenant = await _host.CreateTenantAsync("DV-RECOVERY");
    }

    public async Task DisposeAsync() => await _host.DisposeAsync();

    private Task<Guid> CreateJobAsync()
        => _host.AsAsync(_tenant, async sp =>
        {
            var jobs = sp.GetRequiredService<IJobRepository>();
            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
            var job = Job.Create("test.job", _tenant.AdminUserId, "_tmp/x").Value;

            await unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                await jobs.AddAsync(job, ct);
                await unitOfWork.SaveChangesAsync(ct);
                return new TransactionOutcome<bool>(true, ShouldCommit: true);
            });

            return job.Id;
        });

    private Task ExecuteAsync(string sql, Guid jobId)
        => db.CountAsync(sql + " RETURNING 1::bigint", new NpgsqlParameter("id", jobId));

    private async Task<(string Status, string? ErrorCode)> StateAsync(Guid jobId)
        => (await db.QueryAsync(
            "SELECT status, error ->> 'code' FROM core.job WHERE id = @id",
            r => (r.GetString(0), r.IsDBNull(1) ? null : r.GetString(1)),
            new NpgsqlParameter("id", jobId))).Single();

    private Task RecoverAsync()
        => ActivatorUtilities.CreateInstance<JobRecoveryHostedService>(_host.Services).RecoverAsync(CancellationToken.None);

    [Fact]
    public async Task ARunningJob_IsMarkedFailedAsInterrupted_WithAFinishedEventForTheInitiator()
    {
        var id = await CreateJobAsync();
        await ExecuteAsync("UPDATE core.job SET status = 'running', started_at = now() WHERE id = @id", id);

        await RecoverAsync();

        var state = await StateAsync(id);
        state.Status.ShouldBe("failed");
        state.ErrorCode.ShouldBe(JobErrors.Interrupted.Code);
        (await db.CountAsync(
            "SELECT count(*) FROM core.outbox_message WHERE event_type = 'core.job.finished.v1' AND payload ->> 'jobId' = @id",
            new NpgsqlParameter("id", id.ToString()))).ShouldBe(1, "người khởi tạo được báo qua chính đường thông báo thường");
    }

    [Fact]
    public async Task AQueuedJob_WithAPendingOutboxRow_IsLeftAlone_ItWillStillBeDispatched()
    {
        var id = await CreateJobAsync();

        await RecoverAsync();

        (await StateAsync(id)).Status.ShouldBe("queued");
    }

    [Fact]
    public async Task AQueuedJob_WhoseOutboxRowIsAlreadyDone_IsMarkedInterrupted_BecauseNothingWillRunItNow()
    {
        var id = await CreateJobAsync();
        await db.CountAsync(
            "UPDATE core.outbox_message SET status = 'done', processed_at = now() WHERE payload ->> 'jobId' = @id RETURNING 1::bigint",
            new NpgsqlParameter("id", id.ToString()));

        await RecoverAsync();

        var state = await StateAsync(id);
        state.Status.ShouldBe("failed");
        state.ErrorCode.ShouldBe(JobErrors.Interrupted.Code);
    }

    [Fact]
    public async Task AFinishedJob_IsNeverTouched()
    {
        var id = await CreateJobAsync();
        await ExecuteAsync("UPDATE core.job SET status = 'succeeded', progress = 100, finished_at = now() WHERE id = @id", id);

        await RecoverAsync();

        (await StateAsync(id)).Status.ShouldBe("succeeded");
    }

    [Fact]
    public async Task RunningTwice_DoesNotMarkTheSameJobTwice()
    {
        var id = await CreateJobAsync();
        await ExecuteAsync("UPDATE core.job SET status = 'running', started_at = now() WHERE id = @id", id);

        await RecoverAsync();
        await RecoverAsync();

        (await db.CountAsync(
            "SELECT count(*) FROM core.outbox_message WHERE event_type = 'core.job.finished.v1' AND payload ->> 'jobId' = @id",
            new NpgsqlParameter("id", id.ToString()))).ShouldBe(1);
    }
}
