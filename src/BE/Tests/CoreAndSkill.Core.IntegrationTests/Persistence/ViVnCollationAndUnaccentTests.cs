using CoreAndSkill.Core.Application.Roles;
using CoreAndSkill.Core.Application.Tenants;
using CoreAndSkill.Core.Application.Users;
using CoreAndSkill.Core.Domain.Common;
using CoreAndSkill.Core.Domain.Tenants;
using CoreAndSkill.Core.Infrastructure.Identity;
using CoreAndSkill.Core.Infrastructure.Persistence;
using CoreAndSkill.Core.Infrastructure.Roles;
using CoreAndSkill.Core.Infrastructure.Tenants;
using CoreAndSkill.Core.Infrastructure.Users;
using CoreAndSkill.Core.IntegrationTests.Support;
using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Persistence;

// ADR-0119 (docs/adr/0119-cot-ten-dung-collation-icu-core-vi-vn-tim-khong-dau-qua-unaccent-is-deleted-co-default-false.md):
// ba cột tên mang collation `core.vi_vn`, `searchText` so khớp qua `unaccent`, `is_deleted` của mọi bảng kế thừa
// BaseEntity mang DEFAULT false, và hai script schema mới ở database/scripts/core/.
//
// Không cần Docker: đọc mô hình EF thật, ghi lại SQL bằng SqlRecorder, đọc tệp script. ĐIỂM MÙ: không chứng minh thứ tự
// sắp xếp hay việc `đ` khớp `d` trên PostgreSQL thật — đó là việc của ViVnCollationDatabaseTests (RequiresDocker).
public sealed class ViVnCollationAndUnaccentTests
{
    private const string ViVn = "core.vi_vn";

    [Fact]
    public void NameColumns_CarryViVnCollation()
    {
        using var db = OfflineCoreDbContext.Create(Guid.NewGuid());

        Collation(db, typeof(AppUser), nameof(AppUser.FullName)).ShouldBe(ViVn);
        Collation(db, typeof(Tenant), nameof(Tenant.Name)).ShouldBe(ViVn);
        Collation(db, typeof(AppRole), nameof(AppRole.Name)).ShouldBe(ViVn);
    }

    [Fact]
    public void BaseEntityTables_IsDeletedColumn_DefaultsToFalse()
    {
        using var db = OfflineCoreDbContext.Create(Guid.NewGuid());

        var entityTypes = db.Model.GetEntityTypes()
            .Where(t => !t.IsOwned() && typeof(BaseEntity).IsAssignableFrom(t.ClrType))
            .ToList();

        // Tập rỗng thì vòng dưới không kiểm gì (luật T6).
        entityTypes.ShouldNotBeEmpty();
        foreach (var entityType in entityTypes)
        {
            var property = entityType.FindProperty(nameof(BaseEntity.IsDeleted));
            property.ShouldNotBeNull(entityType.ClrType.Name);
            property.GetDefaultValue().ShouldBe(false, entityType.ClrType.Name);
        }
    }

    public static TheoryData<string> Searches => new() { "users", "roles", "tenants" };

    [Theory]
    [MemberData(nameof(Searches))]
    public async Task SearchText_IsMatchedThroughUnaccentOnColumnAndPattern(string search)
    {
        var recorder = new SqlRecorder();
        await using var db = OfflineCoreDbContext.Create(Guid.NewGuid(), recorder.Interceptors);

        Func<Task> run = search switch
        {
            "users" => () => new UserQueryService(db).SearchAsync(
                new UserSearchCriteria(1, 20, "userName", false, "nguyen", null, null), CancellationToken.None),
            "roles" => () => new RoleQueryService(db).SearchAsync(
                new RoleSearchCriteria(1, 20, "name", false, "nguyen"), CancellationToken.None),
            "tenants" => () => new TenantAdminQueryService(db).SearchAsync(
                new TenantSearchCriteria(1, 20, "name", false, "nguyen"), CancellationToken.None),
            _ => throw new ArgumentOutOfRangeException(nameof(search), search, null),
        };

        await Should.ThrowAsync<SqlRecorder.ReaderReachedException>(run);

        var sql = recorder.Executed.ShouldHaveSingleItem();
        sql.ShouldContain("ILIKE", Case.Sensitive, $"SQL: {sql}");
        // Cả cột lẫn mẫu đều qua unaccent: thiếu một vế thì `Nguyễn` không khớp `nguyen` (hoặc ngược lại).
        var ilikeCount = CountOccurrences(sql, "ILIKE");
        CountOccurrences(sql.ToLowerInvariant(), "unaccent(").ShouldBe(ilikeCount * 2, $"SQL: {sql}");
    }

