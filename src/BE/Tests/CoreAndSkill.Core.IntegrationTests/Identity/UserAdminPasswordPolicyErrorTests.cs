using CoreAndSkill.Core.Application.Auth;
using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Application.Identity;
using CoreAndSkill.Core.Application.Users;
using CoreAndSkill.Core.Infrastructure.Identity;
using CoreAndSkill.Core.Infrastructure.Persistence;
using CoreAndSkill.Core.IntegrationTests.Support;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Identity;

// Ánh xạ mã Identity đi theo MÃ, không theo endpoint (docs/contracts/auth.md §6 "Ghi chú"): cùng một lỗi chính sách mật
// khẩu phải ra cùng mã catalog VÀ cùng tham số ở mọi đường — đổi mật khẩu, tạo người dùng, tạo quản trị đơn vị. Thiếu
// MinLength thì FE không dựng được câu "phải có ít nhất {MinLength} ký tự".
//
// Không cần database: chính sách mật khẩu chạy TRƯỚC mọi lệnh ghi của UserManager.CreateAsync, nên kết nối không bao giờ
// được mở (OfflineCoreDbContext chặn nếu có).
public sealed class UserAdminPasswordPolicyErrorTests
{
    private const int RequiredLength = 12;
    private const int RequiredUniqueChars = 5;

    [Fact]
    public async Task CreateUser_ShortPassword_FieldErrorsCarryTheEnforcedPolicyValues()
    {
        await using var db = OfflineCoreDbContext.Create(Guid.NewGuid());
        var service = new UserAdminService(BuildUserManager(db), db, TimeProvider.System, Substitute.For<ICurrentUser>());

        var result = await service.CreateAsync(
            new CreateUserInput("binh.tv", "binh@vd.vn", "Trần Văn Bình", "Ab1!"), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe(UserErrors.CreateFailed.Code);

        var errors = result.Error.FieldErrors["TempPassword"];

        var tooShort = errors.Single(e => e.Code == AuthErrors.PasswordTooShort.Code);
        tooShort.Params.ShouldContainKeyAndValue("MinLength", RequiredLength.ToString(System.Globalization.CultureInfo.InvariantCulture));

        var uniqueChars = errors.Single(e => e.Code == AuthErrors.PasswordRequiresUniqueChars.Code);
        uniqueChars.Params.ShouldContainKeyAndValue(
            "MinUniqueChars", RequiredUniqueChars.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    private static UserManager<AppUser> BuildUserManager(CoreDbContext db)
    {
        var store = new UserStore<AppUser, AppRole, CoreDbContext, Guid, AppUserClaim, AppUserRole, AppUserLogin, AppUserToken, AppRoleClaim>(db);
        var options = new IdentityOptions();
        options.Password.RequiredLength = RequiredLength;
        options.Password.RequiredUniqueChars = RequiredUniqueChars;

        // Không UserValidator: bộ kiểm trùng tên đăng nhập tra database. Chính sách mật khẩu vẫn qua PasswordValidator.
        return new UserManager<AppUser>(
            store, Options.Create(options), new PasswordHasher<AppUser>(),
            [], [new PasswordValidator<AppUser>()], new UpperInvariantLookupNormalizer(), new IdentityErrorDescriber(),
            null!, NullLogger<UserManager<AppUser>>.Instance);
    }
}
