using CoreAndSkill.Core.Application.Diagnostics;
using CoreAndSkill.Tests.Shared;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.UnitTests.Diagnostics;

// docs/wiki-core/be/07-observability.md §7, §9 — Meter khai trong code, chưa xuất ra hệ ngoài.
// Kiểm bằng MeterListener (BCL, không cần hạ tầng thu thập nào) — đúng cách "nghe" một Meter.
//
// Nghe qua `MeterProbe`, không qua `MeterListener` trần: listener trần nghe TOÀN tiến trình, nên hai
// test dưới đây từng đếm luôn phép đo của `LoginCommandHandlerTests` và `JobRunnerTests` chạy song song
// — đọc ra 400 thay vì 1. Xem `Tests/Shared/MeterProbe.cs`.
public class CoreMetricsTests
{
    [Fact]
    public void MeterName_MatchesDocumentedValue()
        => CoreMetrics.MeterName.ShouldBe("CoreAndSkill.Core");

    [Fact]
    public void LoginFailed_Add_IsObservableThroughMeterListener()
    {
        using var probe = new MeterProbe(CoreMetrics.LoginFailed);

        CoreMetrics.LoginFailed.Add(1);

        probe.Count("core.auth.login_failed").ShouldBe(1);
    }

    [Fact]
    public void BackgroundJobFailed_Add_IsObservableThroughMeterListener()
    {
        using var probe = new MeterProbe(CoreMetrics.BackgroundJobFailed);

        CoreMetrics.BackgroundJobFailed.Add(1);

        probe.Count("core.job.failed").ShouldBe(1);
    }
}
