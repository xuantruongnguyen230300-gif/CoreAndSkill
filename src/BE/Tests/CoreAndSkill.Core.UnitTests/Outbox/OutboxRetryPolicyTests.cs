using CoreAndSkill.Core.Domain.Outbox;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.UnitTests.Outbox;

// docs/wiki-core/be/12-notifications.md §2.4: khoảng lùi tăng dần, có trần, có ngẫu nhiên.
public class OutboxRetryPolicyTests
{
    [Fact]
    public void DefaultMaxAttempts_IsFive_AsTheDocumentSays()
        => OutboxRetryPolicy.DefaultMaxAttempts.ShouldBe(5);

    [Theory]
    [InlineData(1, 5)]
    [InlineData(2, 30)]
    [InlineData(3, 180)]
    public void Delay_GrowsGeometrically_WithoutJitter(int attempt, double expectedSeconds)
    {
        // jitter 0,5 = đúng giữa dải ±20% => nhân hệ số 1,0.
        OutboxRetryPolicy.Delay(attempt, 0.5).TotalSeconds.ShouldBe(expectedSeconds, tolerance: 0.001);
    }

    [Fact]
    public void Delay_IsCappedAtFifteenMinutes()
    {
        OutboxRetryPolicy.Delay(4, 0.5).ShouldBe(OutboxRetryPolicy.MaxDelay);
        OutboxRetryPolicy.Delay(50, 0.5).ShouldBe(OutboxRetryPolicy.MaxDelay);
    }

    [Fact]
    public void Delay_StaysWithinTwentyPercentOfTheBase()
    {
        var low = OutboxRetryPolicy.Delay(2, 0).TotalSeconds;
        var high = OutboxRetryPolicy.Delay(2, 1).TotalSeconds;

        low.ShouldBe(24, tolerance: 0.001);
        high.ShouldBe(36, tolerance: 0.001);
    }

    [Fact]
    public void Delay_DifferentJitterSamples_GiveDifferentDelays_SoRowsThatFailTogetherDoNotRetryTogether()
        => OutboxRetryPolicy.Delay(2, 0.1).ShouldNotBe(OutboxRetryPolicy.Delay(2, 0.9));

    [Fact]
    public void Delay_ClampsAnOutOfRangeSample_AndAZeroAttemptCount()
    {
        OutboxRetryPolicy.Delay(1, 5).ShouldBe(OutboxRetryPolicy.Delay(1, 1));
        OutboxRetryPolicy.Delay(0, 0.5).ShouldBe(OutboxRetryPolicy.Delay(1, 0.5));
    }
}
