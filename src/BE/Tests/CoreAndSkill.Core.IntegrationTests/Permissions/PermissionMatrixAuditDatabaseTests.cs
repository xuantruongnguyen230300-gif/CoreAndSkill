using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CoreAndSkill.Core.Application.Permissions;
using CoreAndSkill.Core.Application.Tenants;
using CoreAndSkill.Core.Infrastructure.Persistence.Interceptors;
using CoreAndSkill.Core.IntegrationTests.Support;
using CoreAndSkill.Core.Web.Http;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Permissions;

// CHƯA CHẠY THỬ TRONG MÔI TRƯỜNG NÀY — không có Docker daemon lúc viết, nên các test dưới đây CHƯA
// TỪNG được thực thi một lần nào: chúng biên dịch được, chỉ vậy thôi. Đừng đọc sự tồn tại của chúng
// là bằng chứng chúng xanh. CI là nơi chạy chúng lần ĐẦU TIÊN.
// Gỡ khối chú thích này ngay khi có người chạy thật và thấy kết quả.
//
// Luật S17 (docs/RULES.md §6) — PUT /api/v1/core/permissions/matrix qua HTTP tới PostgreSQL thật, rồi đọc thẳng
// core.audit_log. Hình dạng dòng: docs/contracts/permissions.md §6 mục "Nhật ký kiểm toán".
//
// Thứ test không database (PermissionMatrixAuditShapeTests) KHÔNG chứng minh được và test này chứng minh: nhánh
// AuditLogInterceptor hỏi database lấy mã khoá và tên vai trò (đường PUT thật không để Permission/AppRole nào trong bộ
// theo dõi), dòng thật sự vào bảng trong cùng transaction, actor_user_id là người gọi HTTP, và after_value đọc lại được
// từ cột jsonb.
[Trait("Category", "RequiresDocker")]
[Collection(PostgresCollection.Name)]
public sealed class PermissionMatrixAuditDatabaseTests(PostgresFixture db) : IAsyncLifetime
{
    private const string Url = "/api/v1/core/permissions/matrix";
    private const string SecondRole = "AuditRoleB";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly B4DockerHost _host = new(db.ConnectionString, configure: services =>
    {
        services.AddAuthentication(options =>
            {
                options.DefaultScheme = InMemoryCoreHost.TestScheme;
                options.DefaultAuthenticateScheme = InMemoryCoreHost.TestScheme;
                options.DefaultChallengeScheme = InMemoryCoreHost.TestScheme;
            })
            .AddScheme<AuthenticationSchemeOptions, InMemoryCoreHost.TestAuthHandler>(InMemoryCoreHost.TestScheme, _ => { });
        services.AddDataProtection().UseEphemeralDataProtectionProvider();
        services.AddSingleton<ITenantSeedSource, SecondRoleSeedSource>();
    });

    public Task InitializeAsync() => db.ResetAsync();

    public async Task DisposeAsync() => await _host.DisposeAsync();

    [Fact]
    public async Task PermissionMatrixUpdate_WritesOneAuditRowPerTouchedRole_WithGrantedAndRevokedCodes()
    {
        var tenant = await _host.CreateTenantAsync("DV-AUD");
        using var client = Client(tenant);
        var matrix = await GetMatrixAsync(client);
        var roleA = matrix.RoleIdByName[FakeTenantSeedSource.RoleName];
        var roleB = matrix.RoleIdByName[SecondRole];
        var grantCode = CorePermissions.RoleRead;
        var revokeCode = SecondRoleSeedSource.PermissionCode;

        matrix.Grants[matrix.IdByCode[grantCode]].ShouldNotContain(roleA, "chống test rỗng: ô cấp phải đang trống");
        matrix.Grants[matrix.IdByCode[revokeCode]].ShouldContain(roleB, "chống test rỗng: ô thu phải đang được cấp");
        var before = await MatrixAuditRowIdsAsync(tenant.Id);

        // Một PUT: cấp một khoá cho A, thu một khoá của B.
        var response = await client.PutAsJsonAsync(Url, Payload(matrix, change: cell =>
            cell.Code == grantCode ? cell.RoleIds.Append(roleA)
            : cell.Code == revokeCode ? cell.RoleIds.Where(r => r != roleB)
            : cell.RoleIds));
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        var rows = (await ReadMatrixAuditRowsAsync(tenant.Id)).Where(r => !before.Contains(r.Id)).ToList();
        rows.Count.ShouldBe(2);

        var rowA = rows.Single(r => r.TargetId == roleA.ToString());
        rowA.TargetType.ShouldBe("core.role");
        rowA.TargetDisplay.ShouldBe(FakeTenantSeedSource.RoleName);
        rowA.ActorUserId.ShouldBe(tenant.AdminUserId);
        rowA.BeforeValue.ShouldBeNull();
        rowA.Granted.ShouldBe([grantCode]);
        rowA.Revoked.ShouldBeEmpty();

        var rowB = rows.Single(r => r.TargetId == roleB.ToString());
        rowB.TargetDisplay.ShouldBe(SecondRole);
        rowB.ActorUserId.ShouldBe(tenant.AdminUserId);
        rowB.BeforeValue.ShouldBeNull();
        rowB.Granted.ShouldBeEmpty();
        rowB.Revoked.ShouldBe([revokeCode]);

        // PUT không đổi ô nào — 200, không dòng nào (ADR-0052 quyết định 5).
        var unchanged = await GetMatrixAsync(client);
        var afterFirst = await MatrixAuditRowIdsAsync(tenant.Id);
        var noOp = await client.PutAsJsonAsync(Url, Payload(unchanged, change: cell => cell.RoleIds));
        noOp.StatusCode.ShouldBe(HttpStatusCode.OK, await noOp.Content.ReadAsStringAsync());
        (await MatrixAuditRowIdsAsync(tenant.Id)).ShouldBe(afterFirst, ignoreOrder: true);
    }

