using CoreAndSkill.Core.Application.Diagnostics;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Tests.Shared;

// Meta-test cho chính `MeterProbe` — cùng lý lẽ với luật T1 (docs/wiki-core/be/04-testing-strategy.md §3):
// một dụng cụ đo luôn cho kết quả sạch vì nó không ghi được gì cả thì tệ hơn là không có dụng cụ, vì
// mọi test dựng trên nó sẽ xanh mà không kiểm gì.
//
// Bộ này có hai vế đối xứng, đúng khuôn §3.2:
//   • probe PHẢI ghi được phép đo của chính luồng nó — nếu không, mọi test dùng nó xanh giả;
//   • probe PHẢI bỏ qua phép đo của luồng khác — đó chính là ca đã làm
//     `B4SupportTests.OutboxAndFileCounters_AreObservableThroughAMeterListener` giật.
//
// Lớp này ghi ảnh chụp outbox tĩnh nên phải khai chung collection với `B4SupportTests`.
[Collection(OutboxSnapshotCollection.Name)]
public class MeterProbeTests
{
    // Dựng lại đúng cảnh "một test khác chạy song song": một test song song có ExecutionContext RIÊNG,
    // nơi `AsyncLocal` của probe chưa bao giờ được đặt. `new Thread(...).Start()` mặc định CHÉP
    // ExecutionContext của nơi gọi — chép thì không còn là luồng khác nữa — nên phải chặn dòng chảy đó.
    private static void EmitFromAnotherFlow(Action emit)
    {
        Exception? failure = null;

        using (ExecutionContext.SuppressFlow())
        {
            var thread = new Thread(() =>
            {
                try
                {
                    emit();
                }
                catch (Exception ex)
                {
                    failure = ex;
                }
            });

            thread.Start();
            thread.Join();
        }

        if (failure is not null)
            throw failure;
    }

    [Fact]
    public void Probe_RecordsTheMeasurementsItsOwnFlowEmits()
    {
        using var probe = new MeterProbe(CoreMetrics.OutboxDispatched);

        CoreMetrics.OutboxDispatched.Add(1);
        CoreMetrics.OutboxDispatched.Add(1);

        probe.Count("core.outbox.dispatched").ShouldBe(2);
    }

    [Fact]
    public async Task Probe_StillRecords_AfterTheTestFlowCrossesAnAwait()
    {
        using var probe = new MeterProbe(CoreMetrics.OutboxDispatched);

        await Task.Yield();
        await Task.Run(() => CoreMetrics.OutboxDispatched.Add(1));

        probe.Count("core.outbox.dispatched").ShouldBe(1, "ExecutionContext chảy theo await và Task.Run của chính test");
    }

    [Fact]
    public void Probe_IgnoresAnInstrumentItWasNotAskedToWatch()
    {
        using var probe = new MeterProbe(CoreMetrics.OutboxDispatched);

        CoreMetrics.OutboxDead.Add(1);

        probe.Names.ShouldBeEmpty();
        probe.Count("core.outbox.dead").ShouldBe(0);
    }

    [Fact]
    public void Probe_IgnoresAMeasurementOnAWatchedInstrument_WhenAnotherFlowEmitsIt()
    {
        using var probe = new MeterProbe(CoreMetrics.OutboxDispatched);

        CoreMetrics.OutboxDispatched.Add(1);
        EmitFromAnotherFlow(() => CoreMetrics.OutboxDispatched.Add(7));

        probe.Count("core.outbox.dispatched").ShouldBe(1, "7 kia là của luồng khác — cùng instrument, khác luồng");
    }

    // Ca đã đỏ thật, dựng lại tất định: listener toàn tiến trình nhặt thêm `core.auth.login_failed`
    // và `core.job.failed` do test khác phát, rồi phép so cả tập báo đỏ.
    [Fact]
    public void Probe_KeepsTheNameSetExact_WhenAnotherFlowEmitsOtherCoreCounters()
    {
        using var probe = new MeterProbe(CoreMetrics.OutboxDispatched, CoreMetrics.OutboxDead);

        CoreMetrics.OutboxDispatched.Add(1);
        EmitFromAnotherFlow(() =>
        {
            CoreMetrics.LoginFailed.Add(1);
            CoreMetrics.BackgroundJobFailed.Add(1);
            CoreMetrics.OutboxDead.Add(1);
        });

        probe.Names.ShouldBe(["core.outbox.dispatched"], ignoreOrder: true);
    }

    [Fact]
    public void Probe_ReadsTheLastValueOfAnObservableGauge()
    {
        using var probe = new MeterProbe(CoreMetrics.OutboxPending);

        CoreMetrics.SetOutboxSnapshot(pendingCount: 11, oldestPendingAgeSeconds: 0, deadCount: 0);
        probe.RecordObservableInstruments();

        probe.Value("core.outbox.pending").ShouldBe(11);
    }

    [Fact]
    public void Probe_Value_ThrowsInsteadOfReturningZero_WhenNothingWasMeasured()
    {
        using var probe = new MeterProbe(CoreMetrics.OutboxPending);

        Should.Throw<InvalidOperationException>(() => probe.Value("core.outbox.pending"))
            .Message.ShouldContain("core.outbox.pending");
    }

    [Fact]
    public void Probe_StopsRecording_AfterItIsDisposed()
    {
        var probe = new MeterProbe(CoreMetrics.OutboxDispatched);
        probe.Dispose();

        CoreMetrics.OutboxDispatched.Add(1);

        probe.Count("core.outbox.dispatched").ShouldBe(0);
    }
}
