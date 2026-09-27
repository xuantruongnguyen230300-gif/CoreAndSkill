using System.Collections.Concurrent;
using CoreAndSkill.Core.Application.Tenants;
using CoreAndSkill.Core.Infrastructure.Identity;
using CoreAndSkill.Core.IntegrationTests.Support;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Tenants;

// CHƯA CHẠY THỬ TRONG MÔI TRƯỜNG NÀY — không có Docker daemon lúc viết, nên các test dưới đây CHƯA
// TỪNG được thực thi một lần nào: chúng biên dịch được, chỉ vậy thôi. Đừng đọc sự tồn tại của chúng
// là bằng chứng chúng xanh. CI là nơi chạy chúng lần ĐẦU TIÊN.
// Gỡ khối chú thích này ngay khi có người chạy thật và thấy kết quả.
//
// docs/contracts/tenants.md §6 "Ghi chú": chính sách mật khẩu kiểm TRƯỚC khi gọi UserManager, nên
// fieldErrors["TempPassword"] không nói gì về tên đăng nhập. Bằng chứng thứ tự: một PasswordValidator ghi lại tài khoản
// nó được hỏi — lần hỏi ĐẦU TIÊN phải là tài khoản "thăm dò" (chưa mang tên đăng nhập nào), không phải tài khoản sắp tạo.
[Trait("Category", "RequiresDocker")]
[Collection(PostgresCollection.Name)]
public sealed class TenantAdminPasswordPolicyOrderDatabaseTests(PostgresFixture db) : IAsyncLifetime
{
    private readonly RecordingPasswordValidator _validator = new();
    private B4DockerHost _host = null!;

    public Task InitializeAsync()
    {
        _host = new B4DockerHost(db.ConnectionString,
            configure: services => services.AddSingleton<IPasswordValidator<AppUser>>(_validator));
        return db.ResetAsync();
    }

    public async Task DisposeAsync() => await _host.DisposeAsync();

    [Fact]
    public async Task CreateAdditionalAdmin_WeakPassword_IsRejectedByThePolicyCheck_BeforeUserManager()
    {
        var tenant = await _host.CreateTenantAsync("DV-PW");
        _validator.SeenUserNames.Clear();

        using var scope = _host.Services.CreateScope();
        var result = await ProvisioningInTransaction.RunAsync(scope.ServiceProvider, (provisioning, ct) =>
            provisioning.CreateAdditionalAdminAsync(tenant.Id, "admin", "admin2@dv-pw.example.com", "Quản trị 2", "yeu", ct));

        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe(TenantProvisioningErrors.AdminCreateFailed.Code);
        result.Error.FieldErrors.Keys.ShouldBe(["TempPassword"]);

        _validator.SeenUserNames.ShouldNotBeEmpty("chống test rỗng: validator thử phải được hỏi");
        _validator.SeenUserNames.ShouldAllBe(name => name == null,
            "mật khẩu yếu phải bị chặn ở phép kiểm chính sách trên tài khoản thăm dò — UserManager.CreateAsync không được chạy");
    }

    private sealed class RecordingPasswordValidator : IPasswordValidator<AppUser>
    {
        public ConcurrentQueue<string?> SeenUserNames { get; } = new();

        public Task<IdentityResult> ValidateAsync(UserManager<AppUser> manager, AppUser user, string? password)
        {
            SeenUserNames.Enqueue(user.UserName);
            return Task.FromResult(IdentityResult.Success);
        }
    }
}
