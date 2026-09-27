using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Application.Jobs;
using CoreAndSkill.Core.Domain.Jobs;
using CoreAndSkill.Core.Domain.Outbox;
using CoreAndSkill.Core.Infrastructure.Jobs;
using CoreAndSkill.Core.Infrastructure.Persistence;
using CoreAndSkill.Core.IntegrationTests.Support;
using Microsoft.EntityFrameworkCore;
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
// ADR-0071 phương án C: câu quét việc dở dang lúc khởi động chuyển từ Database.SqlQueryRaw sang LINQ trên DbSet kèm
// IgnoreQueryFilters([Tenant]). Nhóm này trả lời đúng MỘT câu hỏi: bản LINQ có chọn ĐÚNG tập việc mà bản SQL thô cũ
// chọn không? Sai ở đây không hiện ra như một lỗi — nó hiện ra như một việc đang chạy bị đánh dấu hỏng oan, hoặc một
// việc đã chết nằm treo mãi ở `queued`.
//
// Bản SQL thô cũ đứng làm ORACLE, chạy THẲNG qua Npgsql (PostgresFixture.QueryAsync): không CoreDbContext, không bộ
// lọc toàn cục, không interceptor — phép so không đi qua chính thứ đang được kiểm. Nhưng hai bản cùng sai một kiểu thì
// phép so đó xanh, nên tập kỳ vọng còn được viết TAY ở `Cases` và khẳng định riêng.
//
// Cần Docker daemon — dựng PostgreSQL thật qua Testcontainers (PostgresFixture). Máy không có
// Docker: loại nhóm này bằng `--filter "Category!=RequiresDocker"`.
// Xem docs/wiki-core/be/04-testing-strategy.md §4.2. CI không bao giờ dùng filter đó — CI luôn có Docker daemon.
[Trait("Category", "RequiresDocker")]
[Collection(PostgresCollection.Name)]
public sealed class JobRecoveryCandidateParityDatabaseTests(PostgresFixture db) : IAsyncLifetime
{
    // NGUYÊN VĂN câu mà JobRecoveryHostedService.CandidateSql chạy trước ADR-0071 phương án C, chỉ đổi danh sách cột
    // thành `j.id`. Đây là bản đối chiếu — sửa nó cho khớp bản LINQ là bỏ đi chính thứ nhóm này kiểm.
    private const string LegacyCandidateSql = """
        SELECT j.id
        FROM core.job j
        WHERE j.is_deleted = false
          AND (j.status = 'running'
               OR (j.status = 'queued'
                   AND NOT EXISTS (SELECT 1 FROM core.outbox_message o
                                   WHERE o.status = 'pending'
                                     AND o.event_type = 'core.job.queued.v1'
                                     AND o.payload ->> 'jobId' = j.id::text)))
        """;

    private B4DockerHost _host = null!;
    private TestTenant _first = null!;
    private TestTenant _second = null!;

    public async Task InitializeAsync()
    {
        await db.ResetAsync();
        _host = new B4DockerHost(db.ConnectionString);
        _first = await _host.CreateTenantAsync("DV-PARITY-1");
        _second = await _host.CreateTenantAsync("DV-PARITY-2");
    }

    public async Task DisposeAsync() => await _host.DisposeAsync();

    private sealed record Case(string Label, Guid JobId, Guid TenantId, bool IsCandidate);

