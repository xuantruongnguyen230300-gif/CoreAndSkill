using System.Text;
using System.Text.Json;
using CoreAndSkill.Core.Application.Common;
using CoreAndSkill.Core.Application.Import;
using CoreAndSkill.Core.Application.Jobs;
using CoreAndSkill.Core.Application.Tabular;
using CoreAndSkill.Core.Domain.Common;
using CoreAndSkill.Core.Infrastructure.Identity;
using CoreAndSkill.Core.Infrastructure.Persistence;
using CoreAndSkill.Core.IntegrationTests.Support;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Import;

// CHƯA CHẠY THỬ TRONG MÔI TRƯỜNG NÀY — không có Docker daemon lúc viết, nên các test dưới đây CHƯA
// TỪNG được thực thi một lần nào: chúng biên dịch được, chỉ vậy thôi. Đừng đọc sự tồn tại của chúng
// là bằng chứng chúng xanh. CI là nơi chạy chúng lần ĐẦU TIÊN.
// Gỡ khối chú thích này ngay khi có người chạy thật và thấy kết quả.
//
// Nhập dữ liệu từ đầu đến cuối trên PostgreSQL THẬT — docs/wiki-core/be/15-import-export.md §4, §6. Đây là test mà
// EfImportRowWriterTests và ImportJobExecutorTests (không DB) chỉ chứng minh được PHẦN CƠ CHẾ của: dòng lỗi nằm GIỮA
// các dòng đúng thật sự KHÔNG có trong bảng. Không có test này thì việc huỷ theo dõi sau mỗi dòng có thể bị gỡ trong
// một lần tái cấu trúc mà không ai biết (§4.2 "Test bắt buộc").
//
// Core không có luồng nhập nào của riêng nó cho người dùng cuối, nên test tự khai một IImportDefinition ghi vào
// core.app_role (có chỉ mục duy nhất theo (tenant, tên chuẩn hoá) — nguồn của lỗi trùng khoá thật từ DB).
// Đường chạy: StartImportCommand (đếm dòng, tạo việc + dòng outbox) -> JobRunner.RunAsync (bộ phát nền bị gỡ khỏi
// host, test tự gọi) -> ImportJobExecutor.
// Cần Docker daemon — dựng PostgreSQL thật qua Testcontainers (PostgresFixture). Máy không có
// Docker: loại nhóm này bằng `--filter "Category!=RequiresDocker"`.
// Xem docs/wiki-core/be/04-testing-strategy.md §4.2. CI không bao giờ dùng filter đó — CI luôn có Docker daemon.
[Trait("Category", "RequiresDocker")]
[Collection(PostgresCollection.Name)]
public sealed class ImportJobDatabaseTests(PostgresFixture db) : IAsyncLifetime
{
    private const string ImportType = "test.role.import";

    private readonly ImportBehavior _behavior = new();
    private B4DockerHost _host = null!;
    private TestTenant _tenant = null!;

    public async Task InitializeAsync()
    {
        await db.ResetAsync();
        await CreateHostAsync(null);
    }

    public async Task DisposeAsync() => await _host.DisposeAsync();

    private async Task CreateHostAsync(IReadOnlyDictionary<string, string?>? config)
    {
        if (_host is not null)
            await _host.DisposeAsync();

        _host = new B4DockerHost(db.ConnectionString, config, services =>
        {
            services.AddSingleton(_behavior);
            services.AddScoped<IImportDefinition, RoleImportDefinition>();
        });
        _tenant = await _host.CreateTenantAsync("DV-IMPORT");
    }

    private static MemoryStream Csv(params string[] lines)
        => new(Encoding.UTF8.GetBytes(string.Join("\n", lines) + "\n"));

    private Task<Result<Guid>> StartAsync(Stream file)
        => _host.AsAsync(_tenant, sp => sp.GetRequiredService<ISender>().Send(new StartImportCommand(ImportType, file, "vai-tro.csv")));

    private async Task<string> TempKeyOfAsync(Guid jobId)
    {
        var keys = await db.QueryAsync(
            "SELECT payload ->> 'input' FROM core.outbox_message WHERE event_type = 'core.job.queued.v1' AND payload ->> 'jobId' = @id",
            r => r.GetString(0),
            new NpgsqlParameter("id", jobId.ToString()));
        return keys.Single();
    }

    private async Task<Guid> RunImportAsync(params string[] lines)
    {
        var started = await StartAsync(Csv(lines));
        started.IsSuccess.ShouldBeTrue(started.Error?.Code);

        var jobId = started.Value;
        var input = await TempKeyOfAsync(jobId);
        await _host.AsAsync(_tenant, sp => sp.GetRequiredService<JobRunner>().RunAsync(jobId, input, CancellationToken.None));
        return jobId;
    }

    private async Task<IReadOnlyList<string>> RoleNamesAsync()
        => await db.QueryAsync(
            "SELECT name FROM core.app_role WHERE name LIKE 'imp-%' ORDER BY name",
            r => r.GetString(0));

