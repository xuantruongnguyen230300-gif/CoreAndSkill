using System.Text;
using System.Text.Json;
using CoreAndSkill.Core.Application.Audit;
using CoreAndSkill.Core.Application.Export;
using CoreAndSkill.Core.Application.Tabular;
using CoreAndSkill.Core.Application.Users;
using CoreAndSkill.Core.IntegrationTests.Support;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Users;

// Xuất danh sách người dùng trên PostgreSQL THẬT — docs/contracts/exports.md §1, docs/wiki-core/be/15-import-export.md §5.
// Điều duy nhất không có DB thì không chứng minh được: tệp xuất KHỚP với thứ màn danh sách đang hiển thị, vì cả hai đi
// qua CÙNG MỘT chỗ dựng truy vấn (UserQueryService.FilterAndOrder). B4ExportEndpointTests chạy trên repository trong bộ
// nhớ không lọc gì nên không nói được điều đó.
// Cần Docker daemon — dựng PostgreSQL thật qua Testcontainers (PostgresFixture). Máy không có
// Docker: loại nhóm này bằng `--filter "Category!=RequiresDocker"`.
// Xem docs/wiki-core/be/04-testing-strategy.md §4.2. CI không bao giờ dùng filter đó — CI luôn có Docker daemon.
[Trait("Category", "RequiresDocker")]
[Collection(PostgresCollection.Name)]
public sealed class ExportUsersDatabaseTests(PostgresFixture db) : IAsyncLifetime
{
    private B4DockerHost _host = null!;
    private TestTenant _tenant = null!;
    private TestTenant _other = null!;

    public async Task InitializeAsync()
    {
        await db.ResetAsync();
        _host = new B4DockerHost(db.ConnectionString);
        _tenant = await _host.CreateTenantAsync("DV-EXPORT");
        _other = await _host.CreateTenantAsync("DV-EXPORT-2");

        foreach (var (userName, fullName) in new[] { ("an.nv", "Nguyễn An"), ("binh.tt", "Trần Bình"), ("chi.lm", "Lê Chi"), ("dung.nv", "Nguyễn Dũng"), ("em.pt", "Phạm Em") })
        {
            var created = await _host.AsAsync(_tenant, sp => sp.GetRequiredService<ISender>().Send(
                new CreateUserCommand(userName, $"{userName}@vd.vn", fullName, B4DockerHost.Password, [])));
            created.IsSuccess.ShouldBeTrue($"{userName}: {created.Error?.Code}");
        }

        // Một người dùng ở đơn vị khác — không bao giờ được lọt vào tệp xuất của đơn vị này.
        (await _host.AsAsync(_other, sp => sp.GetRequiredService<ISender>().Send(
            new CreateUserCommand("nguoi.don.vi.khac", "khac@vd.vn", "Người đơn vị khác", B4DockerHost.Password, [])))).IsSuccess.ShouldBeTrue();
    }

    public async Task DisposeAsync() => await _host.DisposeAsync();

    private static UserSearchCriteria Criteria(string? search = null, string sortBy = "userName", bool desc = false, string? status = null)
        => new(1, 1000, sortBy, desc, search, null, status);

    private Task<T> InScopeAsync<T>(Func<IUserQueryService, Task<T>> action)
        => _host.AsAsync(_tenant, sp => action(sp.GetRequiredService<IUserQueryService>()));

    private async Task<IReadOnlyList<Guid>> StreamedIdsAsync(UserSearchCriteria criteria)
        => await _host.AsAsync(_tenant, async sp =>
        {
            var ids = new List<Guid>();
            await foreach (var user in sp.GetRequiredService<IUserQueryService>().StreamAsync(criteria, 10_000, CancellationToken.None))
                ids.Add(user.Id);
            return (IReadOnlyList<Guid>)ids;
        });

    // ---- Cùng bộ lọc với danh sách -----------------------------------------------------------------

    public static TheoryData<string?, string, bool, string?> Filters => new()
    {
        { null, "userName", false, null },
        { null, "userName", true, null },
        { "nv", "userName", false, null },
        { "an", "userName", true, null },
        { null, "createdAt", true, null },
        { null, "userName", false, "active" },
    };

    [Theory]
    [MemberData(nameof(Filters))]
    public async Task TheExportedSet_IsExactlyWhatTheListShows_InTheSameOrder(string? search, string sortBy, bool desc, string? status)
    {
        var criteria = Criteria(search, sortBy, desc, status);

        var listed = (await InScopeAsync(q => q.SearchAsync(criteria, CancellationToken.None))).Items.Select(u => u.Id).ToList();
        var streamed = await StreamedIdsAsync(criteria);
        var counted = await InScopeAsync(q => q.CountAsync(criteria, CancellationToken.None));

        listed.ShouldNotBeEmpty("bộ lọc phải khớp ít nhất một dòng — nếu không thì so sánh rỗng với rỗng");
        streamed.ShouldBe(listed);
        counted.ShouldBe(listed.Count);
    }

