using CoreAndSkill.Core.Application.Identity;
using CoreAndSkill.Core.Application.Users;
using CoreAndSkill.Core.Domain.Common;
using NSubstitute;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.UnitTests.Users;

public class UpdateUserCommandHandlerTests
{
    private readonly IUserAdminService _userAdmin = Substitute.For<IUserAdminService>();

    [Fact]
    public async Task Handle_DelegatesToUserAdminService()
    {
        var id = Guid.NewGuid();
        _userAdmin.UpdateAsync(id, Arg.Any<UpdateUserInput>(), Arg.Any<CancellationToken>()).Returns(Result.Success());
        var handler = new UpdateUserCommandHandler(_userAdmin);

        var result = await handler.Handle(new UpdateUserCommand(id, "an@vd.vn", "An", "v1"), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        await _userAdmin.Received(1).UpdateAsync(
            id, Arg.Is<UpdateUserInput>(i => i.Email == "an@vd.vn" && i.FullName == "An" && i.Version == "v1"),
            Arg.Any<CancellationToken>());
    }
}
