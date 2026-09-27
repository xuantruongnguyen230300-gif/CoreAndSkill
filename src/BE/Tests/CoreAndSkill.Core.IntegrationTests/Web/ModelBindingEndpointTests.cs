using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using CoreAndSkill.Core.Application.Permissions;
using CoreAndSkill.Core.IntegrationTests.Support;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Web;

// docs/quy-uoc/be-api-controller.md §2.4 và docs/contracts/auth.md (bảng mã fieldErrors của login). Ô bỏ trống là việc
// của VALIDATOR (CORE.VALIDATION.REQUIRED), không phải của model binding: với <Nullable>enable</Nullable>, MVC mặc định tự
// gắn [Required] cho mọi tham số `string` không-null của record body, chặn request trước khi action (và validator) chạy,
// rồi ModelBindingProblemFactory gắn nhãn FORMAT cho nó — FE hiện "sai định dạng" cho một ô bỏ trống.
//
// Model binding chỉ còn lo đúng phần §2.4 khai: body không parse được, sai kiểu.
public sealed class ModelBindingEndpointTests(InMemoryHostFixture fixture) : B4EndpointTestBase(fixture), IClassFixture<InMemoryHostFixture>
{
    private static StringContent JsonBody(string json) => new(json, Encoding.UTF8, "application/json");

    // Đăng nhập là endpoint ẩn danh nhưng vẫn đòi token CSRF — lấy token cho một client chưa đăng nhập.
    private HttpClient AnonymousClientWithCsrf()
    {
        var client = _host.CreateClient(userId: null);
        var response = client.GetAsync("/api/v1/core/antiforgery/token").GetAwaiter().GetResult();
        response.EnsureSuccessStatusCode();
        var token = response.Content.ReadFromJsonAsync<JsonElement>(Json).GetAwaiter().GetResult()
            .GetProperty("data").GetProperty("token").GetString()!;
        client.DefaultRequestHeaders.Add("X-XSRF-TOKEN", token);
        return client;
    }

    private HttpClient RoleWriter()
    {
        var user = Guid.NewGuid();
        _host.Permissions.Grant(user, CorePermissions.RoleWrite);
        return _host.CreateClient(user);
    }

    [Theory]
    [InlineData("""{"tenantCode":"","userName":"a","password":"b"}""")]
    [InlineData("""{"tenantCode":null,"userName":"a","password":"b"}""")]
    [InlineData("""{"userName":"a","password":"b"}""")]
    public async Task Login_AnEmptyOrMissingTenantCode_IsRequired_NotFormat(string json)
    {
        using var client = AnonymousClientWithCsrf();

        var response = await client.PostAsync("/api/v1/core/auth/login", JsonBody(json));
        var error = (await ReadEnvelope(response)).Error!;

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        error.Code.ShouldBe("CORE.VALIDATION.FAILED");
        error.FieldErrors!["TenantCode"].Single().Code.ShouldBe("CORE.VALIDATION.REQUIRED");
    }

    [Fact]
    public async Task Login_AllThreeEmpty_EachFieldIsRequired()
    {
        using var client = AnonymousClientWithCsrf();

        var response = await client.PostAsync("/api/v1/core/auth/login", JsonBody("""{"tenantCode":"","userName":"","password":""}"""));
        var fieldErrors = (await ReadEnvelope(response)).Error!.FieldErrors!;

        fieldErrors.Keys.ShouldBe(["TenantCode", "UserName", "Password"], ignoreOrder: true);
        fieldErrors.Values.SelectMany(v => v).Select(e => e.Code).Distinct().ShouldBe(["CORE.VALIDATION.REQUIRED"]);
    }

