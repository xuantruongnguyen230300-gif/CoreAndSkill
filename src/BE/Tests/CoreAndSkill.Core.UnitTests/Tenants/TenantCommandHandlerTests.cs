using CoreAndSkill.Core.Application.Tenants;
using CoreAndSkill.Core.Domain.Common;
using NSubstitute;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.UnitTests.Tenants;

public class TenantCommandHandlerTests
{
    private readonly ITenantProvisioningService _provisioning = Substitute.For<ITenantProvisioningService>();
    private readonly ITenantAdminQueryService _tenantQuery = Substitute.For<ITenantAdminQueryService>();

    [Fact]
    public async Task CreateTenantCommandHandler_Success_ReturnsCreatedTenant()
    {
        var tenantId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        _provisioning.CreateTenantAsync(Arg.Any<CreateTenantInput>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new TenantProvisioningResult(tenantId, adminId, TenantWasCreated: true, AdminWasCreated: true)));
        var dto = new TenantListItemDto(tenantId, "SYT-HN", "Sở Y tế Hà Nội", true, DateTimeOffset.UtcNow);
        _tenantQuery.FindByIdAsync(tenantId, Arg.Any<CancellationToken>()).Returns(dto);
        var handler = new CreateTenantCommandHandler(_provisioning, _tenantQuery);

        var result = await handler.Handle(
            new CreateTenantCommand("SYT-HN", "Sở Y tế Hà Nội", "quantri", "a@vd.vn", "Nguyễn Văn An", "Temp@1"),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(dto);
        await _provisioning.Received(1).CreateTenantAsync(
            Arg.Is<CreateTenantInput>(i => i.FailIfExists && !i.IsSystem && i.AdminHasPermissionBypass && !i.AdminIsSystemOperator),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateTenantCommandHandler_ServiceFails_PropagatesError()
    {
        _provisioning.CreateTenantAsync(Arg.Any<CreateTenantInput>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<TenantProvisioningResult>(TenantProvisioningErrors.CodeDuplicate));
        var handler = new CreateTenantCommandHandler(_provisioning, _tenantQuery);

        var result = await handler.Handle(
            new CreateTenantCommand("SYT-HN", "Sở Y tế Hà Nội", "quantri", "a@vd.vn", "Nguyễn Văn An", "Temp@1"),
            CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe(TenantProvisioningErrors.CodeDuplicate.Code);
        await _tenantQuery.DidNotReceive().FindByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetTenantActiveCommandHandler_DelegatesToProvisioningService()
    {
        var id = Guid.NewGuid();
        _provisioning.SetTenantActiveAsync(id, false, Arg.Any<CancellationToken>()).Returns(Result.Success());
        var handler = new SetTenantActiveCommandHandler(_provisioning);

        var result = await handler.Handle(new SetTenantActiveCommand(id, false), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        await _provisioning.Received(1).SetTenantActiveAsync(id, false, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RecoveryResetTenantAdminPasswordCommandHandler_DelegatesToProvisioningService()
    {
        var id = Guid.NewGuid();
        _provisioning.RecoveryResetAdminPasswordAsync(id, "quantri", "Temp@1", Arg.Any<CancellationToken>())
            .Returns(Result.Success());
        var handler = new RecoveryResetTenantAdminPasswordCommandHandler(_provisioning);

        var result = await handler.Handle(
            new RecoveryResetTenantAdminPasswordCommand(id, "quantri", "Temp@1"), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task CreateTenantAdminCommandHandler_Success_ReturnsSuccessWithoutData()
    {
        var id = Guid.NewGuid();
        var newUserId = Guid.NewGuid();
        _provisioning.CreateAdditionalAdminAsync(id, "quantri2", "b@vd.vn", "Trần Thị Bình", "Temp@1", Arg.Any<CancellationToken>())
            .Returns(Result.Success(newUserId));
        var handler = new CreateTenantAdminCommandHandler(_provisioning);

        var result = await handler.Handle(
            new CreateTenantAdminCommand(id, "quantri2", "b@vd.vn", "Trần Thị Bình", "Temp@1"), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task CreateTenantAdminCommandHandler_ServiceFails_PropagatesError()
    {
        var id = Guid.NewGuid();
        _provisioning.CreateAdditionalAdminAsync(id, "quantri2", "b@vd.vn", "Trần Thị Bình", "Temp@1", Arg.Any<CancellationToken>())
            .Returns(Result.Failure<Guid>(TenantProvisioningErrors.AdminCreateFailed));
        var handler = new CreateTenantAdminCommandHandler(_provisioning);

        var result = await handler.Handle(
            new CreateTenantAdminCommand(id, "quantri2", "b@vd.vn", "Trần Thị Bình", "Temp@1"), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe(TenantProvisioningErrors.AdminCreateFailed.Code);
    }
}
