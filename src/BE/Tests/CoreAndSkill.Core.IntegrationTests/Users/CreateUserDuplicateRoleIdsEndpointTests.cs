using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CoreAndSkill.Core.Application.Identity;
using CoreAndSkill.Core.Application.Permissions;
using CoreAndSkill.Core.Application.Roles;
using CoreAndSkill.Core.Application.Users;
using CoreAndSkill.Core.Domain.Common;
using CoreAndSkill.Core.IntegrationTests.Support;
using CoreAndSkill.Core.Web.Http;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Users;

// POST /api/v1/core/users với roleIds trùng — 400 CORE.USER.DUPLICATE_ROLE_ENTRY, như PUT /users/{id}/roles (docs/contracts/
// users.md §5, §7), và không tài khoản nào được tạo. Qua HTTP thật trên host không database (InMemoryCoreHost): mọi seam chạm
// database mà handler dùng được thay bằng bản giả trả lời "mọi thứ hợp lệ", nên nếu handler lọc trùng lặng lẽ thay vì chặn, ca
// này đi hết đường và tạo tài khoản (201) — đó là ca đỏ trên bản cũ.
public sealed class CreateUserDuplicateRoleIdsEndpointTests : IAsyncLifetime
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly Guid _callerId = Guid.NewGuid();
    private readonly Guid _roleId = Guid.NewGuid();
    private readonly IUserAdminService _userAdmin = Substitute.For<IUserAdminService>();
    private readonly InMemoryCoreHost _host;

    public CreateUserDuplicateRoleIdsEndpointTests()
    {
        var lookup = Substitute.For<IUserLookupService>();
        var roles = Substitute.For<IRoleQueryService>();
        roles.FindExistingIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(ci => (IReadOnlySet<Guid>)ci.ArgAt<IReadOnlyCollection<Guid>>(0).ToHashSet());
        _userAdmin.CreateAsync(Arg.Any<CreateUserInput>(), Arg.Any<CancellationToken>()).Returns(Result.Success(Guid.NewGuid()));
        _userAdmin.GrantInitialRolesAsync(Arg.Any<Guid>(), Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        _host = new InMemoryCoreHost(configure: services =>
        {
            services.AddSingleton(lookup);
            services.AddSingleton(roles);
            services.AddSingleton(_userAdmin);
        });
    }

    public Task InitializeAsync()
    {
        _host.Permissions.Grant(_callerId, CorePermissions.UserWrite);
        _host.Permissions.Grant(_callerId, CorePermissions.UserRoleAssign);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _host.DisposeAsync();

    [Fact]
    public async Task DuplicateRoleIds_Return400DuplicateRoleEntry_AndCreateNoAccount()
    {
        using var client = _host.CreateClient(_callerId);

        var response = await client.PostAsJsonAsync("/api/v1/core/users", Body([_roleId, _roleId]));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest, await response.Content.ReadAsStringAsync());
        (await ReadEnvelope(response)).Error!.Code.ShouldBe(UserErrors.DuplicateRoleEntry.Code);
        await _userAdmin.DidNotReceiveWithAnyArgs().CreateAsync(default!, default);
        await _userAdmin.DidNotReceiveWithAnyArgs().GrantInitialRolesAsync(default, default!, default);
    }

    // Đối chứng: cùng host, cùng người gọi, cùng vai trò — khác đúng việc vai trò chỉ xuất hiện một lần. Thiếu ca này thì ca
    // trên xanh cả khi host không bao giờ đi tới được bước tạo tài khoản.
    [Fact]
    public async Task SameRoleOnce_CreatesTheAccount()
    {
        using var client = _host.CreateClient(_callerId);

        var response = await client.PostAsJsonAsync("/api/v1/core/users", Body([_roleId]));

        response.StatusCode.ShouldBe(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        await _userAdmin.ReceivedWithAnyArgs(1).CreateAsync(default!, default);
    }

    private static object Body(Guid[] roleIds) => new
    {
        userName = "binh.tv",
        email = "binh@vd.vn",
        fullName = "Trần Văn Bình",
        tempPassword = "MatKhauTam!2026",
        roleIds,
    };

    private static async Task<ApiEnvelope<JsonElement>> ReadEnvelope(HttpResponseMessage response)
        => JsonSerializer.Deserialize<ApiEnvelope<JsonElement>>(await response.Content.ReadAsStringAsync(), Json)!;
}
