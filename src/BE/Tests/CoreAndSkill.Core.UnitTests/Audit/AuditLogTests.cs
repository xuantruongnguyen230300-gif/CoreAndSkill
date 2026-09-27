using CoreAndSkill.Core.Domain.Audit;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.UnitTests.Audit;

public class AuditLogTests
{
    [Fact]
    public void Record_SetsRequiredFields()
    {
        var tenantId = Guid.NewGuid();
        var occurredAt = DateTimeOffset.UtcNow;
        var actorUserId = Guid.NewGuid();

        var result = AuditLog.Record(
            tenantId, occurredAt, actorUserId, "Nguyễn Văn An", "core.user.lock", "core.user", "abc-123", "Nguyễn Văn An");

        result.IsSuccess.ShouldBeTrue();

        var log = result.Value;
        log.Id.ShouldNotBe(Guid.Empty);
        log.TenantId.ShouldBe(tenantId);
        log.OccurredAt.ShouldBe(occurredAt);
        log.ActorUserId.ShouldBe(actorUserId);
        log.ActorDisplay.ShouldBe("Nguyễn Văn An");
        log.ActionCode.ShouldBe("core.user.lock");
        log.TargetType.ShouldBe("core.user");
        log.TargetId.ShouldBe("abc-123");
        log.TargetDisplay.ShouldBe("Nguyễn Văn An");
        log.ActorTenantId.ShouldBeNull();
    }

    [Fact]
    public void Record_NoActor_ActorUserIdIsNull_DisplayIsSystem()
    {
        var log = AuditLog.Record(
            Guid.NewGuid(), DateTimeOffset.UtcNow, actorUserId: null, actorDisplay: "system",
            "core.tenant.create", "core.tenant", Guid.NewGuid().ToString()).Value;

        log.ActorUserId.ShouldBeNull();
        log.ActorDisplay.ShouldBe("system");
    }

    [Fact]
    public void Record_CrossTenant_ActorTenantIdMarksIt()
    {
        var systemTenantId = Guid.NewGuid();
        var targetTenantId = Guid.NewGuid();

        var log = AuditLog.Record(
            targetTenantId, DateTimeOffset.UtcNow, Guid.NewGuid(), "superadmin",
            "core.tenant.deactivate", "core.tenant", targetTenantId.ToString(),
            actorTenantId: systemTenantId).Value;

        log.TenantId.ShouldBe(targetTenantId);
        log.ActorTenantId.ShouldBe(systemTenantId); // dấu hiệu XUYÊN ĐƠN VỊ
    }
}
