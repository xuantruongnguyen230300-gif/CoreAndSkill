using CoreAndSkill.Core.Application.Common;
using CoreAndSkill.Core.Application.Roles;
using CoreAndSkill.Core.Domain.Common;
using NSubstitute;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.UnitTests.Roles;

public class RoleCommandHandlerTests
{
    private readonly IRoleAdminService _roleAdmin = Substitute.For<IRoleAdminService>();
    private readonly IRoleQueryService _roleQuery = Substitute.For<IRoleQueryService>();

    [Fact]
    public async Task CreateRoleCommandHandler_DelegatesToRoleAdminService()
    {
        var newId = Guid.NewGuid();
        _roleAdmin.CreateAsync("Kế toán", Arg.Any<CancellationToken>()).Returns(Result.Success(newId));
        var handler = new CreateRoleCommandHandler(_roleAdmin);

        var result = await handler.Handle(new CreateRoleCommand("Kế toán"), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(newId);
    }

    [Fact]
    public async Task UpdateRoleCommandHandler_PassesVersion_AndReturnsRoleWithNewVersion()
    {
        var id = Guid.NewGuid();
        var renamed = new RoleSummaryDto(id, "Kế toán trưởng", false, 0, null, "stamp-moi");
        _roleAdmin.RenameAsync(id, "Kế toán trưởng", "stamp-cu", Arg.Any<CancellationToken>()).Returns(Result.Success());
        _roleQuery.FindByIdAsync(id, Arg.Any<CancellationToken>()).Returns(renamed);
        var handler = new UpdateRoleCommandHandler(_roleAdmin, _roleQuery);

        var result = await handler.Handle(new UpdateRoleCommand(id, "Kế toán trưởng", "stamp-cu"), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(renamed);
        await _roleAdmin.Received(1).RenameAsync(id, "Kế toán trưởng", "stamp-cu", Arg.Any<CancellationToken>());
    }

    // Xung đột đi thẳng ra ngoài; không đọc lại bản ghi — không có gì mới để trả.
    [Fact]
    public async Task UpdateRoleCommandHandler_ConcurrencyConflict_Propagates_WithoutReadingBack()
    {
        var id = Guid.NewGuid();
        _roleAdmin.RenameAsync(id, "Kế toán trưởng", "stamp-cu", Arg.Any<CancellationToken>())
            .Returns(Result.Failure(CommonErrors.ConcurrencyConflict));
        var handler = new UpdateRoleCommandHandler(_roleAdmin, _roleQuery);

        var result = await handler.Handle(new UpdateRoleCommand(id, "Kế toán trưởng", "stamp-cu"), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe(CommonErrors.ConcurrencyConflict.Code);
        await _roleQuery.DidNotReceiveWithAnyArgs().FindByIdAsync(default, default);
    }

    [Fact]
    public async Task DeleteRoleCommandHandler_DelegatesToRoleAdminService()
    {
        var id = Guid.NewGuid();
        _roleAdmin.DeleteAsync(id, Arg.Any<CancellationToken>()).Returns(Result.Success());
        var handler = new DeleteRoleCommandHandler(_roleAdmin);

        var result = await handler.Handle(new DeleteRoleCommand(id), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        await _roleAdmin.Received(1).DeleteAsync(id, Arg.Any<CancellationToken>());
    }
}
