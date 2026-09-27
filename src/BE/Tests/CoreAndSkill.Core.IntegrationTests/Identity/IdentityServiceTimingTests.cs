using CoreAndSkill.Core.Application.Auth;
using CoreAndSkill.Core.Application.Configuration;
using CoreAndSkill.Core.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Identity;

// F1 — chống dò tài khoản bằng thời gian phản hồi (docs/wiki-core/be/09-security-beyond-auth.md bảng "Đường rò", hàng
// "Thời gian phản hồi"). Mọi nhánh trả INVALID_CREDENTIALS phải trả giá ĐÚNG MỘT phép kiểm băm — kể cả khi không có
// tài khoản nào để kiểm. Đếm lời gọi qua một hasher bọc hasher thật thay vì đo thời gian: đo thời gian thật chập chờn.
public sealed class IdentityServiceTimingTests
{
    private const string Password = "Passw0rd-Test1";

    private readonly CountingPasswordHasher _hasher = new();
    private readonly IUserPasswordStore<AppUser> _store =
        Substitute.For<IUserPasswordStore<AppUser>, IUserLockoutStore<AppUser>, IUserSecurityStampStore<AppUser>>();

    private IdentityService Build()
    {
        var users = new UserManager<AppUser>(
            _store, Options.Create(new IdentityOptions()), _hasher,
            [], [], new UpperInvariantLookupNormalizer(), new IdentityErrorDescriber(),
            null!, NullLogger<UserManager<AppUser>>.Instance);

        return new IdentityService(users, new PasswordRehashScope());
    }

    private AppUser ExistingUser()
    {
        var user = new AppUser { UserName = "quantri", NormalizedUserName = "QUANTRI" };
        user.ChangeFullName("Quản trị");
        _store.FindByNameAsync("QUANTRI", Arg.Any<CancellationToken>()).Returns(user);
        _store.GetPasswordHashAsync(user, Arg.Any<CancellationToken>())
            .Returns(new PasswordHasher<AppUser>().HashPassword(user, Password));
        ((IUserSecurityStampStore<AppUser>)_store).GetSecurityStampAsync(user, Arg.Any<CancellationToken>()).Returns("stamp");
        _store.UpdateAsync(user, Arg.Any<CancellationToken>()).Returns(IdentityResult.Success);
        return user;
    }

    [Fact]
    public async Task UnknownUser_RunsExactlyOnePasswordVerification()
    {
        _store.FindByNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((AppUser?)null);
        var identity = Build();

        var result = await identity.CheckCredentialsAsync("khong-co", Password, CancellationToken.None);

        result.Error!.Code.ShouldBe(AuthErrors.InvalidCredentials.Code);
        _hasher.VerifyCalls.ShouldBe(1);
    }

    [Fact]
    public async Task SimulatedCheck_RunsExactlyOnePasswordVerification()
    {
        var identity = Build();

        await identity.SimulateCredentialCheckAsync(Password, CancellationToken.None);

        _hasher.VerifyCalls.ShouldBe(1);
    }

    // Đối chứng: tài khoản có thật — sai hay đúng mật khẩu — cũng đúng MỘT phép kiểm. Nếu hai test trên xanh vì phép
    // đếm hỏng thì các ca này đỏ.
    [Theory]
    [InlineData(Password)]
    [InlineData("Sai-mat-khau-1")]
    public async Task ExistingUser_RunsExactlyOnePasswordVerification(string password)
    {
        ExistingUser();
        var identity = Build();

        await identity.CheckCredentialsAsync("quantri", password, CancellationToken.None);

        _hasher.VerifyCalls.ShouldBe(1);
    }

    // Phép giả phải trả giá THẬT: hash giả do chính hasher đang dùng dựng ra (cùng thuật toán, cùng số vòng lặp), không
    // phải một chuỗi rác mà hasher từ chối ngay ở bước đọc định dạng.
    [Fact]
    public async Task SimulatedCheck_VerifiesAgainstAHashTheRealHasherCanParse()
    {
        var identity = Build();

        await identity.SimulateCredentialCheckAsync(Password, CancellationToken.None);

        _hasher.LastVerifiedHash.ShouldNotBeNull();
        var parsed = Convert.FromBase64String(_hasher.LastVerifiedHash);
        parsed[0].ShouldBe((byte)0x01, "định dạng V3 của PasswordHasher — PBKDF2 đủ số vòng lặp");
    }

    private sealed class CountingPasswordHasher : IPasswordHasher<AppUser>
    {
        private readonly PasswordHasher<AppUser> _inner = new();

        public int VerifyCalls { get; private set; }

        public string? LastVerifiedHash { get; private set; }

        public string HashPassword(AppUser user, string password) => _inner.HashPassword(user, password);

        public PasswordVerificationResult VerifyHashedPassword(AppUser user, string hashedPassword, string providedPassword)
        {
            VerifyCalls++;
            LastVerifiedHash = hashedPassword;
            return _inner.VerifyHashedPassword(user, hashedPassword, providedPassword);
        }
    }
}
