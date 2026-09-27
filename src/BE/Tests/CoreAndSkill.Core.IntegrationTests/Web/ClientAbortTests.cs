using System.Net;
using CoreAndSkill.Core.IntegrationTests.Support;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Web;

// Luật R10 — docs/RULES.md §5, docs/quy-uoc/be-api-controller.md §2.4 hàng "IExceptionHandler của Core": client đã huỷ
// request (RequestAborted đã huỷ) mà ngoại lệ đi lên là một ngoại lệ KHÁC bọc OperationCanceledException / IOException
// bên trong ⇒ 499, KHÔNG envelope (client đã đi, không ai đọc), KHÔNG dòng log Error (đóng tab giữa chừng không phải sự
// cố vận hành). Ca ngoại lệ đứng ĐẦU là OperationCanceledException thì ExceptionHandlerMiddleware của framework tự xử lý
// trước khi tới Core (docs/wiki-core/be/ly-do/be-api-controller.md §2.4) — nên test này chỉ chứng minh ca BỊ BỌC.
//
// Qua pipeline THẬT của host (UseExceptionHandler + CoreExceptionHandler + EnvelopeMiddleware), không gọi handler trần:
// hành vi cuối cùng phụ thuộc cả việc framework có nhường ca bọc cho Core hay không, và EnvelopeMiddleware có bọc lại
// một thân rỗng 499 hay không. Endpoint ném nằm trong CHÍNH project test — Core không có endpoint sản phẩm nào ném
// một ngoại lệ bọc theo yêu cầu, và không được thêm một cái chỉ để test.
public sealed class ClientAbortTests : IDisposable
{
    private readonly RecordingLoggerProvider _logs = new();
    private readonly CoreWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ClientAbortTests()
    {
        _factory = new CoreWebApplicationFactory(configureServices: services =>
        {
            services.AddControllers().AddApplicationPart(typeof(AbortProbeController).Assembly);
            services.AddSingleton<ILoggerProvider>(_logs);
        });
        _client = _factory.CreateHttpsClient(); // host đã lên tới đây — nhiễu khởi động (DB không tới được) nằm trước mốc này
        _logs.Clear();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    public static TheoryData<string> WrappedAbortKinds => new() { AbortProbeController.WrappedCancellation, AbortProbeController.WrappedIo };

    [Theory]
    [MemberData(nameof(WrappedAbortKinds))]
    public async Task ClientAbort_WrappedCancellation_Returns499_WithoutErrorLog(string kind)
    {
        var response = await _client.GetAsync($"{AbortProbeController.Path}?kind={kind}&aborted=true");
        var body = await response.Content.ReadAsStringAsync();

        ((int)response.StatusCode).ShouldBe(499);
        body.ShouldBeEmpty("client đã đi — không có envelope nào để đọc");
        ErrorsOnTheRequestPath().ShouldBeEmpty("một lần đóng tab giữa chừng không phải sự cố vận hành — không được ghi Error");
    }

    // Đối chứng: cùng ngoại lệ bọc nhưng client KHÔNG huỷ — đây là lỗi thật (một CancellationTokenSource nội bộ hết hạn,
    // một IOException của kho tệp), phải ra 500 kèm envelope và một dòng Error như mọi ngoại lệ ngoài dự kiến. Thiếu
    // đối chứng này, nhánh 499 có thể nuốt mọi OperationCanceledException bọc và làm sự cố thật im lặng.
    [Theory]
    [MemberData(nameof(WrappedAbortKinds))]
    public async Task WrappedCancellation_WithoutClientAbort_IsStillA500_WithEnvelopeAndErrorLog(string kind)
    {
        var response = await _client.GetAsync($"{AbortProbeController.Path}?kind={kind}&aborted=false");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        body.ShouldContain("CORE.SYSTEM.UNEXPECTED");
        ErrorsOnTheRequestPath().ShouldNotBeEmpty();
    }

    // Chỉ xét đường xử lý request: pipeline HTTP của framework (gồm ExceptionHandlerMiddleware) và mã của Core. Kết nối DB
    // không tới được ở host này sinh Error từ EF/Npgsql theo nhịp của job nền — thứ đó không nói gì về luật R10.
    private List<string> ErrorsOnTheRequestPath() => _logs.Entries
        .Where(e => e.Level >= LogLevel.Error)
        .Where(e => e.Category.StartsWith("Microsoft.AspNetCore.", StringComparison.Ordinal)
                    || e.Category.StartsWith("CoreAndSkill.", StringComparison.Ordinal))
        .Select(e => $"{e.Category}: {e.Message}")
        .ToList();

    // Ghi lại MỌI dòng log của host, mọi category — NullLogger không nói được "không có dòng Error nào".
    private sealed class RecordingLoggerProvider : ILoggerProvider
    {
        private readonly List<Entry> _entries = [];

        public IReadOnlyList<Entry> Entries
        {
            get
            {
                lock (_entries)
                    return [.. _entries];
            }
        }

        public void Clear()
        {
            lock (_entries)
                _entries.Clear();
        }

        public ILogger CreateLogger(string categoryName) => new Logger(this, categoryName);

        public void Dispose()
        {
        }

        internal sealed record Entry(string Category, LogLevel Level, string Message);

        private sealed class Logger(RecordingLoggerProvider owner, string category) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                lock (owner._entries)
                    owner._entries.Add(new Entry(category, logLevel, formatter(state, exception)));
            }
        }
    }
}

// Endpoint thử: ném một ngoại lệ BỌC ca huỷ, tuỳ chọn sau khi đã đánh dấu request là bị client huỷ. RequestAborted của
// TestServer gán được — đó là cách duy nhất mô phỏng "client đã đi" mà phía client vẫn đọc được phản hồi (huỷ thật từ
// HttpClient thì chính client ném TaskCanceledException, không còn phản hồi nào để so). Lớp ở cấp namespace vì MVC chỉ
// nhận controller public KHÔNG lồng; chỉ vào host khi test gắn assembly này bằng AddApplicationPart.
[ApiController]
[Route(Path)]
public sealed class AbortProbeController : ControllerBase
{
    public const string Path = "api/v1/core/test/abort-probe";
    public const string WrappedCancellation = "cancellation";
    public const string WrappedIo = "io";

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Get([FromQuery] string kind, [FromQuery] bool aborted)
    {
        if (aborted)
            HttpContext.RequestAborted = new CancellationToken(canceled: true);

        Exception inner = kind == WrappedIo
            ? new IOException("Kết nối bị đóng trong lúc ghi thân phản hồi.")
            : new OperationCanceledException("Request đã bị huỷ.");

        throw new InvalidOperationException("Một tầng giữa bọc lại ngoại lệ huỷ.", inner);
    }
}
