using System.Text;
using CoreAndSkill.Core.Application.Files;
using CoreAndSkill.Core.Domain.Files;
using CoreAndSkill.Core.Infrastructure.Files;
using CoreAndSkill.Core.IntegrationTests.Support;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Files;

// CHƯA CHẠY THỬ TRONG MÔI TRƯỜNG NÀY — không có Docker daemon lúc viết, nên các test dưới đây CHƯA
// TỪNG được thực thi một lần nào: chúng biên dịch được, chỉ vậy thôi. Đừng đọc sự tồn tại của chúng
// là bằng chứng chúng xanh. CI là nơi chạy chúng lần ĐẦU TIÊN.
// Gỡ khối chú thích này ngay khi có người chạy thật và thấy kết quả.
//
// Tệp trên PostgreSQL THẬT — docs/wiki-core/be/14-file-storage.md §3-§5. B4FilesEndpointTests (không DB) đã chứng minh
// hợp đồng HTTP; ở đây là những thứ chỉ DB thật mới chứng minh được: bộ lọc đơn vị và xoá mềm trong EfFileRepository,
// cột created_by do interceptor đóng dấu (cơ sở của quy tắc "chưa gắn thì chỉ người tải lên đọc"), và job đối soát
// đĩa <-> DB với thời gian tua bằng MutableClock.
// Cần Docker daemon — dựng PostgreSQL thật qua Testcontainers (PostgresFixture). Máy không có
// Docker: loại nhóm này bằng `--filter "Category!=RequiresDocker"`.
// Xem docs/wiki-core/be/04-testing-strategy.md §4.2. CI không bao giờ dùng filter đó — CI luôn có Docker daemon.
[Trait("Category", "RequiresDocker")]
[Collection(PostgresCollection.Name)]
public sealed class FilesDatabaseTests(PostgresFixture db) : IAsyncLifetime
{
    private static readonly byte[] PdfBytes = Encoding.ASCII.GetBytes("%PDF-1.4\n1 0 obj\n<<>>\nendobj\ntrailer\n<<>>\n%%EOF\n");

    private B4DockerHost _host = null!;
    private TestTenant _tenantA = null!;
    private TestTenant _tenantB = null!;

    public async Task InitializeAsync()
    {
        await db.ResetAsync();
        _host = new B4DockerHost(db.ConnectionString, configure: services => services.AddSingleton<IFilePurposeSource>(new TestPurposes()));
        _tenantA = await _host.CreateTenantAsync("DV-FILE-A");
        _tenantB = await _host.CreateTenantAsync("DV-FILE-B");
    }

    public async Task DisposeAsync() => await _host.DisposeAsync();

    private sealed class TestPurposes : IFilePurposeSource
    {
        public IReadOnlyCollection<FilePurposeDefinition> GetPurposes() => [new("ho-so", [FileContentDetector.Pdf])];
    }

    private async Task<Guid> UploadAsync(TestTenant tenant)
    {
        var result = await _host.AsAsync(tenant, sp => sp.GetRequiredService<ISender>()
            .Send(new UploadFileCommand(new MemoryStream(PdfBytes), "quyet-dinh.pdf", "ho-so")));
        result.IsSuccess.ShouldBeTrue(result.Error?.Code);
        return result.Value.Id;
    }

    private Task<string> StorageKeyAsync(Guid id)
        => db.QueryAsync("SELECT storage_key FROM core.file WHERE id = @id", r => r.GetString(0), new NpgsqlParameter("id", id))
            .ContinueWith(t => t.Result.Single());

    // Gắn thẳng bằng SQL: tệp chưa gắn quá Core:File:UnattachedRetentionHours bị gỡ, nên test nào tua đồng hồ qua hạn đó
    // mà muốn giữ bản ghi "sống" thì phải gắn nó trước — giống tệp thật đã vào một bản ghi chủ.
    private Task AttachAsync(Guid id)
        => db.CountAsync(
            "UPDATE core.file SET owner_table = 'test.tai_lieu', owner_id = @owner WHERE id = @id RETURNING 1::bigint",
            new NpgsqlParameter("id", id), new NpgsqlParameter("owner", Guid.NewGuid()));

    private string PhysicalPath(string storageKey)
        => Path.Combine(_host.Factory.FileRootPath, storageKey.Replace('/', Path.DirectorySeparatorChar));

    // ---- Bộ lọc đơn vị và xoá mềm ---------------------------------------------------------------

