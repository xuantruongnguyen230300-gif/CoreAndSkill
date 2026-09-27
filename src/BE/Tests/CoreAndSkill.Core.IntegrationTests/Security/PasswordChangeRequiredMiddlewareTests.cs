using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CoreAndSkill.Core.IntegrationTests.Support;
using CoreAndSkill.Core.IntegrationTests.Web;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Security;

// docs/contracts/auth.md §1.2 — tài khoản mang MustChangePassword bị chặn 403 CORE.AUTH.PASSWORD_CHANGE_REQUIRED ở MỌI
// endpoint có danh tính, trừ đúng bốn đường. Test đi qua pipeline HTTP THẬT (định tuyến, xác thực, antiforgery, phân
// quyền, middleware) — nên nó canh cả việc allowlist khớp đúng route mà controller thật sự đăng ký: allowlist lệch khỏi
// route là tài khoản mật khẩu tạm không còn đường thoát, và test "đường được phép" đỏ.
public sealed class PasswordChangeRequiredMiddlewareTests(InMemoryHostFixture fixture) : IClassFixture<InMemoryHostFixture>
{
    private const string PasswordChangeRequired = "CORE.AUTH.PASSWORD_CHANGE_REQUIRED";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    // Token CSRF gắn với TẬP CLAIM lúc phát — cờ phải có mặt TRƯỚC khi lấy token, nếu không request ghi trượt ở lớp
    // antiforgery (CSRF_REJECTED) thay vì tới middleware đang kiểm.
    private async Task<HttpClient> Client(bool mustChangePassword)
    {
        var client = fixture.Host.CreateClient(userId: Guid.NewGuid(), withCsrf: false);
        client.DefaultRequestHeaders.Add("X-Test-MustChangePassword", mustChangePassword ? "True" : "False");

        using var response = await client.GetAsync("/api/v1/core/antiforgery/token");
        response.EnsureSuccessStatusCode();
        var token = (await response.Content.ReadFromJsonAsync<JsonElement>(Json))
            .GetProperty("data").GetProperty("token").GetString()!;
        client.DefaultRequestHeaders.Add("X-XSRF-TOKEN", token);
        return client;
    }

    private static HttpRequestMessage Request(string method, string path)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (method == "POST")
            request.Content = JsonContent.Create(new { }, options: Json);
        return request;
    }

    private static async Task<string?> ErrorCode(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(body))
            return null;

        using var document = JsonDocument.Parse(body);
        return document.RootElement.TryGetProperty("error", out var error) && error.ValueKind == JsonValueKind.Object
            ? error.GetProperty("code").GetString()
            : null;
    }

    // Bốn đường của docs/contracts/auth.md §1.2 — gõ tay theo contract, KHÔNG đọc từ hằng số của code: test phải đỏ khi
    // code lệch khỏi contract, kể cả khi hằng số và allowlist cùng lệch.
    public static TheoryData<string, string> AllowedRoutes => new()
    {
        { "GET", "/api/v1/core/antiforgery/token" },
        { "GET", "/api/v1/core/auth/me" },
        { "POST", "/api/v1/core/auth/change-password-required" },
        { "POST", "/api/v1/core/auth/logout" },
    };

    [Theory]
    [MemberData(nameof(AllowedRoutes))]
    public async Task AllowedRoute_IsNotBlocked(string method, string path)
    {
        using var client = await Client(mustChangePassword: true);

        using var response = await client.SendAsync(Request(method, path));

        (await ErrorCode(response)).ShouldNotBe(PasswordChangeRequired, $"{method} {path} là đường được phép");
        response.StatusCode.ShouldNotBe(HttpStatusCode.Forbidden, $"{method} {path}");
        response.StatusCode.ShouldNotBe(HttpStatusCode.NotFound, $"{method} {path} phải khớp một route thật");
    }

    // Endpoint ẩn danh không bị kiểm — docs/contracts/auth.md §1.2, cùng khuôn lớp antiforgery (đọc [AllowAnonymous] trên
    // METADATA của endpoint, không đọc đường dẫn). Gõ tay theo contract như hai danh sách còn lại. Thân `{}` trượt ở
    // validator (400) nên không chạm DB — thứ test cần chỉ là request đi QUA middleware tới action.
    public static TheoryData<string, string> AnonymousRoutes => new()
    {
        { "POST", "/api/v1/core/auth/login" },
        { "POST", "/api/v1/core/client-errors" },
    };

    [Theory]
    [MemberData(nameof(AnonymousRoutes))]
    public async Task AnonymousRoute_IsNotChecked_EvenWhenTheFlagIsSet(string method, string path)
    {
        using var client = await Client(mustChangePassword: true);

        using var response = await client.SendAsync(Request(method, path));

        (await ErrorCode(response)).ShouldNotBe(PasswordChangeRequired, $"{method} {path} là endpoint ẩn danh");
        response.StatusCode.ShouldNotBe(HttpStatusCode.Forbidden, $"{method} {path}");
        response.StatusCode.ShouldNotBe(HttpStatusCode.NotFound, $"{method} {path} phải khớp một route thật");
    }

    public static TheoryData<string, string> BlockedRoutes => new()
    {
        { "GET", "/api/v1/core/notifications" },
        { "POST", "/api/v1/core/auth/change-password" }, // đường đổi mật khẩu TỰ NGUYỆN — không phải đường thoát
        { "GET", "/api/v1/core/profile" },
    };

    [Theory]
    [MemberData(nameof(BlockedRoutes))]
    public async Task RouteOutsideAllowlist_Returns403PasswordChangeRequired(string method, string path)
    {
        using var client = await Client(mustChangePassword: true);

        using var response = await client.SendAsync(Request(method, path));

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await ErrorCode(response)).ShouldBe(PasswordChangeRequired);
    }

    // Đối chứng: cùng đường, tài khoản KHÔNG mang cờ thì không bị chặn — test trên đỏ vì middleware, không vì lý do khác.
    [Theory]
    [MemberData(nameof(BlockedRoutes))]
    public async Task RouteOutsideAllowlist_WithoutTheFlag_IsNotBlocked(string method, string path)
    {
        using var client = await Client(mustChangePassword: false);

        using var response = await client.SendAsync(Request(method, path));

        (await ErrorCode(response)).ShouldNotBe(PasswordChangeRequired);
    }
}