    [Fact]
    public async Task RoleList_SortByName_OrdersByNameColumnNotNormalizedName()
    {
        // Lệnh đọc đầu là đếm tổng — trả một dòng giả để dịch vụ đi tới câu trang (có ORDER BY), lệnh đọc thứ hai.
        var count = new DataTable();
        count.Columns.Add("count", typeof(int));
        count.Rows.Add(1);
        var recorder = new SqlRecorder(readers: [count]);
        await using var db = OfflineCoreDbContext.Create(Guid.NewGuid(), recorder.Interceptors);

        await Should.ThrowAsync<SqlRecorder.ReaderReachedException>(() => new RoleQueryService(db).SearchAsync(
            new RoleSearchCriteria(1, 20, "name", false, null), CancellationToken.None));

        // Sắp xếp theo cột `name` thì collation vi_vn mới có tác dụng; `normalized_name` (chữ HOA bất biến) thì không.
        recorder.Executed.Count.ShouldBe(2);
        var sql = recorder.Executed[1];
        sql.ShouldContain("ORDER BY", Case.Sensitive, $"SQL: {sql}");
        var orderBy = sql[sql.IndexOf("ORDER BY", StringComparison.Ordinal)..];
        orderBy.ShouldNotContain("normalized_name", Case.Sensitive, $"SQL: {sql}");
    }

    [Fact]
    public async Task NewSchemaScripts_CarryGuardAndExpectedStatements()
    {
        var directory = PostgresFixture.LocateScriptsDirectory();
        var defaults = await File.ReadAllTextAsync(Path.Combine(directory, "0008__core__add-default-is-deleted.sql"));
        var collation = await File.ReadAllTextAsync(Path.Combine(directory, "0009__core__add-collation-vi-vn-and-unaccent.sql"));

        foreach (var script in new[] { defaults, collation })
        {
            // Khối tự chặn của script mới — docs/database/script-runbook.md §3.1.
            script.ShouldContain("DO $guard$");
            script.ShouldContain("INSERT INTO core.schema_script_history");
        }

        defaults.ShouldContain("ALTER TABLE core.role_permission ALTER COLUMN is_deleted SET DEFAULT FALSE");
        collation.ShouldContain("CREATE COLLATION core.vi_vn");
        collation.ShouldContain("PROVIDER = icu");
        collation.ShouldContain("DETERMINISTIC = True");
        collation.ShouldContain("CREATE EXTENSION IF NOT EXISTS unaccent SCHEMA public");
        collation.ShouldContain("ALTER COLUMN full_name TYPE character varying(200) COLLATE core.vi_vn");
        collation.ShouldContain("ALTER COLUMN name TYPE character varying(200) COLLATE core.vi_vn");
        collation.ShouldContain("ALTER COLUMN name TYPE character varying(256) COLLATE core.vi_vn");
    }

    private static string? Collation(DbContext db, Type clrType, string property)
        => db.GetService<IDesignTimeModel>().Model.FindEntityType(clrType)!.FindProperty(property)!.GetCollation();

    private static int CountOccurrences(string text, string value)
    {
        var count = 0;
        for (var index = text.IndexOf(value, StringComparison.Ordinal); index >= 0;
             index = text.IndexOf(value, index + value.Length, StringComparison.Ordinal))
            count++;
        return count;
    }
}