    // Dựng đủ mọi nhánh của câu quét, trên HAI đơn vị. Tạo việc qua đúng đường nghiệp vụ nên OutboxInterceptor ghi kèm
    // một dòng `core.job.queued.v1` ở trạng thái `pending` cho từng việc — đó là trạng thái xuất phát của mọi ca dưới.
    private async Task<IReadOnlyList<Case>> SeedAsync()
    {
        var cases = new List<Case>();

        async Task<Guid> Add(string label, TestTenant tenant, bool isCandidate, Func<Guid, Task>? arrange = null)
        {
            var id = await CreateJobAsync(tenant);
            if (arrange is not null)
                await arrange(id);

            cases.Add(new Case(label, id, tenant.Id, isCandidate));
            return id;
        }

        await Add("running", _first, true, id => SetJobStatusAsync(id, JobStatus.Running));
        await Add("queued-con-dong-pending", _first, false);
        await Add("queued-dong-da-done", _first, true, id => SetOutboxStatusAsync(id, OutboxStatus.Done));
        await Add("queued-dong-da-dead", _first, true, id => SetOutboxStatusAsync(id, OutboxStatus.Dead));

        // Không còn dòng nào cho việc này, TRONG KHI việc khác vẫn còn dòng `pending`: nếu phép dò không tương quan theo
        // đúng jobId thì dòng của việc khác sẽ che việc này, và ca này đỏ.
        await Add("queued-khong-con-dong-nao", _first, true, DeleteOutboxAsync);

        await Add("queued-dong-loai-su-kien-khac", _first, true,
            id => SetOutboxEventTypeAsync(id, JobFinishedEvent.TypeKey));

        await Add("succeeded", _first, false, id => SetJobStatusAsync(id, JobStatus.Succeeded));
        await Add("failed", _first, false, id => SetJobStatusAsync(id, JobStatus.Failed));

        // Bộ lọc xoá mềm PHẢI còn áp — nó thay cho `is_deleted = false` của bản SQL thô.
        await Add("running-nhung-da-xoa-mem", _first, false, async id =>
        {
            await SetJobStatusAsync(id, JobStatus.Running);
            await SoftDeleteJobAsync(id);
        });

        // Đơn vị THỨ HAI: câu quét lúc khởi động phải vượt ranh giới đơn vị, và phải vượt nó ở CẢ HAI bảng.
        await Add("running-don-vi-khac", _second, true, id => SetJobStatusAsync(id, JobStatus.Running));
        await Add("queued-con-dong-pending-don-vi-khac", _second, false);

        return cases;
    }

    [Fact]
    public async Task TheLinqScan_PicksExactlyTheCandidates_TheRawSqlItReplacedPicked()
    {
        var cases = await SeedAsync();

        var expected = cases.Where(c => c.IsCandidate).Select(c => c.JobId).Order().ToList();
        var oracle = (await db.QueryAsync(LegacyCandidateSql, reader => reader.GetGuid(0))).Order().ToList();
        var actual = await ScanAsync();
        var actualIds = actual.Select(c => c.Id).Order().ToList();

        // Chống xanh rỗng (T6): phép so phải có cả dòng được chọn lẫn dòng bị loại, nếu không nó đúng với mọi câu.
        expected.ShouldNotBeEmpty();
        expected.Count.ShouldBeLessThan(cases.Count, "phải có ca bị loại, không thì phép so đúng với cả câu `SELECT *`");

        actualIds.ShouldBe(oracle, $"bản LINQ chọn khác bản SQL thô nó thay thế.{Describe(cases, actualIds)}");
        actualIds.ShouldBe(expected, $"bản LINQ chọn khác tập kỳ vọng viết tay.{Describe(cases, actualIds)}");
        oracle.ShouldBe(expected, $"bản SQL thô (oracle) chọn khác tập kỳ vọng viết tay.{Describe(cases, oracle)}");
    }

    // Ba cột chiếu ra phải là ba cột của ĐÚNG dòng đó: bản SQL thô dựa vào bí danh cột trùng tên thuộc tính, bản LINQ
    // dựa vào thứ tự tham số của record — hai cách sai khác nhau, cùng một hậu quả (mở phạm vi ngữ cảnh SAI đơn vị).
    [Fact]
    public async Task EachCandidate_CarriesTheTenantAndTheCreatorOfItsOwnRow()
    {
        var cases = await SeedAsync();
        var elsewhere = cases.Single(c => c.Label == "running-don-vi-khac");

        var candidate = (await ScanAsync()).Single(c => c.Id == elsewhere.JobId);

        candidate.TenantId.ShouldBe(_second.Id);
        candidate.CreatedByUserId.ShouldBe(_second.AdminUserId);
    }

