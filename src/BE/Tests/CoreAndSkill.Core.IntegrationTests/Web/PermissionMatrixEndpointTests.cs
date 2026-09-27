using System.Net;
using System.Net.Http.Json;
using System.Text;
using CoreAndSkill.Core.Application.Permissions;
using CoreAndSkill.Core.IntegrationTests.Support;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Web;

// PUT /api/v1/core/permissions/matrix — docs/contracts/permissions.md §6, bảng "Lỗi". Chỉ các ca bị chặn TRƯỚC khi chạm
// database (validator, đầu handler): host không có PostgreSQL, nên ca nào lọt xuống IPermissionMatrixService sẽ ra 500
// vì không nối được — đúng thứ các test dưới đây phải chứng minh là KHÔNG xảy ra.
//
// ENTRIES_INCOMPLETE cần danh mục quyền trong DB — PermissionMatrixDatabaseTests (RequiresDocker).
public sealed class PermissionMatrixEndpointTests(InMemoryHostFixture fixture)
    : B4EndpointTestBase(fixture), IClassFixture<InMemoryHostFixture>
{
    private const string Url = "/api/v1/core/permissions/matrix";

    private HttpClient PermissionWriter()
    {
        var user = Guid.NewGuid();
        _host.Permissions.Grant(user, CorePermissions.PermissionWrite);
        return _host.CreateClient(user);
    }

    private static StringContent JsonBody(string json) => new(json, Encoding.UTF8, "application/json");

    [Fact]
    public async Task DuplicatePermissionId_Returns400_WithFieldErrorOnEntries_AndNamesTheDuplicatedId()
    {
        using var client = PermissionWriter();
        var duplicated = Guid.NewGuid();

        var response = await client.PutAsJsonAsync(Url, new
        {
            version = "sha256:00000000",
            entries = new object[]
            {
                new { permissionId = Guid.NewGuid(), roleIds = Array.Empty<Guid>() },
                new { permissionId = duplicated, roleIds = Array.Empty<Guid>() },
                new { permissionId = duplicated, roleIds = new[] { Guid.NewGuid() } },
            },
        });
        var error = (await ReadEnvelope(response)).Error!;

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        error.Code.ShouldBe(PermissionErrors.DuplicateEntry.Code);
        error.MessageParams.ShouldNotBeNull();
        error.MessageParams["PermissionId"].ShouldBe(duplicated.ToString());

        // Khoá "Entries" KHÔNG chỉ số — lỗi thuộc quan hệ giữa các phần tử (card §6 "Ghi chú").
        error.FieldErrors.ShouldNotBeNull();
        error.FieldErrors.Keys.ShouldBe(["Entries"]);
        error.FieldErrors["Entries"].Single().Code.ShouldBe(PermissionErrors.DuplicateEntry.Code);
    }

    // V-01: phần tử null trong entries không được đi tới handler (NullReferenceException ⇒ 500).
    [Fact]
    public async Task NullEntry_Returns400_ValidationFailed_NotA500()
    {
        using var client = PermissionWriter();

        var response = await client.PutAsync(Url, JsonBody("""{"version":"x","entries":[null]}"""));
        var error = (await ReadEnvelope(response)).Error!;

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        error.Code.ShouldBe("CORE.VALIDATION.FAILED");
        error.FieldErrors!["Entries[0]"].Single().Code.ShouldBe("CORE.VALIDATION.REQUIRED");
    }

    // entries[].roleIds là field bắt buộc (card §6, bảng field) — null không được đi xuống tầng ghi.
    [Fact]
    public async Task NullRoleIds_Returns400_ValidationFailed_NotA500()
    {
        using var client = PermissionWriter();

        var response = await client.PutAsync(Url,
            JsonBody($$"""{"version":"x","entries":[{"permissionId":"{{Guid.NewGuid()}}","roleIds":null}]}"""));
        var error = (await ReadEnvelope(response)).Error!;

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        error.Code.ShouldBe("CORE.VALIDATION.FAILED");
        error.FieldErrors!["Entries[0].RoleIds"].Single().Code.ShouldBe("CORE.VALIDATION.REQUIRED");
    }
}
