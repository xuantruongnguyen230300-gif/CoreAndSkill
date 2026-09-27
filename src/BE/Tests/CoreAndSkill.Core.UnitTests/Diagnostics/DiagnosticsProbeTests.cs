using CoreAndSkill.Core.Application.Diagnostics;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.UnitTests.Diagnostics;

public class DiagnosticsProbeTests
{
    [Fact]
    public void Succeed_ReturnsSuccessResult_WithServerTimeFromProvider()
    {
        var now = new DateTimeOffset(2026, 9, 16, 10, 0, 0, TimeSpan.Zero);
        var result = DiagnosticsProbe.Succeed(new FixedTimeProvider(now));

        result.IsSuccess.ShouldBeTrue();
        result.Value.Message.ShouldBe("pong");
        result.Value.ServerTimeUtc.ShouldBe(now);
    }

    [Fact]
    public void Fail_ReturnsFailureResult_WithProbeFailureCatalogError()
    {
        var result = DiagnosticsProbe.Fail();

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(DiagnosticsErrors.ProbeFailureRequested);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
