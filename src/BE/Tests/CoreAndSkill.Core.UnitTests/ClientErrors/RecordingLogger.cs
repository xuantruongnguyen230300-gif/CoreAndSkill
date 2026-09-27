using Microsoft.Extensions.Logging;

namespace CoreAndSkill.Core.UnitTests.ClientErrors;

// NullLogger không nói được gì về NỘI DUNG dòng log, và nội dung mới là thứ luật §5 của
// docs/wiki-core/be/07-observability.md canh ("không được log cái gì"). Một test dùng NullLogger
// vẫn xanh nguyên khi handler ghi thẳng mật khẩu ra — đó là cổng hỏng âm thầm.
//
// Đây là lớp CÀI TAY chứ không phải mock: handler là `internal`, nên NSubstitute không proxy được
// ILogger<ReportClientErrorCommandHandler> (Castle DynamicProxy cần InternalsVisibleTo cho assembly
// proxy động, không khai ở đây). Cài tay một interface ba phương thức rẻ hơn mở InternalsVisibleTo.
internal sealed class RecordingLogger<T> : ILogger<T>
{
    private readonly List<Entry> _entries = [];

    public IReadOnlyList<Entry> Entries => _entries;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        // Giữ CẢ HAI mặt của một dòng log có cấu trúc, vì bí mật lọt qua mặt nào cũng là lọt:
        // chuỗi đã dựng (thứ đi vào sink dạng văn bản) và từng giá trị tham số (thứ đi vào sink
        // dạng có cấu trúc, và sống lâu hơn).
        var values = state as IReadOnlyList<KeyValuePair<string, object?>> ?? [];

        _entries.Add(new Entry(
            logLevel,
            formatter(state, exception),
            [.. values.Select(pair => pair.Value?.ToString() ?? string.Empty)]));
    }

    internal sealed record Entry(LogLevel Level, string Message, IReadOnlyList<string> Values);
}
