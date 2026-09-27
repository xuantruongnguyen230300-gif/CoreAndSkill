using CoreAndSkill.Core.Application.Roles;
using NSubstitute;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.UnitTests.Roles;

public class GetRoleByIdQueryHandlerTests
{
    private readonly IRoleQueryService _roleQuery = Substitute.For<IRoleQueryService>();

    [Fact]
    public async Task Handle_NotFound_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _roleQuery.FindByIdAsync(id, Arg.Any<CancellationToken>()).Returns((RoleSummaryDto?)null);
        var handler = new GetRoleByIdQueryHandler(_roleQuery);

        var result = await handler.Handle(new GetRoleByIdQuery(id), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe(RoleErrors.NotFound.Code);
    }

    [Fact]
    public async Task Handle_Found_ReturnsSuccess()
    {
        var id = Guid.NewGuid();
        var dto = new RoleSummaryDto(id, "Quản trị", true, 1, null, "stamp");
        _roleQuery.FindByIdAsync(id, Arg.Any<CancellationToken>()).Returns(dto);
        var handler = new GetRoleByIdQueryHandler(_roleQuery);

        var result = await handler.Handle(new GetRoleByIdQuery(id), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(dto);
    }
}
