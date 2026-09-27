using CoreAndSkill.Core.Application.Auth;
using CoreAndSkill.Core.Application.Configuration;
using CoreAndSkill.Core.Application.Identity;
using CoreAndSkill.Core.Application.Permissions;
using CoreAndSkill.Core.Application.Tenants;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.UnitTests.Auth;

public class SessionDtoFactoryTests
{
    [Fact]
    public async Task BuildAsync_ComposesSessionFromAllSeams()
    {
        var userLookup = Substitute.For<IUserLookupService>();
        var permissionChecker = Substitute.For<IPermissionChecker>();
        var userId = Guid.NewGuid();
        var user = new UserSummaryDto(userId, "an.nv", "An", "an@vd.vn", false, true, false, "vi");
        var tenant = new TenantSummary(Guid.NewGuid(), "SO-GD", "Sở GD", true);

        userLookup.GetRoleNamesAsync(userId, Arg.Any<CancellationToken>()).Returns(["Vận hành"]);
        permissionChecker.GetEffectivePermissionsAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new HashSet<string> { "core.menu.read" });

        var options = Options.Create(new CoreAuthOptions
        {
            CookieName = "c",
            AntiforgeryCookieName = "a",
            SessionMinutes = 30,
            AllowedOrigins = ["https://x"],
        });
        var factory = new SessionDtoFactory(userLookup, permissionChecker, options);

        var session = await factory.BuildAsync(user, tenant, mustChangePassword: true, CancellationToken.None);

        session.Id.ShouldBe(userId);
        session.UserName.ShouldBe("an.nv");
        session.Email.ShouldBe("an@vd.vn");
        session.FullName.ShouldBe("An");
        session.Roles.ShouldContain("Vận hành");
        session.Permissions.ShouldContain("core.menu.read");
        session.MustChangePassword.ShouldBeTrue();
        session.IsSystemOperator.ShouldBeTrue();
        session.SessionMinutes.ShouldBe(30);
        session.PreferredLanguage.ShouldBe("vi");
        session.TenantCode.ShouldBe("SO-GD");
        session.TenantName.ShouldBe("Sở GD");
    }
}