    // Luật B1 — docs/quy-uoc/be-api-controller.md §2.3: payload đi ra camelCase, nhưng KHOÁ của fieldErrors giữ PascalCase,
    // khớp đúng tên property C# của DTO (cơ chế: DictionaryKeyPolicy để null, cố ý không set). FE tra fieldErrors['Email'].
    //
    // Test riêng cho chính phép phân biệt hoa-thường, đọc CHUỖI JSON THÔ: test kia so tập khoá sau khi đã giải tuần tự nên
    // nó canh casing một cách tình cờ, và người "sửa cho nhất quán" chỉ cần đổi luôn danh sách kỳ vọng là cổng im lặng tắt.
    // Ở đây cả hai vế cùng bị ghim trong một khẳng định: khoá của envelope là camelCase, khoá của fieldErrors là PascalCase.
    [Fact]
    public async Task Login_FieldErrorKeys_StayPascalCase_WhileTheEnvelopeItselfIsCamelCase()
    {
        using var client = AnonymousClientWithCsrf();

        var response = await client.PostAsync("/api/v1/core/auth/login", JsonBody("""{"tenantCode":"","userName":"","password":""}"""));
        var body = await response.Content.ReadAsStringAsync();

        using var document = JsonDocument.Parse(body);
        var envelopeKeys = document.RootElement.EnumerateObject().Select(p => p.Name).ToList();
        var errorKeys = document.RootElement.GetProperty("error").EnumerateObject().Select(p => p.Name).ToList();
        var fieldErrorKeys = document.RootElement.GetProperty("error").GetProperty("fieldErrors")
            .EnumerateObject().Select(p => p.Name).ToList();

        // Vế 1 — envelope và các field của error: camelCase.
        envelopeKeys.ShouldBe(["success", "data", "error", "traceId"], ignoreOrder: true);
        errorKeys.ShouldContain("fieldErrors");
        errorKeys.ShouldContain("messageParams");

        // Vế 2 — khoá của fieldErrors: PascalCase, đúng tên property C# của LoginRequest.
        fieldErrorKeys.ShouldBe(["TenantCode", "UserName", "Password"], ignoreOrder: true);

        // Vế 3 — phân biệt hoa-thường là ĐIỀU KIỆN, không phải tình cờ: biến thể camelCase của ba khoá không có mặt ở đâu
        // trong thân, kể cả khi ai đó "sửa cho nhất quán" và test vế 2 được cập nhật theo.
        foreach (var camelCase in new[] { "\"tenantCode\"", "\"userName\"", "\"password\"" })
            body.ShouldNotContain(camelCase, Case.Sensitive);
    }

    [Theory]
    [InlineData("""{"name":""}""")]
    [InlineData("""{}""")]
    public async Task CreateRole_AnEmptyOrMissingName_IsRequired_NotFormat(string json)
    {
        using var client = RoleWriter();

        var response = await client.PostAsync("/api/v1/core/roles", JsonBody(json));
        var error = (await ReadEnvelope(response)).Error!;

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        error.Code.ShouldBe("CORE.VALIDATION.FAILED");
        error.FieldErrors!["Name"].Single().Code.ShouldBe("CORE.VALIDATION.REQUIRED");
    }

    // V-02: JSON sai kiểu làm bộ đọc body thất bại -> ngoài khoá "$.name", MVC còn gắn một lỗi cho CHÍNH tham số action
    // (`body`) vì nó thành null. Khoá đó không phải trường DTO nào — không được lọt vào fieldErrors.
    [Fact]
    public async Task CreateRole_AWrongJsonType_IsAGenericValidationFailure_WithNoKeyNamedAfterTheActionParameter()
    {
        using var client = RoleWriter();

        var response = await client.PostAsync("/api/v1/core/roles", JsonBody("""{"name":1}"""));
        var body = await response.Content.ReadAsStringAsync();
        var error = (await ReadEnvelope(response)).Error!;

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        error.Code.ShouldBe("CORE.VALIDATION.FAILED");
        (error.FieldErrors?.Keys ?? []).ShouldNotContain("body");
        (error.FieldErrors?.Keys ?? []).ShouldNotContain("Body");
        body.ShouldNotContain("tools.ietf.org");
    }

    [Fact]
    public async Task CreateRole_AnEmptyBody_IsA400Envelope_NeverA500()
    {
        using var client = RoleWriter();

        var response = await client.PostAsync("/api/v1/core/roles", JsonBody(""));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var error = (await ReadEnvelope(response)).Error!;
        error.Code.ShouldBe("CORE.VALIDATION.FAILED");
        (error.FieldErrors?.Keys ?? []).ShouldNotContain("body");
    }
}