    private async Task<(string Status, JsonDocument? Result, Guid? ResultFileId)> JobAsync(Guid jobId)
    {
        var rows = await db.QueryAsync(
            "SELECT status, result::text, result_file_id FROM core.job WHERE id = @id",
            r => (r.GetString(0), r.IsDBNull(1) ? null : JsonDocument.Parse(r.GetString(1)), r.IsDBNull(2) ? (Guid?)null : r.GetGuid(2)),
            new NpgsqlParameter("id", jobId));
        return rows.Single();
    }

    // ---- Bẫy §4.2 ----------------------------------------------------------------------------

    [Fact]
    public async Task MiddleErrorRow_IsNotInTheDatabase_AndTheRowsAroundItAre()
    {
        // Dòng 3 của tệp (tên "imp-bad") thêm entity RỒI mới bị coi là sai — đúng tình huống bẫy §4.2.
        var jobId = await RunImportAsync("Name", "imp-alpha", "imp-bad", "imp-beta");

        (await RoleNamesAsync()).ShouldBe(["imp-alpha", "imp-beta"]);

        var job = await JobAsync(jobId);
        job.Status.ShouldBe("succeeded", "nhập một phần vẫn là succeeded; dòng hỏng nằm ở result.failed");
        job.Result!.RootElement.GetProperty("totalRows").GetInt32().ShouldBe(3);
        job.Result.RootElement.GetProperty("succeeded").GetInt32().ShouldBe(2);
        var failed = job.Result.RootElement.GetProperty("failed").EnumerateArray().ToList();
        failed.Count.ShouldBe(1);
        failed[0].GetProperty("row").GetInt32().ShouldBe(3, "số dòng của TỆP GỐC, kể cả dòng tiêu đề");
    }

    [Fact]
    public async Task AFailedRowDoesNotDragTheNextRow_AcrossManyFailuresAndSuccesses()
    {
        var jobId = await RunImportAsync(
            "Name", "imp-1", "imp-bad", "imp-2", "imp-bad", "imp-bad", "imp-3", "imp-bad", "imp-4");

        (await RoleNamesAsync()).ShouldBe(["imp-1", "imp-2", "imp-3", "imp-4"]);
        var failedRows = (await JobAsync(jobId)).Result!.RootElement.GetProperty("failed").EnumerateArray()
            .Select(f => f.GetProperty("row").GetInt32()).ToList();
        failedRows.ShouldBe([3, 5, 6, 8]);
    }

    // ---- Chống trùng (§4.4) ------------------------------------------------------------------

    [Fact]
    public async Task ADuplicateWithinTheSameFile_IsSkipped_NotOverwritten_AndReportedWithTheDuplicateCode()
    {
        var jobId = await RunImportAsync("Name", "imp-alpha", "imp-beta", "IMP-ALPHA");

        (await RoleNamesAsync()).ShouldBe(["imp-alpha", "imp-beta"]);
        var failed = (await JobAsync(jobId)).Result!.RootElement.GetProperty("failed").EnumerateArray().Single();
        failed.GetProperty("row").GetInt32().ShouldBe(4);
        failed.GetProperty("code").GetString().ShouldBe(ImportErrors.DuplicateRow.Code);
    }

    [Fact]
    public async Task ReimportingTheSameFile_DoesNotDuplicateData()
    {
        await RunImportAsync("Name", "imp-alpha", "imp-beta");
        var again = await RunImportAsync("Name", "imp-alpha", "imp-beta");

        (await RoleNamesAsync()).ShouldBe(["imp-alpha", "imp-beta"]);
        var result = (await JobAsync(again)).Result!.RootElement;
        result.GetProperty("succeeded").GetInt32().ShouldBe(0);
        result.GetProperty("failedCount").GetInt32().ShouldBe(2);
    }

    [Fact]
    public async Task ADuplicateTheDefinitionCouldNotSee_IsCaughtByTheDatabaseUniqueIndex_AsARowError_NotAJobFailure()
    {
        // Định nghĩa "quên" kiểm tồn tại (đua nhau giữa hai lần nhập): chỉ có chỉ mục duy nhất của DB chặn được.
        await RunImportAsync("Name", "imp-alpha");
        _behavior.SkipExistsCheck = true;

        var jobId = await RunImportAsync("Name", "imp-alpha", "imp-beta");

        (await RoleNamesAsync()).ShouldBe(["imp-alpha", "imp-beta"]);
        var job = await JobAsync(jobId);
        job.Status.ShouldBe("succeeded");
        var failed = job.Result!.RootElement.GetProperty("failed").EnumerateArray().Single();
        failed.GetProperty("row").GetInt32().ShouldBe(2);
        failed.GetProperty("code").GetString().ShouldBe(ImportErrors.DuplicateRow.Code);
    }

    // ---- Tệp kết quả -------------------------------------------------------------------------

