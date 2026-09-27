using CoreAndSkill.Core.Application.Identity;
using CoreAndSkill.Core.Application.Users;
using CoreAndSkill.Core.Domain.Common;
using NSubstitute;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.UnitTests.Users;

public class UnlockUserCommandHandlerTests
{
    private readonly IUserAdminService _userAdmin = Substitute.For<IUserAdminService>();

    [Fact]
    public async Task Handle_DelegatesToUserAdminService()
    {
        var id = Guid.NewGuid();
        _userAdmin.UnlockAsync(id, "v1", Arg.Any<CancellationToken>()).Returns(Result.Success());
        var handler = new UnlockUserCommandHandler(_userAdmin);

        var result = await handler.Handle(new UnlockUserCommand(id, "v1"), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        await _userAdmin.Received(1).UnlockAsync(id, "v1", Arg.Any<CancellationToken>());
    }
}
