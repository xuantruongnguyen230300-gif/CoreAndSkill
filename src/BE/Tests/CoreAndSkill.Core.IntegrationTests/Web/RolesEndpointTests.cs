using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CoreAndSkill.Core.Application.Common;
using CoreAndSkill.Core.Application.Common.Paging;
using CoreAndSkill.Core.Application.Permissions;
using CoreAndSkill.Core.Application.Roles;
using CoreAndSkill.Core.Domain.Common;
using CoreAndSkill.Core.IntegrationTests.Support;
using CoreAndSkill.Core.Web.Http;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Web;

// Hợp đồng trên dây của token đồng thời cho vai trò — docs/contracts/roles.md §1, §3, §5; khuôn
// docs/wiki-core/be/06-concurrency-control.md §6.3. Pipeline HTTP thật (binding, validator, handler, envelope, ánh xạ
// ErrorType → HTTP); hai seam vai trò thay bằng bản trong bộ nhớ (InMemoryRoles).
//
// Thứ nó chứng minh: `version` có mặt ở GET danh sách và chi tiết; PUT đọc `version` từ body và đưa nó xuống tầng ghi;
// xung đột ra 409 CORE.CONCURRENCY.CONFLICT; ghi thành công trả bản ghi mang version mới.
// Thứ nó KHÔNG chứng minh: phép so thật — RoleRenameConcurrencyTests (RoleStore thật) và RoleRenameDatabaseTests.
public sealed class RolesEndpointTests(RolesHostFixture fixture) : IClassFixture<RolesHostFixture>
{
    private const string Url = "/api/v1/core/roles";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly InMemoryCoreHost _host = fixture.Reset();
    private readonly InMemoryRoles _roles = fixture.Roles;

    private HttpClient RoleAdmin()
    {
        var user = Guid.NewGuid();
        _host.Permissions.Grant(user, CorePermissions.RoleRead);
        _host.Permissions.Grant(user, CorePermissions.RoleWrite);
        return _host.CreateClient(user);
    }

    [Fact]
    public async Task ListAndDetail_CarryVersion()
    {
        var (id, stamp) = _roles.Add("Kế toán");
        using var client = RoleAdmin();

        var list = (await ReadEnvelope(await client.GetAsync(Url))).Data;
        var item = list.GetProperty("items").EnumerateArray().Single();
        item.GetProperty("version").GetString().ShouldBe(stamp);

        var detail = (await ReadEnvelope(await client.GetAsync($"{Url}/{id}"))).Data;
        detail.GetProperty("version").GetString().ShouldBe(stamp);
    }

    [Fact]
    public async Task Put_StaleVersion_Returns409_ConcurrencyConflict_AndNameUnchanged()
    {
        var (id, _) = _roles.Add("Kế toán");
        using var client = RoleAdmin();

        var response = await client.PutAsJsonAsync($"{Url}/{id}", new { name = "Kế toán trưởng", version = "stamp-cu" });

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await ReadEnvelope(response)).Error!.Code.ShouldBe(CommonErrors.ConcurrencyConflict.Code);
        _roles.NameOf(id).ShouldBe("Kế toán");
    }

    // Thiếu `version` là 409, KHÔNG phải 400 — §6.3 luật 3: "Lệch hoặc thiếu ⇒ 409", `null` không bao giờ khớp.
    [Fact]
    public async Task Put_MissingVersion_Returns409_ConcurrencyConflict_AndNameUnchanged()
    {
        var (id, _) = _roles.Add("Kế toán");
        using var client = RoleAdmin();

        var response = await client.PutAsJsonAsync($"{Url}/{id}", new { name = "Kế toán trưởng" });

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await ReadEnvelope(response)).Error!.Code.ShouldBe(CommonErrors.ConcurrencyConflict.Code);
        _roles.NameOf(id).ShouldBe("Kế toán");
    }

    [Fact]
    public async Task Put_CurrentVersion_Returns200_WithRenamedRoleAndNewVersion()
    {
        var (id, stamp) = _roles.Add("Kế toán");
        using var client = RoleAdmin();

        var response = await client.PutAsJsonAsync($"{Url}/{id}", new { name = "Kế toán trưởng", version = stamp });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var data = (await ReadEnvelope(response)).Data;
        data.GetProperty("id").GetGuid().ShouldBe(id);
        data.GetProperty("name").GetString().ShouldBe("Kế toán trưởng");
        var newVersion = data.GetProperty("version").GetString();
        newVersion.ShouldNotBeNullOrEmpty();
        newVersion.ShouldNotBe(stamp);

        // Version cũ đã hết hiệu lực (§6.3 luật 4) — gửi lại nó là 409.
        var again = await client.PutAsJsonAsync($"{Url}/{id}", new { name = "Kế toán tổng hợp", version = stamp });
        again.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        _roles.NameOf(id).ShouldBe("Kế toán trưởng");
    }

    // Tên rỗng vẫn là lỗi validation — chặn trước tầng ghi, như trước khi có version.
    [Fact]
    public async Task Put_EmptyName_Returns400_ValidationFailed()
    {
        var (id, stamp) = _roles.Add("Kế toán");
        using var client = RoleAdmin();

        var response = await client.PutAsJsonAsync($"{Url}/{id}", new { name = "", version = stamp });
        var error = (await ReadEnvelope(response)).Error!;

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        error.Code.ShouldBe("CORE.VALIDATION.FAILED");
        error.FieldErrors!.Keys.ShouldContain("Name");
    }

    private static async Task<ApiEnvelope<JsonElement>> ReadEnvelope(HttpResponseMessage response)
        => JsonSerializer.Deserialize<ApiEnvelope<JsonElement>>(await response.Content.ReadAsStringAsync(), Json)!;
}

