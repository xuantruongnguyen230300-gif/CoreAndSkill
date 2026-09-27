using CoreAndSkill.Core.Infrastructure.Outbox;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Outbox;

// docs/wiki-core/be/12-notifications.md §2.5: dòng `dead` hoặc dòng `pending` quá cũ ⇒ /health/ready trả Degraded.
// Đây là phần QUYẾT ĐỊNH (thuần, không cần DB). Phần ĐO — câu SQL đếm dead/pending — chỉ được chứng minh bởi
// OutboxDatabaseTests (RequiresDocker), chưa chạy ở môi trường không có Docker.
public class OutboxHealthCheckTests
{
    private static readonly TimeSpan Threshold = TimeSpan.FromMinutes(15);

    [Fact]
    public void OneDeadRow_IsDegraded_NoMatterHowFresh()
    {
        var result = OutboxHealthCheck.Evaluate(new OutboxSnapshot(PendingCount: 0, OldestPendingAge: TimeSpan.Zero, DeadCount: 1), Threshold);

        result.Status.ShouldBe(HealthStatus.Degraded);
    }

    [Fact]
    public void AStalePendingRow_IsDegraded()
    {
        var result = OutboxHealthCheck.Evaluate(new OutboxSnapshot(3, TimeSpan.FromMinutes(16), 0), Threshold);

        result.Status.ShouldBe(HealthStatus.Degraded);
    }

    [Fact]
    public void PendingRowsWithinTheThreshold_AreHealthy()
    {
        var result = OutboxHealthCheck.Evaluate(new OutboxSnapshot(50, TimeSpan.FromMinutes(14), 0), Threshold);

        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public void ExactlyAtTheThreshold_IsStillHealthy_OnlyPastItIsDegraded()
    {
        OutboxHealthCheck.Evaluate(new OutboxSnapshot(1, Threshold, 0), Threshold).Status.ShouldBe(HealthStatus.Healthy);
        OutboxHealthCheck.Evaluate(new OutboxSnapshot(1, Threshold + TimeSpan.FromSeconds(1), 0), Threshold).Status.ShouldBe(HealthStatus.Degraded);
    }

    [Fact]
    public void AnEmptyOutbox_IsHealthy()
    {
        OutboxHealthCheck.Evaluate(new OutboxSnapshot(0, TimeSpan.Zero, 0), Threshold).Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public void TheDegradedMessage_LeaksNoInternals_BecauseTheEndpointIsPublic()
    {
        var result = OutboxHealthCheck.Evaluate(new OutboxSnapshot(0, TimeSpan.Zero, 7), Threshold);

        result.Description.ShouldNotBeNull().ShouldNotContain("7");
        result.Description.ShouldNotContain("core.outbox_message");
    }
}
