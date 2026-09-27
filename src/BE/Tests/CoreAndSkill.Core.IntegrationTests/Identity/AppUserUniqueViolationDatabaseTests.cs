using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CoreAndSkill.Core.Application.Tenants;
using CoreAndSkill.Core.Application.Users;
using CoreAndSkill.Core.IntegrationTests.Support;
using CoreAndSkill.Core.Web.Http;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Identity;

// CHƯA CHẠY THỬ TRONG MÔI TRƯỜNG NÀY — không có Docker daemon lúc viết, nên các test dưới đây CHƯA
// TỪNG được thực thi một lần nào: chúng biên dịch được, chỉ vậy thôi. Đừng đọc sự tồn tại của chúng
// là bằng chứng chúng xanh. CI là nơi chạy chúng lần ĐẦU TIÊN.
// Gỡ khối chú thích này ngay khi có người chạy thật và thấy kết quả.
//
// docs/adr/0089-trung-unique-khi-ghi-dong-thoi-dich-o-kho-identity.md trên PostgreSQL THẬT — mặt mà AppUserUniqueViolationTests
// (không database) không chứng minh được: 23505 thật của Npgsql, qua lớp cha UserStore của bản Identity đang dùng, tới được
// AppUserStore và ra đúng hình dạng lỗi; lượt thua không để lại dòng nào. ADR-0089 "Tiêu cực": nâng bản Identity mà lớp cha
// đổi cách lưu thì chỉ test ở đây bắt được.
//
// Ép hai lượt CHỒNG nhau thay vì mong chúng tình cờ chồng: "lượt kia" là một kết nối Npgsql riêng đã chèn bản trùng mà CHƯA
// commit (HeldTransaction). Request thật lọt phép kiểm trước và bộ kiểm Identity (READ COMMITTED — không thấy dòng chưa
// commit), rồi lệnh ghi của nó phải CHỜ trên index duy nhất; test đợi tới khi thấy nó chờ rồi mới commit phía kia ⇒ 23505.
//
// Cần Docker daemon — dựng PostgreSQL thật qua Testcontainers (PostgresFixture). Máy không có Docker: loại nhóm này bằng
// `--filter "Category!=RequiresDocker"`. Xem docs/wiki-core/be/04-testing-strategy.md §4.2.
[Trait("Category", "RequiresDocker")]
[Collection(PostgresCollection.Name)]
public sealed class AppUserUniqueViolationDatabaseTests(PostgresFixture db) : IAsyncLifetime
{
    private const string UsersUrl = "/api/v1/core/users";
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
    });

    public Task InitializeAsync() => db.ResetAsync();

    public async Task DisposeAsync() => await _host.DisposeAsync();

    // POST /users — users.md §5 "Hai lượt tạo cùng userName hoặc cùng email chạy đồng thời".
    [Theory]
    [InlineData("UserName")]
    [InlineData("Email")]
    public async Task Create_WhileAnotherCreateOfTheSameValueCommits_Is422CreateFailed_WithTheFieldError_AndWritesNothing(string field)
    {
        var tenant = await _host.CreateTenantAsync("DV-TRUNG");
        using var client = Client(tenant);
        var createAuditRowsBefore = await UserCreateAuditRowsAsync(tenant.Id);

        // Lượt kia giữ ĐÚNG giá trị trùng ở ô đang xét, giá trị KHÁC ở ô còn lại — chỉ một index vỡ.
        var sameUserName = field == "UserName";
        await using var other = await HeldTransaction.OpenAsync(db.ConnectionString);
        await InsertAccountAsync(
            other, tenant.Id,
            userName: sameUserName ? "binh.tv" : "nguoi.kia",
            email: sameUserName ? "nguoi.kia@vd.vn" : "binh@vd.vn");

        var request = client.PostAsJsonAsync(UsersUrl, new
        {
            userName = "binh.tv",
            email = "binh@vd.vn",
            fullName = "Trần Văn Bình",
            tempPassword = B4DockerHost.Password,
            roleIds = Array.Empty<Guid>(),
        });

        await LockContention.WaitUntilSomeSessionWaitsOnALockAsync(db, request);
        await other.CommitAsync();
        var response = await request;

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity, await response.Content.ReadAsStringAsync());
        var error = (await ReadEnvelope(response)).Error.ShouldNotBeNull();
        error.Code.ShouldBe(UserErrors.CreateFailed.Code);
        var fieldError = error.FieldErrors.ShouldNotBeNull()[field].ShouldHaveSingleItem();
        fieldError.Code.ShouldBe(sameUserName ? UserErrors.UsernameDuplicated.Code : UserErrors.EmailDuplicated.Code);
        fieldError.MessageParams.ShouldNotBeNull().ShouldContainKeyAndValue(field, sameUserName ? "binh.tv" : "binh@vd.vn");

        // Lượt thua không ghi gì: chỉ còn dòng của lượt kia, và không thêm dòng nhật ký tạo tài khoản nào.
        (await db.CountAsync(
            "SELECT count(*) FROM core.app_user WHERE tenant_id = @t AND (normalized_user_name = 'BINH.TV' OR normalized_email = 'BINH@VD.VN')",
            new NpgsqlParameter("t", tenant.Id))).ShouldBe(1);
        (await UserCreateAuditRowsAsync(tenant.Id)).ShouldBe(createAuditRowsBefore);
    }

    // PUT /users/{id} — users.md §6 "Ghi chú — sửa email đồng thời". Email trong tham số là email MỚI vừa bị từ chối.
    [Fact]
    public async Task Update_WhileAnotherAccountTakesTheSameEmailAndCommits_Is422UpdateFailed_WithTheNewEmail_AndChangesNothing()
    {
        var tenant = await _host.CreateTenantAsync("DV-TRUNG");
        using var client = Client(tenant);
        var userId = await CreateUserAsync(client);
        var version = await GetVersionAsync(client, userId);

        await using var other = await HeldTransaction.OpenAsync(db.ConnectionString);
        await InsertAccountAsync(other, tenant.Id, userName: "nguoi.kia", email: "email.moi@vd.vn");

        var request = client.PutAsJsonAsync($"{UsersUrl}/{userId}", new { email = "email.moi@vd.vn", fullName = "Nguyễn Văn An", version });

        await LockContention.WaitUntilSomeSessionWaitsOnALockAsync(db, request);
        await other.CommitAsync();
        var response = await request;

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity, await response.Content.ReadAsStringAsync());
        var error = (await ReadEnvelope(response)).Error.ShouldNotBeNull();
        error.Code.ShouldBe(UserErrors.UpdateFailed.Code);
        var fieldError = error.FieldErrors.ShouldNotBeNull()["Email"].ShouldHaveSingleItem();
        fieldError.Code.ShouldBe(UserErrors.EmailDuplicated.Code);
        fieldError.MessageParams.ShouldNotBeNull().ShouldContainKeyAndValue("Email", "email.moi@vd.vn");

        (await db.QueryAsync("SELECT email FROM core.app_user WHERE id = @id", r => r.GetString(0), new NpgsqlParameter("id", userId)))
            .ShouldHaveSingleItem().ShouldBe("an@vd.vn");
        (await GetVersionAsync(client, userId)).ShouldBe(version, "lượt thua không đổi token của tài khoản");
    }

    // tenants.md §6 — cùng lỗi Identity, nhưng bộ ánh xạ riêng của endpoint gộp MỌI ca thành một mã, không nêu tên đăng nhập.
    // Chạy thẳng service trong transaction (cùng đường CoreCommandRunner / TransactionBehavior), không qua HTTP.
    [Fact]
    public async Task CreateAdditionalAdmin_WhileAnotherCreateOfTheSameUserNameCommits_IsAdminCreateFailed_WithoutNamingTheUserName()
    {
        var tenant = await _host.CreateTenantAsync("DV-TRUNG");

        await using var other = await HeldTransaction.OpenAsync(db.ConnectionString);
        await InsertAccountAsync(other, tenant.Id, userName: "quantri2", email: "nguoi.kia@vd.vn");

        using var scope = _host.Services.CreateScope();
        var request = ProvisioningInTransaction.RunAsync(scope.ServiceProvider, (provisioning, ct) =>
            provisioning.CreateAdditionalAdminAsync(tenant.Id, "quantri2", "quantri2@vd.vn", "Quản trị 2", B4DockerHost.Password, ct));

        await WaitUntilSomeSessionWaitsOnALockAsync(request);
        await other.CommitAsync();
        var result = await request;

        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe(TenantProvisioningErrors.AdminCreateFailed.Code);
        result.Error.FieldErrors.ShouldBeEmpty("tenants.md §6: không lý do nào liên quan userName được lộ");
        (await db.CountAsync(
            "SELECT count(*) FROM core.app_user WHERE tenant_id = @t AND normalized_user_name = 'QUANTRI2'",
            new NpgsqlParameter("t", tenant.Id))).ShouldBe(1);
    }

    // ---- Hạ tầng của test --------------------------------------------------------------------

    // Một tài khoản do "lượt kia" chèn, đủ cột NOT NULL của core.app_user (database/scripts/core/0001__core__initial.sql).
    // Giá trị chuẩn hoá theo đúng cách Identity chuẩn hoá (UpperInvariantLookupNormalizer) — index so trên hai cột này.
    private static Task InsertAccountAsync(HeldTransaction other, Guid tenantId, string userName, string email)
        => other.ExecuteOneRowAsync(
            """
            INSERT INTO core.app_user (id, tenant_id, full_name, user_name, normalized_user_name, email, normalized_email,
                email_confirmed, security_stamp, concurrency_stamp, phone_number_confirmed, two_factor_enabled,
                lockout_enabled, access_failed_count)
            VALUES (@id, @tenant, 'Người kia', @userName, @normalizedUserName, @email, @normalizedEmail,
                false, @stamp, @stamp, false, false, true, 0)
            """,
            new NpgsqlParameter("id", Guid.NewGuid()),
            new NpgsqlParameter("tenant", tenantId),
            new NpgsqlParameter("userName", userName),
            new NpgsqlParameter("normalizedUserName", userName.Normalize().ToUpperInvariant()),
            new NpgsqlParameter("email", email),
            new NpgsqlParameter("normalizedEmail", email.Normalize().ToUpperInvariant()),
            new NpgsqlParameter("stamp", Guid.NewGuid().ToString()));

    private Task<long> UserCreateAuditRowsAsync(Guid tenantId)
        => db.CountAsync(
            "SELECT count(*) FROM core.audit_log WHERE tenant_id = @t AND action_code = 'core.user.create'",
            new NpgsqlParameter("t", tenantId));

    // Như LockContention.WaitUntilSomeSessionWaitsOnALockAsync, cho một lời gọi service thay vì một request HTTP.
    private async Task WaitUntilSomeSessionWaitsOnALockAsync(Task operation)
    {
        var deadline = DateTime.UtcNow + LockContention.ProbeLimit;
        while (DateTime.UtcNow < deadline)
        {
            if (operation.IsCompleted)
                throw new ShouldAssertException("chống test rỗng: lời gọi xong trước khi chờ khoá.");

            var waiting = await db.CountAsync(
                "SELECT count(*) FROM pg_stat_activity WHERE datname = current_database() AND wait_event_type = 'Lock'");
            if (waiting > 0)
                return;

            await Task.Delay(TimeSpan.FromMilliseconds(50));
        }

        throw new ShouldAssertException($"chống test rỗng: sau {LockContention.ProbeLimit.TotalSeconds} giây không phiên nào chờ khoá.");
    }

    private static async Task<Guid> CreateUserAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync(UsersUrl, new
        {
            userName = "an.nv",
            email = "an@vd.vn",
            fullName = "Nguyễn Văn An",
            tempPassword = B4DockerHost.Password,
            roleIds = Array.Empty<Guid>(),
        });
        response.StatusCode.ShouldBe(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        return (await ReadEnvelope(response)).Data.GetGuid();
    }

    private static async Task<string> GetVersionAsync(HttpClient client, Guid userId)
    {
        var response = await client.GetAsync($"{UsersUrl}/{userId}");
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return (await ReadEnvelope(response)).Data.GetProperty("version").GetString()!;
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

    private static async Task<ApiEnvelope<JsonElement>> ReadEnvelope(HttpResponseMessage response)
        => JsonSerializer.Deserialize<ApiEnvelope<JsonElement>>(await response.Content.ReadAsStringAsync(), Json)!;
}