public sealed class RolesHostFixture : IAsyncLifetime
{
    internal InMemoryRoles Roles { get; } = new();

    internal InMemoryCoreHost Host { get; }

    public RolesHostFixture()
    {
        Host = new InMemoryCoreHost(configure: services =>
        {
            foreach (var descriptor in services
                         .Where(d => d.ServiceType == typeof(IRoleQueryService) || d.ServiceType == typeof(IRoleAdminService))
                         .ToList())
            {
                services.Remove(descriptor);
            }

            services.AddSingleton<IRoleQueryService>(Roles);
            services.AddSingleton<IRoleAdminService>(Roles);
        });
    }

    internal InMemoryCoreHost Reset()
    {
        Host.Reset();
        Roles.Rows.Clear();
        return Host;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await Host.DisposeAsync();
}

// Vai trò trong bộ nhớ, token theo đúng luật §6.3: so nguyên chuỗi, null không khớp, ghi xong thì token đổi.
internal sealed class InMemoryRoles : IRoleQueryService, IRoleAdminService
{
    public ConcurrentDictionary<Guid, (string Name, string Stamp)> Rows { get; } = new();

    public (Guid Id, string Stamp) Add(string name)
    {
        var id = Guid.NewGuid();
        var stamp = Guid.NewGuid().ToString();
        Rows[id] = (name, stamp);
        return (id, stamp);
    }

    public string NameOf(Guid id) => Rows[id].Name;

    private RoleSummaryDto ToDto(Guid id) => new(id, Rows[id].Name, false, 0, null, Rows[id].Stamp);

    public Task<PagedList<RoleSummaryDto>> SearchAsync(RoleSearchCriteria criteria, CancellationToken ct)
        => Task.FromResult(new PagedList<RoleSummaryDto>
        {
            Items = [.. Rows.Keys.Select(ToDto)],
            Page = criteria.Page,
            PageSize = criteria.PageSize,
            TotalCount = Rows.Count,
        });

    public Task<RoleSummaryDto?> FindByIdAsync(Guid id, CancellationToken ct)
        => Task.FromResult(Rows.ContainsKey(id) ? ToDto(id) : null);

    public Task<bool> ExistsAsync(Guid id, CancellationToken ct) => Task.FromResult(Rows.ContainsKey(id));

    public Task<IReadOnlySet<Guid>> FindExistingIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct)
        => Task.FromResult<IReadOnlySet<Guid>>(ids.Where(Rows.ContainsKey).ToHashSet());

    public Task<bool> NameExistsAsync(string name, Guid? excludingId, CancellationToken ct)
        => Task.FromResult(Rows.Any(r => r.Key != excludingId && string.Equals(r.Value.Name, name, StringComparison.OrdinalIgnoreCase)));

    public Task<bool> UserHoldsAnySystemRoleAsync(Guid userId, CancellationToken ct) => Task.FromResult(false);

    public Task<Result<Guid>> CreateAsync(string name, CancellationToken ct) => Task.FromResult(Result.Success(Add(name).Id));

    public Task<Result> RenameAsync(Guid roleId, string name, string? version, CancellationToken ct)
    {
        if (!Rows.TryGetValue(roleId, out var row))
            return Task.FromResult(Result.Failure(RoleErrors.NotFound));

        if (!string.Equals(version, row.Stamp, StringComparison.Ordinal))
            return Task.FromResult(Result.Failure(CommonErrors.ConcurrencyConflict));

        Rows[roleId] = (name, Guid.NewGuid().ToString());
        return Task.FromResult(Result.Success());
    }

    public Task<Result> DeleteAsync(Guid roleId, CancellationToken ct)
        => Task.FromResult(Rows.TryRemove(roleId, out _) ? Result.Success() : Result.Failure(RoleErrors.NotFound));
}