    [Fact]
    public async Task TheFilters_ReallyNarrowTheSet_SoTheEqualityAboveIsNotVacuous()
    {
        var all = await InScopeAsync(q => q.CountAsync(Criteria(), CancellationToken.None));
        var filtered = await InScopeAsync(q => q.CountAsync(Criteria("nv"), CancellationToken.None));

        filtered.ShouldBeLessThan(all);
        filtered.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task AnotherTenantsUser_IsNeverInTheExport()
    {
        var names = await _host.AsAsync(_tenant, async sp =>
        {
            var list = new List<string>();
            await foreach (var user in sp.GetRequiredService<IUserQueryService>().StreamAsync(Criteria(), 10_000, CancellationToken.None))
                list.Add(user.UserName);
            return list;
        });

        names.ShouldNotContain("nguoi.don.vi.khac");
        names.ShouldContain("an.nv");
    }

    // ---- Chính tệp -----------------------------------------------------------------------------------

    // Gửi lệnh VÀ ghi tệp trong CÙNG một scope — đúng đường thật: ExportFileResult gọi ExportFile.WriteToAsync khi ghi
    // phản hồi, tức sau commit nhưng vẫn trong vòng đời request (docs/contracts/exports.md, dòng "✅ CÓ THẬT" của §1 Ghi
    // chú). Hàm ghi đọc dữ liệu qua IUserQueryService của scope đó (scoped, giữ CoreDbContext); gọi nó sau khi scope đã
    // huỷ là một đường không request nào đi, và chỉ nhận ObjectDisposedException.
    private Task<byte[]> ExportBytesAsync(ExportUsersCommand command)
        => _host.AsAsync(_tenant, async sp =>
        {
            var result = await sp.GetRequiredService<ISender>().Send(command);
            result.IsSuccess.ShouldBeTrue(result.Error?.Code);

            using var output = new MemoryStream();
            await result.Value.WriteToAsync(output, CancellationToken.None);
            return output.ToArray();
        });

    private async Task<string> ExportAsync(ExportUsersCommand command)
        => Encoding.UTF8.GetString(await ExportBytesAsync(command));

    [Fact]
    public async Task TheCsvFile_HasOneRowPerListedUser_InTheListsOrder()
    {
        var listed = (await InScopeAsync(q => q.SearchAsync(Criteria(sortBy: "userName", desc: true), CancellationToken.None))).Items.Select(u => u.UserName).ToList();

        var text = await ExportAsync(new ExportUsersCommand(ExportFormats.Csv, "userName", SortDescending: true));

        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries).Skip(1).ToList();
        lines.Count.ShouldBe(listed.Count);
        for (var i = 0; i < listed.Count; i++)
            lines[i].ShouldStartWith(listed[i], customMessage: $"dòng {i + 2}");
    }

    [Fact]
    public async Task EveryExport_WritesOneAuditRow_WithTheFilterAndRowCount_ButNoPersonalData()
    {
        await ExportAsync(new ExportUsersCommand(ExportFormats.Csv, SearchText: "nv"));

        var rows = await db.QueryAsync(
            "SELECT action_code, after_value::text FROM core.audit_log WHERE action_code = @a",
            r => (r.GetString(0), r.GetString(1)),
            new NpgsqlParameter("a", AuditActions.UserExport));

        var row = rows.ShouldHaveSingleItem();
        using var after = JsonDocument.Parse(row.Item2);
        after.RootElement.GetProperty("searchText").GetString().ShouldBe("nv");
        after.RootElement.GetProperty("rowCount").GetInt32().ShouldBe(2);
        row.Item2.ShouldNotContain("an.nv");
        row.Item2.ShouldNotContain("@vd.vn");
    }

    [Fact]
    public async Task ARefusedExport_LeavesNoAuditRow()
    {
        await using var capped = new B4DockerHost(db.ConnectionString, new Dictionary<string, string?> { ["Core:Export:MaxRows"] = "2" });
        var tenant = await capped.CreateTenantAsync("DV-EXPORT-CAP");
        foreach (var name in new[] { "u1", "u2" })
            (await capped.AsAsync(tenant, sp => sp.GetRequiredService<ISender>().Send(
                new CreateUserCommand(name, $"{name}@vd.vn", name, B4DockerHost.Password, [])))).IsSuccess.ShouldBeTrue();

        var result = await capped.AsAsync(tenant, sp => sp.GetRequiredService<ISender>().Send(new ExportUsersCommand(ExportFormats.Csv)));

        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe(ExportErrors.TooManyRows.Code);
        (await db.CountAsync("SELECT count(*) FROM core.audit_log WHERE action_code = @a", new NpgsqlParameter("a", AuditActions.UserExport))).ShouldBe(0);
    }

    [Fact]
    public async Task TheXlsxFile_ReadsBackToTheSameRowsAsTheCsv()
    {
        using var output = new MemoryStream(await ExportBytesAsync(new ExportUsersCommand(ExportFormats.Xlsx, "userName")));

        await using var source = await _host.Services.GetRequiredService<ITabularReader>().OpenAsync(output, CancellationToken.None);
        var names = new List<string?>();
        await foreach (var row in source.ReadRowsAsync(CancellationToken.None))
            names.Add(row.Values["UserName"]);

        var listed = (await InScopeAsync(q => q.SearchAsync(Criteria(), CancellationToken.None))).Items.Select(u => u.UserName).ToList();
        names.ShouldBe(listed.Cast<string?>().ToList());
    }
}