    // ---- Hạ tầng của test --------------------------------------------------------------------

    // Nguồn seed thứ hai, gộp với FakeTenantSeedSource: một vai trò nữa đang được cấp sẵn một khoá, để một PUT vừa cấp
    // (vai trò A) vừa thu (vai trò B).
    private sealed class SecondRoleSeedSource : ITenantSeedSource
    {
        public const string PermissionCode = CorePermissions.UserRead;

        public IReadOnlyCollection<SeedRole> GetRoles() => [new SeedRole(SecondRole, IsSystem: false)];

        public IReadOnlyCollection<SeedRolePermission> GetRolePermissions() => [new SeedRolePermission(SecondRole, PermissionCode)];

        public IReadOnlyCollection<SeedMenuItem> GetMenuItems() => [];
    }

    private sealed record Cell(string Code, IEnumerable<Guid> RoleIds);

    private sealed record Matrix(
        string Version,
        IReadOnlyDictionary<string, Guid> RoleIdByName,
        IReadOnlyDictionary<string, Guid> IdByCode,
        IReadOnlyDictionary<Guid, HashSet<Guid>> Grants);

    private sealed record AuditRow(
        Guid Id, string TargetType, string TargetId, string? TargetDisplay, Guid? ActorUserId, string? BeforeValue,
        string[] Granted, string[] Revoked);

    private static object Payload(Matrix matrix, Func<Cell, IEnumerable<Guid>> change) => new
    {
        version = matrix.Version,
        entries = matrix.IdByCode.Select(kv => new
        {
            permissionId = kv.Value,
            roleIds = change(new Cell(kv.Key, matrix.Grants[kv.Value])).Distinct().ToArray(),
        }).ToArray(),
    };

    private async Task<IReadOnlyList<Guid>> MatrixAuditRowIdsAsync(Guid tenantId)
        => (await ReadMatrixAuditRowsAsync(tenantId)).Select(r => r.Id).ToList();

    private Task<IReadOnlyList<AuditRow>> ReadMatrixAuditRowsAsync(Guid tenantId)
        => db.QueryAsync(
            """
            SELECT id, target_type, target_id, target_display, actor_user_id, before_value::text, after_value::text
            FROM core.audit_log
            WHERE tenant_id = @tenant AND action_code = @action
            """,
            reader =>
            {
                var after = reader.IsDBNull(6) ? null : reader.GetString(6);
                string[] granted = [], revoked = [];
                if (after is not null)
                {
                    using var json = JsonDocument.Parse(after);
                    granted = json.RootElement.GetProperty("granted").EnumerateArray().Select(e => e.GetString()!).ToArray();
                    revoked = json.RootElement.GetProperty("revoked").EnumerateArray().Select(e => e.GetString()!).ToArray();
                }

                return new AuditRow(
                    reader.GetGuid(0), reader.GetString(1), reader.GetString(2),
                    reader.IsDBNull(3) ? null : reader.GetString(3),
                    reader.IsDBNull(4) ? null : reader.GetGuid(4),
                    reader.IsDBNull(5) ? null : reader.GetString(5),
                    granted, revoked);
            },
            new NpgsqlParameter("tenant", tenantId),
            new NpgsqlParameter("action", AuditActionCodes.PermissionMatrixUpdate));

    private static async Task<Matrix> GetMatrixAsync(HttpClient client)
    {
        var response = await client.GetAsync(Url);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var data = JsonSerializer.Deserialize<ApiEnvelope<JsonElement>>(await response.Content.ReadAsStringAsync(), Json)!.Data;

        var roles = data.GetProperty("roles").EnumerateArray()
            .ToDictionary(r => r.GetProperty("name").GetString()!, r => r.GetProperty("id").GetGuid());
        var rows = data.GetProperty("rows").EnumerateArray().ToList();

        return new Matrix(
            data.GetProperty("version").GetString()!,
            roles,
            rows.ToDictionary(r => r.GetProperty("code").GetString()!, r => r.GetProperty("permissionId").GetGuid()),
            rows.ToDictionary(
                r => r.GetProperty("permissionId").GetGuid(),
                r => r.GetProperty("grantedRoleIds").EnumerateArray().Select(x => x.GetGuid()).ToHashSet()));
    }

    private HttpClient Client(TestTenant tenant)
    {
        var client = _host.Factory.CreateHttpsClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add("X-Test-User", tenant.AdminUserId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-UserName", tenant.AdminUserName);
        client.DefaultRequestHeaders.Add("X-Test-Tenant", tenant.Id.ToString());

        var token = client.GetFromJsonAsync<JsonElement>("/api/v1/core/antiforgery/token", Json).GetAwaiter().GetResult()
            .GetProperty("data").GetProperty("token").GetString()!;
        client.DefaultRequestHeaders.Add("X-XSRF-TOKEN", token);
        return client;
    }
}
