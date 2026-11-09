using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Application.Tenants;
using CoreAndSkill.Core.Infrastructure.Persistence;
using CoreAndSkill.Core.Infrastructure.Tenants;
using CoreAndSkill.Core.IntegrationTests.Support;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NSubstitute;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Persistence;

// ADR-0119: phần cần PostgreSQL thật của ViVnCollationAndUnaccentTests (không Docker).
//   1. Cột tên sắp xếp theo bảng chữ cái tiếng Việt (collation ICU `core.vi_vn`): `ă`, `â`, `đ`... đứng sau chữ gốc.
//   2. `searchText` không phân biệt dấu và hoa thường: `nguyen` ra `Nguyễn`, `do` ra `Đỗ`.
//   3. Câu INSERT tay bỏ `is_deleted` vẫn chạy (cột mang DEFAULT false).
//   4. So sánh bằng (`=`) vẫn phân biệt dấu — collation deterministic.
[Trait("Category", "RequiresDocker")]
[Collection(PostgresCollection.Name)]
public sealed class ViVnCollationDatabaseTests(PostgresFixture db) : IAsyncLifetime
{
    public Task InitializeAsync() => db.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task TenantName_OrderBy_FollowsVietnameseAlphabet()
    {
        foreach (var (code, name) in new[] { ("T1", "Đào"), ("T2", "Dung"), ("T3", "Ân"), ("T4", "An"), ("T5", "Ba") })
            await InsertTenantAsync(code, name);

        var names = await db.QueryAsync("SELECT name FROM core.tenant ORDER BY name, id", r => r.GetString(0));

        names.ShouldBe(["An", "Ân", "Ba", "Dung", "Đào"]);
    }

    [Fact]
    public async Task TenantSearch_IgnoresDiacriticsAndCase()
    {
        await InsertTenantAsync("NGUYEN", "Công ty Nguyễn Văn");
        await InsertTenantAsync("DO", "Đỗ Gia");
        await InsertTenantAsync("KHAC", "Lê Thị");

        await using var context = CreateContext();
        var service = new TenantAdminQueryService(context);

        var byNguyen = await service.SearchAsync(
            new TenantSearchCriteria(1, 20, "name", false, "NGUYEN van"), CancellationToken.None);
        byNguyen.Items.Select(t => t.Name).ShouldBe(["Công ty Nguyễn Văn"]);

        var byD = await service.SearchAsync(new TenantSearchCriteria(1, 20, "name", false, "do gia"), CancellationToken.None);
        byD.Items.Select(t => t.Name).ShouldBe(["Đỗ Gia"]);
    }

    [Fact]
    public async Task InsertWithoutIsDeleted_UsesColumnDefaultFalse()
    {
        // core.permission_resource kế thừa BaseEntity; câu INSERT tay bỏ cột is_deleted.
        await db.CountAsync(
            "WITH i AS (INSERT INTO core.permission_resource (id, key, name_key) " +
            "VALUES (gen_random_uuid(), 'adr0119-probe', 'x') RETURNING 1) SELECT count(*) FROM i");

        var deleted = await db.QueryAsync(
            "SELECT is_deleted FROM core.permission_resource WHERE key = 'adr0119-probe'", r => r.GetBoolean(0));
        deleted.ShouldBe([false]);
    }

    [Fact]
    public async Task Equality_StillDistinguishesDiacritics()
    {
        (await db.CountAsync("SELECT count(*) FROM (SELECT 'Nguyễn'::text COLLATE core.vi_vn AS a) s WHERE a = 'Nguyen'"))
            .ShouldBe(0);
    }

    private Task InsertTenantAsync(string code, string name)
        => db.CountAsync(
            "WITH i AS (INSERT INTO core.tenant (id, code, name) VALUES (gen_random_uuid(), @code, @name) RETURNING 1) " +
            "SELECT count(*) FROM i",
            new NpgsqlParameter("code", code), new NpgsqlParameter("name", name));

    private CoreDbContext CreateContext()
    {
        var tenantContext = Substitute.For<ITenantContext>();
        var options = new DbContextOptionsBuilder<CoreDbContext>()
            .UseNpgsql(db.ConnectionString)
            .UseSnakeCaseNamingConvention()
            .Options;
        return new CoreDbContext(options, tenantContext);
    }
}