    [Fact]
    public async Task ARecordIsInvisibleToAnotherTenant_AsIfItDidNotExist()
    {
        var id = await UploadAsync(_tenantA);

        var seenByA = await _host.AsAsync(_tenantA, sp => sp.GetRequiredService<IFileRepository>().FindByIdAsync(id, CancellationToken.None));
        var seenByB = await _host.AsAsync(_tenantB, sp => sp.GetRequiredService<IFileRepository>().FindByIdAsync(id, CancellationToken.None));

        seenByA.ShouldNotBeNull();
        seenByB.ShouldBeNull("bộ lọc đơn vị: tệp của đơn vị khác là 'không có', không phải 'không có quyền'");
    }

    [Fact]
    public async Task ASoftDeletedRecord_IsNotFound()
    {
        var id = await UploadAsync(_tenantA);
        await _host.AsAsync(_tenantA, sp => sp.GetRequiredService<ISender>().Send(new DeleteFileCommand(id)));

        var found = await _host.AsAsync(_tenantA, sp => sp.GetRequiredService<IFileRepository>().FindByIdAsync(id, CancellationToken.None));

        found.ShouldBeNull();
        (await db.CountAsync("SELECT count(*) FROM core.file WHERE id = @id AND is_deleted", new NpgsqlParameter("id", id))).ShouldBe(1, "xoá mềm: bản ghi còn, đánh dấu is_deleted");
    }

    // ---- Tải lên / tải xuống ----------------------------------------------------------------------

    [Fact]
    public async Task Upload_StoresTheRecordWithTheUploaderAsCreatedBy_AndTheBytesUnderTheRoot()
    {
        var id = await UploadAsync(_tenantA);

        var row = (await db.QueryAsync(
            "SELECT created_by, original_name, content_type, size_bytes, purpose, owner_table FROM core.file WHERE id = @id",
            r => (r.GetString(0), r.GetString(1), r.GetString(2), r.GetInt64(3), r.GetString(4), r.IsDBNull(5)),
            new NpgsqlParameter("id", id))).Single();

        row.Item1.ShouldBe(_tenantA.AdminUserName);
        row.Item2.ShouldBe("quyet-dinh.pdf");
        row.Item3.ShouldBe("application/pdf");
        row.Item4.ShouldBe(PdfBytes.Length);
        row.Item5.ShouldBe("ho-so");
        row.Item6.ShouldBeTrue("chưa gắn bản ghi chủ nào");
        File.Exists(PhysicalPath(await StorageKeyAsync(id))).ShouldBeTrue();
    }

    [Fact]
    public async Task AnUnattachedFile_IsReadableByItsUploaderOnly()
    {
        var id = await UploadAsync(_tenantA);
        var stranger = _tenantA with { AdminUserId = Guid.NewGuid(), AdminUserName = "nguoi.la" };

        var byUploader = await _host.AsAsync(_tenantA, sp => sp.GetRequiredService<ISender>().Send(new GetFileQuery(id)));
        var byStranger = await _host.AsAsync(stranger, sp => sp.GetRequiredService<ISender>().Send(new GetFileQuery(id)));

        byUploader.IsSuccess.ShouldBeTrue();
        byStranger.IsFailure.ShouldBeTrue();
        byStranger.Error!.Code.ShouldBe(FileErrors.NotFound.Code);
    }

    // ---- Đối soát (§5.2) ----------------------------------------------------------------------

    private Task RunMaintenanceAsync()
        => ActivatorUtilities.CreateInstance<FileMaintenanceHostedService>(_host.Services).RunOnceAsync(CancellationToken.None);

    [Fact]
    public async Task AnOrphanOnDisk_IsKept_WithinTheGracePeriod_AndDeleted_AfterIt()
    {
        string orphanKey;
        await using (var content = new MemoryStream(PdfBytes))
            orphanKey = await _host.Services.GetRequiredService<IFileStorage>().SaveAsync(content, "ho-so", ".pdf", CancellationToken.None);

        await RunMaintenanceAsync();
        File.Exists(PhysicalPath(orphanKey)).ShouldBeTrue("mới tải lên, giao dịch có thể chưa commit — chưa được xoá");

        _host.Clock.Advance(TimeSpan.FromHours(48));
        await RunMaintenanceAsync();
        File.Exists(PhysicalPath(orphanKey)).ShouldBeFalse();
    }