    // Đầu-tới-cuối trên hai đơn vị: phép khôi phục đánh dấu được việc của đơn vị mà nó KHÔNG đứng trong phạm vi lúc
    // quét — nhờ Enter() mở lại phạm vi theo từng việc.
    [Fact]
    public async Task Recovery_MarksInterruptedJobs_AcrossEveryTenant()
    {
        var cases = await SeedAsync();

        await ActivatorUtilities.CreateInstance<JobRecoveryHostedService>(_host.Services)
            .RecoverAsync(CancellationToken.None);

        foreach (var one in cases)
        {
            var status = await StatusAsync(one.JobId);
            var expected = one.IsCandidate ? JobStatus.Failed : StatusBeforeRecoveryOf(one);
            status.ShouldBe(expected, $"ca '{one.Label}'");
        }
    }

    private static string StatusBeforeRecoveryOf(Case one) => one.Label switch
    {
        "succeeded" => JobStatus.Succeeded,
        "failed" => JobStatus.Failed,
        _ => JobStatus.Queued,
    };

    // Quét ĐÚNG như lúc khởi động: một DI scope, KHÔNG phạm vi ngữ cảnh nào mở, nên ITenantContext.TenantId là null.
    private async Task<IReadOnlyList<RecoveryCandidate>> ScanAsync()
    {
        using var scope = _host.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
        return await JobRecoveryHostedService.Candidates(context).ToListAsync();
    }

    private static string Describe(IReadOnlyList<Case> cases, IReadOnlyList<Guid> chosen)
        => Environment.NewLine + string.Join(Environment.NewLine, cases.Select(c =>
            $"  {(chosen.Contains(c.JobId) ? "CHỌN " : "loại ")} {(c.IsCandidate ? "(kỳ vọng CHỌN)" : "(kỳ vọng loại)")} {c.Label}"));

    private Task<Guid> CreateJobAsync(TestTenant tenant)
        => _host.AsAsync(tenant, async sp =>
        {
            var jobs = sp.GetRequiredService<IJobRepository>();
            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
            var job = Job.Create("test.job", tenant.AdminUserId, "_tmp/x").Value;

            await unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                await jobs.AddAsync(job, ct);
                await unitOfWork.SaveChangesAsync(ct);
                return new TransactionOutcome<bool>(true, ShouldCommit: true);
            });

            return job.Id;
        });

    private Task RunAsync(string sql, params NpgsqlParameter[] parameters)
        => db.QueryAsync(sql, _ => 0, parameters);

    private Task SetJobStatusAsync(Guid jobId, string status)
        => RunAsync(
            "UPDATE core.job SET status = @status WHERE id = @id",
            new NpgsqlParameter("status", status), new NpgsqlParameter("id", jobId));

    private Task SoftDeleteJobAsync(Guid jobId)
        => RunAsync("UPDATE core.job SET is_deleted = true WHERE id = @id", new NpgsqlParameter("id", jobId));

    private Task SetOutboxStatusAsync(Guid jobId, string status)
        => RunAsync(
            "UPDATE core.outbox_message SET status = @status WHERE payload ->> 'jobId' = @id",
            new NpgsqlParameter("status", status), new NpgsqlParameter("id", jobId.ToString()));

    private Task SetOutboxEventTypeAsync(Guid jobId, string eventType)
        => RunAsync(
            "UPDATE core.outbox_message SET event_type = @type WHERE payload ->> 'jobId' = @id",
            new NpgsqlParameter("type", eventType), new NpgsqlParameter("id", jobId.ToString()));

    private Task DeleteOutboxAsync(Guid jobId)
        => RunAsync(
            "DELETE FROM core.outbox_message WHERE payload ->> 'jobId' = @id",
            new NpgsqlParameter("id", jobId.ToString()));

    private async Task<string> StatusAsync(Guid jobId)
        => (await db.QueryAsync(
            "SELECT status FROM core.job WHERE id = @id",
            reader => reader.GetString(0),
            new NpgsqlParameter("id", jobId))).Single();
}
