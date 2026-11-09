using System.Collections.Concurrent;
using System.Net;
using CoreAndSkill.Core.IntegrationTests.Support;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Web;

// Mỗi request đúng MỘT dòng Information từ RequestLogMiddleware: phương thức, đường dẫn, mã trạng thái, thời gian, mã lần gọi.
// Không query string, không thân request (docs/wiki-core/be/07-observability.md §5).
public sealed class RequestLogMiddlewareTests
{
    [Fact]
    public async Task EachRequest_WritesExactlyOneInformationLine_WithoutQueryString()
    {
        var provider = new CapturingProvider();
        using var factory = new CoreWebApplicationFactory(
            configureServices: services => services.AddLogging(logging => logging.AddProvider(provider)));
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync("/api/v1/core/khong-ton-tai?token=bi-mat-123");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var line = provider.Entries.Where(e => e.Category.EndsWith("RequestLogMiddleware")).ShouldHaveSingleItem();
        line.Level.ShouldBe(LogLevel.Information);
        line.Message.ShouldContain("GET");
        line.Message.ShouldContain("/api/v1/core/khong-ton-tai");
        line.Message.ShouldContain("404");
        line.Message.ShouldContain(" ms");
        line.Message.ShouldNotContain("bi-mat-123");
        line.Message.ShouldNotContain("token");
    }

    [Fact]
    public async Task HealthCheckRequests_WriteNoLine_WhileOrdinaryRequestsStillWriteOne()
    {
        var provider = new CapturingProvider();
        using var factory = new CoreWebApplicationFactory(
            configureServices: services => services.AddLogging(logging => logging.AddProvider(provider)));
        using var client = factory.CreateHttpsClient();

        (await client.GetAsync("/health/live")).StatusCode.ShouldBe(HttpStatusCode.OK);
        await client.GetAsync("/health/ready");

        provider.Entries.Where(e => e.Category.EndsWith("RequestLogMiddleware")).ShouldBeEmpty();

        await client.GetAsync("/api/v1/core/khong-ton-tai");

        provider.Entries.Where(e => e.Category.EndsWith("RequestLogMiddleware")).ShouldHaveSingleItem();
    }

    [Fact]
    public async Task EveryResponse_CarriesNosniffAndNoStore_EvenForErrors()
    {
        using var factory = new CoreWebApplicationFactory();
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync("/api/v1/core/khong-ton-tai");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Headers.GetValues("X-Content-Type-Options").ShouldBe(["nosniff"]);
        response.Headers.CacheControl!.NoStore.ShouldBeTrue();
    }

    private sealed record Entry(string Category, LogLevel Level, string Message);

    private sealed class CapturingProvider : ILoggerProvider
    {
        public ConcurrentQueue<Entry> Entries { get; } = new();

        public ILogger CreateLogger(string categoryName) => new Capture(categoryName, Entries);

        public void Dispose() { }

        private sealed class Capture(string category, ConcurrentQueue<Entry> sink) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
                => sink.Enqueue(new Entry(category, logLevel, formatter(state, exception)));
        }
    }
}