    [Fact]
    public async Task TheFailedRows_AreAlsoAFileOwnedByTheJob_ForDownload()
    {
        var jobId = await RunImportAsync("Name", "imp-alpha", "imp-bad");

        var job = await JobAsync(jobId);
        job.ResultFileId.ShouldNotBeNull();
        var owner = await db.QueryAsync(
            "SELECT owner_table, owner_id, purpose FROM core.file WHERE id = @id",
            r => (r.GetString(0), r.GetGuid(1), r.GetString(2)),
            new NpgsqlParameter("id", job.ResultFileId!.Value));
        owner.Single().ShouldBe(("core.job", jobId, "job-result"));
    }

    [Fact]
    public async Task ARunWithNoFailures_HasNoResultFile()
    {
        var jobId = await RunImportAsync("Name", "imp-alpha", "imp-beta");

        (await JobAsync(jobId)).ResultFileId.ShouldBeNull();
    }

    // ---- Trần số dòng (§6): 422 và KHÔNG tạo việc ---------------------------------------------

    [Fact]
    public async Task MoreRowsThanTheCap_IsRefusedWith422Semantics_AndNoJobNoOutboxRowNoTempFile()
    {
        await CreateHostAsync(new Dictionary<string, string?> { ["Core:Import:MaxRows"] = "3" });

        var result = await StartAsync(Csv("Name", "imp-1", "imp-2", "imp-3", "imp-4"));

        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe(ImportErrors.TooManyRows.Code);
        result.Error.Type.ShouldBe(ErrorType.BusinessRule);
        (await db.CountAsync("SELECT count(*) FROM core.job")).ShouldBe(0, "đếm TRƯỚC khi tạo việc");
        (await db.CountAsync("SELECT count(*) FROM core.outbox_message WHERE event_type = 'core.job.queued.v1'")).ShouldBe(0);
        Directory.EnumerateFiles(_host.Factory.FileRootPath, "*", SearchOption.AllDirectories).ShouldBeEmpty("không có tệp tạm nào được ghi");
    }

    [Fact]
    public async Task ExactlyTheCap_IsAccepted()
    {
        await CreateHostAsync(new Dictionary<string, string?> { ["Core:Import:MaxRows"] = "3" });

        var result = await StartAsync(Csv("Name", "imp-1", "imp-2", "imp-3"));

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task AFileMissingARequiredColumn_IsRefused_BeforeAnyJobIsCreated()
    {
        var result = await StartAsync(Csv("Ten", "imp-1"));

        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe(ImportErrors.ColumnsMismatch.Code);
        (await db.CountAsync("SELECT count(*) FROM core.job")).ShouldBe(0);
    }

    [Fact]
    public async Task TheTempSourceFile_IsGoneOnceTheJobFinishes()
    {
        await RunImportAsync("Name", "imp-alpha");

        Directory.EnumerateFiles(Path.Combine(_host.Factory.FileRootPath, "_tmp"), "*", SearchOption.AllDirectories).ShouldBeEmpty();
    }

    // ---- Định nghĩa thử -----------------------------------------------------------------------

    private sealed class ImportBehavior
    {
        public bool SkipExistsCheck { get; set; }
    }

    private sealed class RoleImportDefinition(CoreDbContext context, ImportBehavior behavior) : IImportDefinition
    {
        public string Type => ImportType;

        public IReadOnlyList<ImportColumn> Columns { get; } = [new ImportColumn("Name", Required: true, Example: "imp-vi-du")];

        public string NaturalKey(TabularRow row)
            => row.Values.TryGetValue("Name", out var name) && !string.IsNullOrWhiteSpace(name)
                ? name.Trim().ToUpperInvariant()
                : $"row:{row.Number}";

        public Task<bool> NaturalKeyExistsAsync(string naturalKey, CancellationToken ct)
            => behavior.SkipExistsCheck
                ? Task.FromResult(false)
                : context.Set<AppRole>().AnyAsync(r => r.NormalizedName == naturalKey, ct);

        public Task<ImportRowResult> ImportRowAsync(TabularRow row, CancellationToken ct)
        {
            var name = row.Values.TryGetValue("Name", out var raw) ? raw?.Trim() : null;
            if (string.IsNullOrEmpty(name))
                return Task.FromResult(ImportRowResult.Failed(CommonErrors.Required, "Name"));

            context.Set<AppRole>().Add(new AppRole { Name = name, NormalizedName = name.ToUpperInvariant() });

            // Sai nghiệp vụ được phát hiện SAU khi đã thêm entity — chính là tình huống bẫy §4.2. Định nghĩa
            // KHÔNG tự dọn: bộ chạy phải huỷ theo dõi.
            if (name.Equals("imp-bad", StringComparison.OrdinalIgnoreCase))
                return Task.FromResult(ImportRowResult.Failed(CommonErrors.Format, "Name"));

            return Task.FromResult(ImportRowResult.Ok);
        }
    }
}
