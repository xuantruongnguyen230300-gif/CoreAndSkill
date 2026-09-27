using CoreAndSkill.Core.Application.Permissions;
using NSubstitute;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.UnitTests.Permissions;

public class PermissionQueryHandlerTests
{
    private readonly IPermissionMatrixService _matrixService = Substitute.For<IPermissionMatrixService>();

    [Fact]
    public async Task GetPermissionsQueryHandler_ReturnsCatalog()
    {
        IReadOnlyList<PermissionListItemDto> catalog =
            [new PermissionListItemDto(Guid.NewGuid(), "core.user.read", "core.user", "read", "permission.core.user.read", true, null)];
        _matrixService.GetCatalogAsync(Arg.Any<CancellationToken>()).Returns(catalog);
        var handler = new GetPermissionsQueryHandler(_matrixService);

        var result = await handler.Handle(new GetPermissionsQuery(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(catalog);
    }

    [Fact]
    public async Task GetPermissionMatrixQueryHandler_ReturnsMatrix()
    {
        var matrix = new PermissionMatrixDto([], [], "sha256:abc");
        _matrixService.GetMatrixAsync(Arg.Any<CancellationToken>()).Returns(matrix);
        var handler = new GetPermissionMatrixQueryHandler(_matrixService);

        var result = await handler.Handle(new GetPermissionMatrixQuery(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(matrix);
    }

    [Fact]
    public async Task GetPermissionMatrixByResourceQueryHandler_ReturnsMatrix()
    {
        var matrix = new PermissionMatrixByResourceDto([], [], "sha256:abc");
        _matrixService.GetMatrixByResourceAsync(Arg.Any<CancellationToken>()).Returns(matrix);
        var handler = new GetPermissionMatrixByResourceQueryHandler(_matrixService);

        var result = await handler.Handle(new GetPermissionMatrixByResourceQuery(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(matrix);
    }
}
