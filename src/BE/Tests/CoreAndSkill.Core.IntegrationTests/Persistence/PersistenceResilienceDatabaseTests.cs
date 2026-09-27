using System.Data.Common;
using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Application.Roles;
using CoreAndSkill.Core.Infrastructure.Identity;
using CoreAndSkill.Core.Infrastructure.Persistence;
using CoreAndSkill.Core.IntegrationTests.Support;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Persistence;

// CHƯA CHẠY THỬ TRONG MÔI TRƯỜNG NÀY — không có Docker daemon lúc viết, nên các test dưới đây CHƯA
// TỪNG được thực thi một lần nào: chúng biên dịch được, chỉ vậy thôi. Đừng đọc sự tồn tại của chúng
// là bằng chứng chúng xanh. CI là nơi chạy chúng lần ĐẦU TIÊN.
// Gỡ khối chú thích này ngay khi có người chạy thật và thấy kết quả.
//
// Hai hành vi chỉ chứng minh được trên PostgreSQL thật, là nửa còn lại của CoreDbContextResilienceTests và
// SearchTextLikeEscapingTests (không cần Docker):
//   1. docs/quy-uoc/be-performance.md §7.2 — kết nối Scoped DÙNG CHUNG bị máy chủ cắt giữa transaction: UnitOfWork
//      phải mở lại chính kết nối đó ở lượt thử lại và commit được.
//   2. `searchText` khớp theo nghĩa đen — `_` và `%` không phải ký tự đại diện.
[Trait("Category", "RequiresDocker")]
[Collection(PostgresCollection.Name)]
public sealed class PersistenceResilienceDatabaseTests(PostgresFixture db) : IAsyncLifetime
{
    private readonly B4DockerHost _host = new(db.ConnectionString);

    public Task InitializeAsync() => db.ResetAsync();

    public async Task DisposeAsync() => await _host.DisposeAsync();

    [Fact]
    public async Task UnitOfWork_SharedConnectionKilledMidTransaction_ReopensAndCommitsOnRetry()
    {
        var tenant = await _host.CreateTenantAsync("DV-RETRY");
        var attempts = 0;

        await _host.AsAsync(tenant, async sp =>
        {
            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
            var context = sp.GetRequiredService<CoreDbContext>();
            var connection = (NpgsqlConnection)sp.GetRequiredService<DbConnection>();

            await unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                attempts++;
                if (attempts == 1)
                {
                    // Máy chủ cắt đúng phiên đang giữ transaction — cùng hình dạng một nhịp chớp mạng.
                    await TerminateBackendAsync(connection.ProcessID);
                    await context.DataProtectionKeys.CountAsync(ct);
                }

                context.DataProtectionKeys.Add(new DataProtectionKey { FriendlyName = "retry-probe", Xml = "<key/>" });
                return new TransactionOutcome<int>(0, ShouldCommit: true);
            });
        });

        attempts.ShouldBe(2);
        (await db.CountAsync("SELECT count(*) FROM core.data_protection_key WHERE friendly_name = 'retry-probe'")).ShouldBe(1);
    }

    [Theory]
    [InlineData("m_a", "nhom_a")]
    [InlineData("%", "tron100%")]
    [InlineData(@"a\b", @"a\b")]
    public async Task RoleSearch_WithLikeWildcard_MatchesLiterally(string searchText, string onlyMatch)
    {
        var tenant = await _host.CreateTenantAsync("DV-LIKE");

        await _host.AsAsync(tenant, async sp =>
        {
            var roles = sp.GetRequiredService<RoleManager<AppRole>>();
            foreach (var name in new[] { "nhom_a", "nhomxa", "tron100%", "tron1000", @"a\b", "ab" })
                (await roles.CreateAsync(new AppRole { Name = name })).Succeeded.ShouldBeTrue(name);
        });

        var found = await _host.AsAsync(tenant, sp => sp.GetRequiredService<IRoleQueryService>().SearchAsync(
            new RoleSearchCriteria(1, 200, "name", false, searchText), CancellationToken.None));

        found.Items.Select(r => r.Name).ShouldBe([onlyMatch]);
    }

    private async Task TerminateBackendAsync(int processId)
    {
        await using var admin = new NpgsqlConnection(db.ConnectionString);
        await admin.OpenAsync();
        await using var command = new NpgsqlCommand("SELECT pg_terminate_backend(@pid)", admin);
        command.Parameters.Add(new NpgsqlParameter("pid", processId));
        await command.ExecuteScalarAsync();
    }
}