    [Fact]
    public async Task TheFileOfASoftDeletedRecord_IsRemovedFromDisk_AfterTheGrace_ButTheRecordIsKeptAsHistory()
    {
        var id = await UploadAsync(_tenantA);
        var key = await StorageKeyAsync(id);
        await _host.AsAsync(_tenantA, sp => sp.GetRequiredService<ISender>().Send(new DeleteFileCommand(id)));

        _host.Clock.Advance(TimeSpan.FromHours(48));
        await RunMaintenanceAsync();

        File.Exists(PhysicalPath(key)).ShouldBeFalse();
        (await db.CountAsync("SELECT count(*) FROM core.file WHERE id = @id", new NpgsqlParameter("id", id))).ShouldBe(1);
    }

    [Fact]
    public async Task ALiveRecordWithMissingContent_IsNeverAutoDeleted_OnlyReported()
    {
        var id = await UploadAsync(_tenantA);
        await AttachAsync(id);
        File.Delete(PhysicalPath(await StorageKeyAsync(id)));

        _host.Clock.Advance(TimeSpan.FromHours(48));
        await RunMaintenanceAsync();

        (await db.CountAsync("SELECT count(*) FROM core.file WHERE id = @id AND NOT is_deleted", new NpgsqlParameter("id", id)))
            .ShouldBe(1, "chiều 'có bản ghi, không có tệp' chỉ được BÁO CÁO — tự xoá bản ghi là xoá bằng chứng của sự cố");
    }

    [Fact]
    public async Task ExpiredJobResultFiles_AreUnlinked_ByTheMaintenanceRun()
    {
        var id = await UploadAsync(_tenantA);
        // Tệp kết quả thật luôn gắn vào việc nền — gắn ở đây để test chứng minh đúng quy tắc hạn giữ tệp kết quả,
        // không phải quy tắc tệp chưa gắn.
        await AttachAsync(id);
        await db.CountAsync("UPDATE core.file SET purpose = 'job-result' WHERE id = @id RETURNING 1::bigint", new NpgsqlParameter("id", id));

        _host.Clock.Advance(TimeSpan.FromDays(8));
        await RunMaintenanceAsync();

        (await db.CountAsync("SELECT count(*) FROM core.file WHERE id = @id AND is_deleted", new NpgsqlParameter("id", id))).ShouldBe(1);
    }

    [Fact]
    public async Task AnUnattachedFile_IsUnlinked_AfterTheRetention_ThenItsBytesFollowTheRemovedRecordPath()
    {
        var abandoned = await UploadAsync(_tenantA);
        var attached = await UploadAsync(_tenantA);
        await AttachAsync(attached);
        var key = await StorageKeyAsync(abandoned);

        await RunMaintenanceAsync();
        (await db.CountAsync("SELECT count(*) FROM core.file WHERE id = @id AND NOT is_deleted", new NpgsqlParameter("id", abandoned)))
            .ShouldBe(1, "trong hạn: form có thể còn đang mở");

        _host.Clock.Advance(TimeSpan.FromHours(25));
        await RunMaintenanceAsync();

        (await db.CountAsync("SELECT count(*) FROM core.file WHERE id = @id AND is_deleted", new NpgsqlParameter("id", abandoned))).ShouldBe(1);
        (await db.CountAsync("SELECT count(*) FROM core.file WHERE id = @id AND NOT is_deleted", new NpgsqlParameter("id", attached))).ShouldBe(1);
        File.Exists(PhysicalPath(key)).ShouldBeTrue("vừa gỡ: tệp vật lý chờ qua khoảng an toàn như mọi bản ghi đã gỡ");

        _host.Clock.Advance(TimeSpan.FromHours(25));
        await RunMaintenanceAsync();

        File.Exists(PhysicalPath(key)).ShouldBeFalse();
    }

    [Fact]
    public async Task ATempFileOlderThanTheRetention_IsPurged_ByTheMaintenanceRun()
    {
        string tempKey;
        await using (var content = new MemoryStream(PdfBytes))
            tempKey = await _host.Services.GetRequiredService<IFileStorage>().SaveTempAsync(content, CancellationToken.None);

        _host.Clock.Advance(TimeSpan.FromHours(48));
        await RunMaintenanceAsync();

        (await _host.Services.GetRequiredService<IFileStorage>().OpenTempAsync(tempKey, CancellationToken.None)).IsFailure.ShouldBeTrue();
    }
}
