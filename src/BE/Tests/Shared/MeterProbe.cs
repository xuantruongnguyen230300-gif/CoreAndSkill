using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using Xunit;

namespace CoreAndSkill.Tests.Shared;

// Dụng cụ đo chỉ số cho test — docs/wiki-core/be/04-testing-strategy.md §8 ("test không ổn định phải
// được sửa, không chạy lại cho qua").
//
// VẤN ĐỀ NÓ GIẢI. `CoreMetrics` là một `Meter` TĨNH toàn tiến trình, còn `MeterListener` nghe TOÀN
// tiến trình. xUnit chạy các lớp test song song trong CÙNG một tiến trình, nên một listener mở trong
// test A nhìn thấy luôn phép đo do test B phát ra. Hai kiểu hỏng đã dựng lại được (mỗi lần chạy đều
// đỏ khi có một lớp test khác phát chỉ số liên tục):
//   • test khẳng định TẬP tên chỉ số thấy thêm tên lạ — `core.auth.login_failed`, `core.job.failed`;
//   • test khẳng định GIÁ TRỊ đếm được 400 thay vì 1.
//
// CÁCH CÔ LẬP — hai lớp, cần cả hai:
//   1. Chỉ bật đúng các `Instrument` được truyền vào, so theo THAM CHIẾU chứ không theo tên. Lọc theo
//      tên vẫn đúng hôm nay nhưng không nói được ý định: probe canh đúng những dụng cụ này.
//   2. Chỉ ghi phép đo phát ra TRONG luồng logic của chính test. Luồng nhận diện bằng `AsyncLocal`:
//      giá trị của nó chảy theo `await` và theo `Task.Run` của chính test, nhưng một test chạy song
//      song có `ExecutionContext` riêng nên KHÔNG bao giờ thấy được nó.
//
// Lớp 2 là lớp thiết yếu. Lớp 1 không cứu được ca hai test cùng đo MỘT instrument — `core.job.failed`
// bị cả `JobRunnerTests` lẫn `CoreMetricsTests` đo, và đó chính là ca đã đỏ.
//
// GIỚI HẠN. Probe chỉ cô lập phép ĐO. Trạng thái tĩnh mà chính chỉ số đọc ra — `CoreMetrics
// .SetOutboxSnapshot` — vẫn là biến toàn tiến trình: hai test cùng ghi nó vẫn đè nhau, và probe không
// thấy gì bất thường vì phép đo vẫn là của luồng mình. Chỗ đó ép bằng `OutboxSnapshotCollection` bên
// dưới, không ép bằng câu dặn trong chú thích.
//
// Không lồng hai probe trong một test: probe sau chiếm chỗ probe trước cho tới khi nó `Dispose`.
internal sealed class MeterProbe : IDisposable
{
    private static readonly AsyncLocal<Guid> OwningFlow = new();

    private readonly Guid _token = Guid.NewGuid();
    private readonly Guid _flowBeforeProbe;
    private readonly Instrument[] _watched;
    private readonly MeterListener _listener = new();
    private readonly ConcurrentQueue<(string Name, double Value)> _measured = new();

    public MeterProbe(params Instrument[] watched)
    {
        _watched = watched;
        _flowBeforeProbe = OwningFlow.Value;
        OwningFlow.Value = _token;

        _listener.InstrumentPublished = (instrument, listener) =>
        {
            if (Array.Exists(_watched, w => ReferenceEquals(w, instrument)))
                listener.EnableMeasurementEvents(instrument);
        };
        _listener.SetMeasurementEventCallback<long>((instrument, measurement, _, _) => Keep(instrument, measurement));
        _listener.SetMeasurementEventCallback<double>((instrument, measurement, _, _) => Keep(instrument, measurement));
        _listener.Start();
    }

    // Callback chạy ĐỒNG BỘ ngay trong lời gọi `Add`, tức trên luồng của nơi phát — nên `OwningFlow`
    // ở đây là của luồng đó, không phải của luồng đã dựng probe. Đó chính là chỗ phân biệt được
    // "phép đo của test này" với "phép đo của test khác đang chạy song song".
    private void Keep(Instrument instrument, double measurement)
    {
        if (OwningFlow.Value != _token)
            return;

        _measured.Enqueue((instrument.Name, measurement));
    }

    // Tên các chỉ số probe thực sự ghi được — không phải tên các chỉ số nó canh.
    public IReadOnlyCollection<string> Names => _measured.Select(m => m.Name).Distinct(StringComparer.Ordinal).ToList();

    // Đọc lại các chỉ số kiểu gauge; callback của gauge chạy đồng bộ ngay tại đây, tức trong luồng của test.
    public void RecordObservableInstruments() => _listener.RecordObservableInstruments();

    // Tổng các phép đo của một chỉ số. Chỉ số không có phép đo nào thì là 0 — đó là câu trả lời đúng
    // cho câu hỏi "test này làm nó tăng bao nhiêu".
    public long Count(string instrumentName)
        => (long)_measured.Where(m => string.Equals(m.Name, instrumentName, StringComparison.Ordinal)).Sum(m => m.Value);

    // Giá trị đo GẦN NHẤT của một chỉ số. Không có phép đo nào thì ném — trả 0 im lặng sẽ biến một
    // probe hỏng thành một test vẫn xanh (04-testing-strategy.md §3.4: "duyệt tập rỗng thì luôn PASS").
    public double Value(string instrumentName)
    {
        var values = _measured
            .Where(m => string.Equals(m.Name, instrumentName, StringComparison.Ordinal))
            .Select(m => m.Value)
            .ToList();

        return values.Count > 0
            ? values[^1]
            : throw new InvalidOperationException($"MeterProbe không ghi được phép đo nào của '{instrumentName}'.");
    }

    public void Dispose()
    {
        _listener.Dispose();
        OwningFlow.Value = _flowBeforeProbe;
    }
}

// MỌI lớp test ghi `CoreMetrics.SetOutboxSnapshot` phải khai `[Collection(OutboxSnapshotCollection.Name)]`.
//
// Ảnh chụp outbox là biến tĩnh toàn tiến trình, và ba gauge đọc thẳng từ nó. Hai lớp test cùng ghi mà
// chạy song song thì lớp này đọc ra giá trị lớp kia vừa đặt — đúng loại giật mà `MeterProbe` KHÔNG cứu
// được, vì phép đo vẫn phát ra trong luồng của chính test đang đọc. xUnit chạy các lớp trong cùng một
// collection tuần tự với nhau, nên khai chung collection là cách chặn duy nhất không dựa vào trí nhớ.
[CollectionDefinition(OutboxSnapshotCollection.Name)]
public sealed class OutboxSnapshotCollection
{
    public const string Name = "CoreMetrics: anh chup outbox";
}
