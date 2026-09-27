using CoreAndSkill.Core.Application.Permissions;
using CoreAndSkill.Core.Domain.Common;
using NSubstitute;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.UnitTests.Permissions;

public class UpdatePermissionMatrixCommandHandlerTests
{
    private readonly IPermissionMatrixService _matrixService = Substitute.For<IPermissionMatrixService>();

    private UpdatePermissionMatrixCommandHandler CreateHandler() => new(_matrixService);

    [Fact]
    public async Task Handle_DuplicatePermissionIdInEntries_ReturnsDuplicateEntry_WithoutCallingService()
    {
        var permissionId = Guid.NewGuid();
        var command = new UpdatePermissionMatrixCommand("v1",
        [
            new UpdatePermissionMatrixEntry(permissionId, [Guid.NewGuid()]),
            new UpdatePermissionMatrixEntry(permissionId, []),
        ]);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe(PermissionErrors.DuplicateEntry.Code);
        await _matrixService.DidNotReceiveWithAnyArgs().ReplaceMatrixAsync(default, default!, default);
    }

    // docs/contracts/permissions.md §6: fieldErrors["Entries"] (không chỉ số) và messageParams nêu đích danh id bị lặp.
    [Fact]
    public async Task Handle_DuplicatePermissionId_NamesTheDuplicatedId_AndFlagsEntries()
    {
        var duplicated = Guid.NewGuid();
        var command = new UpdatePermissionMatrixCommand("v1",
        [
            new UpdatePermissionMatrixEntry(Guid.NewGuid(), []),
            new UpdatePermissionMatrixEntry(duplicated, []),
            new UpdatePermissionMatrixEntry(duplicated, []),
        ]);

        var error = (await CreateHandler().Handle(command, CancellationToken.None)).Error!;

        error.Params["PermissionId"].ShouldBe(duplicated.ToString());
        error.FieldErrors.Keys.ShouldBe(["Entries"]);
        error.FieldErrors["Entries"].Single().Code.ShouldBe(PermissionErrors.DuplicateEntry.Code);
    }

    // ENTRIES_INCOMPLETE trả từ PermissionMatrixService (cần danh mục trong DB) — mang sẵn fieldErrors["Entries"] ngay
    // trong Error của catalog, nên mọi chỗ trả mã này đều mang nó.
    [Fact]
    public void EntriesIncomplete_CarriesTheEntriesFieldError()
    {
        PermissionErrors.EntriesIncomplete.FieldErrors.Keys.ShouldBe(["Entries"]);
        PermissionErrors.EntriesIncomplete.FieldErrors["Entries"].Single().Code.ShouldBe(PermissionErrors.EntriesIncomplete.Code);
    }

    [Fact]
    public async Task Handle_Success_ReturnsNewVersion()
    {
        var permissionId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        _matrixService.ReplaceMatrixAsync("v1", Arg.Any<IReadOnlyDictionary<Guid, IReadOnlyList<Guid>>>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success("sha256:abcdef12"));
        var command = new UpdatePermissionMatrixCommand("v1", [new UpdatePermissionMatrixEntry(permissionId, [roleId])]);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Version.ShouldBe("sha256:abcdef12");
    }

    [Fact]
    public async Task Handle_ServiceFails_PropagatesError()
    {
        _matrixService.ReplaceMatrixAsync(Arg.Any<string?>(), Arg.Any<IReadOnlyDictionary<Guid, IReadOnlyList<Guid>>>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<string>(PermissionErrors.VersionMismatch));
        var command = new UpdatePermissionMatrixCommand("stale", [new UpdatePermissionMatrixEntry(Guid.NewGuid(), [])]);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe(PermissionErrors.VersionMismatch.Code);
    }
}
