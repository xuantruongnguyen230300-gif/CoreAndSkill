using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CoreAndSkill.Core.Application.Common;
using CoreAndSkill.Core.Application.Identity;
using CoreAndSkill.Core.Application.Permissions;
using CoreAndSkill.Core.Application.Users;
using CoreAndSkill.Core.Domain.Common;
using CoreAndSkill.Core.IntegrationTests.Support;
using CoreAndSkill.Core.Web.Http;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Users;

// POST /api/v1/core/users với userName là danh tính hệ thống (SystemActor) — hình dạng trên dây mà FE nhận: 400
// CORE.VALIDATION.FAILED, fieldErrors["UserName"] đúng một mục mã CORE.USER.USERNAME_RESERVED, không messageParams; không tài
// khoản nào được tạo. Qua HTTP thật trên host không database (InMemoryCoreHost), cùng khuôn CreateUserDuplicateRoleIdsEndpointTests.
public sealed class CreateUserReservedUserNameEndpointTests : IAsyncLifetime
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly Guid _callerId = Guid.NewGuid();
    private readonly IUserAdminService _userAdmin = Substitute.For<IUserAdminService>();
    private readonly InMemoryCoreHost _host;

    public CreateUserReservedUserNameEndpointTests()
    {
        _userAdmin.CreateAsync(Arg.Any<CreateUserInput>(), Arg.Any<CancellationToken>()).Returns(Result.Success(Guid.NewGuid()));

        _host = new InMemoryCoreHost(configure: services =>
        {
            services.AddSingleton(Substitute.For<IUserLookupService>());
            services.AddSingleton(_userAdmin);
        });
    }

    public Task InitializeAsync()
    {
        _host.Permissions.Grant(_callerId, CorePermissions.UserWrite);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _host.DisposeAsync();

    [Theory]
    [InlineData("system")]
    [InlineData("System")]
    [InlineData(" SYSTEM ")]
    public async Task ReservedUserName_Returns400_WithUsernameReservedOnUserName_AndCreatesNoAccount(string userName)
    {
        using var client = _host.CreateClient(_callerId);

        var response = await client.PostAsJsonAsync("/api/v1/core/users", Body(userName));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest, await response.Content.ReadAsStringAsync());
        var error = (await ReadEnvelope(response)).Error.ShouldNotBeNull();
        error.Code.ShouldBe(CommonErrors.ValidationFailed.Code);
        var fieldErrors = error.FieldErrors.ShouldNotBeNull();
        fieldErrors.Keys.ShouldBe(["UserName"]);
        var fieldError = fieldErrors["UserName"].ShouldHaveSingleItem();
        fieldError.Code.ShouldBe(UserErrors.UsernameReserved.Code);
        (fieldError.MessageParams ?? new Dictionary<string, string>()).ShouldBeEmpty();
        await _userAdmin.DidNotReceiveWithAnyArgs().CreateAsync(default!, default);
    }

    // Đối chứng: cùng host, cùng người gọi — tên thường đi tới bước tạo tài khoản. Thiếu ca này thì ca trên xanh cả khi host
    // không bao giờ đi tới được bước tạo.
    [Fact]
    public async Task OrdinaryUserName_CreatesTheAccount()
    {
        using var client = _host.CreateClient(_callerId);

        var response = await client.PostAsJsonAsync("/api/v1/core/users", Body("system1"));

        response.StatusCode.ShouldBe(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        await _userAdmin.ReceivedWithAnyArgs(1).CreateAsync(default!, default);
    }

    private static object Body(string userName) => new
    {
        userName,
        email = "binh@vd.vn",
        fullName = "Trần Văn Bình",
        tempPassword = "MatKhauTam!2026",
        roleIds = Array.Empty<Guid>(),
    };

    private static async Task<ApiEnvelope<JsonElement>> ReadEnvelope(HttpResponseMessage response)
        => JsonSerializer.Deserialize<ApiEnvelope<JsonElement>>(await response.Content.ReadAsStringAsync(), Json)!;
}
