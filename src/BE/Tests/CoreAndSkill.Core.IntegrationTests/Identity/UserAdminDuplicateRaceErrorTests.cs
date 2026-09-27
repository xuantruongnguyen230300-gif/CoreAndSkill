using System.Data;
using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Application.Identity;
using CoreAndSkill.Core.Application.Users;
using CoreAndSkill.Core.Domain.Common;
using CoreAndSkill.Core.Infrastructure.Identity;
using CoreAndSkill.Core.Infrastructure.Persistence;
using CoreAndSkill.Core.IntegrationTests.Support;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Identity;

// Nhánh tranh chấp của docs/contracts/users.md §5, §6: hai người tạo (hoặc sửa) cùng tên đăng nhập / cùng email một lúc.
// Cả hai lọt qua phép kiểm trước (handler cho tạo, câu EXISTS của UserAdminService.UpdateAsync cho sửa), rồi bộ kiểm của
// chính Identity thấy dòng người kia vừa commit ⇒ CREATE_FAILED / UPDATE_FAILED, lý do ở fieldErrors. Mã trong fieldErrors
// là USERNAME_DUPLICATED / EMAIL_DUPLICATED — câu dịch của hai mã đó cần {{UserName}} / {{Email}}, nên tham số PHẢI có mặt
// với đúng khoá của card; thiếu thì người dùng thấy nguyên chữ "{{UserName}}".
//
// RaceUserValidator đứng thay UserValidator của Identity ở đúng khoảnh khắc đó: trả chính lỗi mà IdentityErrorDescriber
// dựng khi thấy trùng. Không database: bộ kiểm người dùng chạy TRƯỚC mọi lệnh ghi của UserManager.
public sealed class UserAdminDuplicateRaceErrorTests
{
    private const string StrongPassword = "Temp-Passw0rd-9";

    private readonly Guid _tenantId = Guid.NewGuid();

    public static TheoryData<string, string, string, string> CreateRaces => new()
    {
        { "DuplicateUserName", "UserName", UserErrors.UsernameDuplicated.Code, "binh.tv" },
        { "DuplicateEmail", "Email", UserErrors.EmailDuplicated.Code, "binh@vd.vn" },
    };

    [Theory]
    [MemberData(nameof(CreateRaces))]
    public async Task Create_IdentityFindsDuplicate_FieldErrorCarriesTheCardParam(
        string identityCode, string field, string expectedCode, string expectedValue)
    {
        await using var db = OfflineCoreDbContext.Create(_tenantId);
        var manager = BuildUserManager(db, new RaceUserValidator(identityCode));
        var service = new UserAdminService(manager, db, TimeProvider.System, Substitute.For<ICurrentUser>());

        var result = await service.CreateAsync(
            new CreateUserInput("binh.tv", "binh@vd.vn", "Trần Văn Bình", StrongPassword), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe(UserErrors.CreateFailed.Code);
        var fieldError = result.Error.FieldErrors[field].Single();
        fieldError.Code.ShouldBe(expectedCode);
        fieldError.Params.ShouldContainKeyAndValue(field, expectedValue);
    }

    // Sửa: email mang trong tham số là email MỚI người gọi vừa gõ (cái bị từ chối), không phải email cũ của tài khoản.
    [Fact]
    public async Task Update_IdentityFindsDuplicateEmail_FieldErrorCarriesTheNewEmail()
    {
        var user = new AppUser
        {
            TenantId = _tenantId,
            UserName = "an.nguyen",
            NormalizedUserName = "AN.NGUYEN",
            Email = "an@vd.vn",
            NormalizedEmail = "AN@VD.VN",
            SecurityStamp = Guid.NewGuid().ToString("N"),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
        };

        // Câu EXISTS kiểm trùng email của UpdateAsync: người kia CHƯA commit ⇒ "không trùng".
        var notTakenYet = new DataTable();
        notTakenYet.Columns.Add("exists", typeof(bool));
        notTakenYet.Rows.Add(false);

        await using var db = OfflineCoreDbContext.Create(_tenantId, [.. new SqlRecorder().Interceptors, new ScriptedReader(notTakenYet)]);
        db.Attach(user);
        var manager = BuildUserManager(db, new RaceUserValidator("DuplicateEmail"), tracked: user);
        var service = new UserAdminService(manager, db, TimeProvider.System, Substitute.For<ICurrentUser>());

        var result = await service.UpdateAsync(
            user.Id, new UpdateUserInput("an.moi@vd.vn", "Nguyễn Văn An", user.ConcurrencyStamp), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe(UserErrors.UpdateFailed.Code);
        var fieldError = result.Error.FieldErrors["Email"].Single();
        fieldError.Code.ShouldBe(UserErrors.EmailDuplicated.Code);
        fieldError.Params.ShouldContainKeyAndValue("Email", "an.moi@vd.vn");
    }

    private static UserManager<AppUser> BuildUserManager(CoreDbContext db, IUserValidator<AppUser> validator, AppUser? tracked = null)
    {
        var store = new UserStore<AppUser, AppRole, CoreDbContext, Guid, AppUserClaim, AppUserRole, AppUserLogin, AppUserToken, AppRoleClaim>(db);
        return new TrackedLookupUserManager(store, validator, tracked);
    }

    // Identity đã thấy dòng của người kia: trả đúng lỗi IdentityErrorDescriber dựng cho ca trùng — cùng Code với UserValidator thật.
    private sealed class RaceUserValidator(string identityCode) : IUserValidator<AppUser>
    {
        private static readonly IdentityErrorDescriber Describer = new();

        public Task<IdentityResult> ValidateAsync(UserManager<AppUser> manager, AppUser user) => Task.FromResult(identityCode switch
        {
            "DuplicateUserName" => IdentityResult.Failed(Describer.DuplicateUserName(user.UserName!)),
            "DuplicateEmail" => IdentityResult.Failed(Describer.DuplicateEmail(user.Email!)),
            _ => throw new ArgumentOutOfRangeException(nameof(identityCode), identityCode, null),
        });
    }

    // FindByIdAsync trỏ về bản ghi đang theo dõi — không database.
    private sealed class TrackedLookupUserManager(IUserStore<AppUser> store, IUserValidator<AppUser> validator, AppUser? tracked)
        : UserManager<AppUser>(store, Microsoft.Extensions.Options.Options.Create(new IdentityOptions()), new PasswordHasher<AppUser>(),
            [validator], [new PasswordValidator<AppUser>()], new UpperInvariantLookupNormalizer(), new IdentityErrorDescriber(),
            null!, NullLogger<UserManager<AppUser>>.Instance)
    {
        public override Task<AppUser?> FindByIdAsync(string userId)
            => Task.FromResult(tracked is not null && userId == tracked.Id.ToString() ? tracked : null);
    }
}
