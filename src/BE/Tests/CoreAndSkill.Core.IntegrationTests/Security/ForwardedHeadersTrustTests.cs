using System.Net;
using CoreAndSkill.Core.IntegrationTests.Support;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Security;

// docs/quy-uoc/be-architecture.md §3.1 ràng buộc 4, be-api-controller.md §6.3 ràng buộc 1. Header chuyển tiếp chỉ
// được tin khi kết nối TCP đến từ một proxy KHAI TƯỜNG MINH (Core:Network:*); danh sách rỗng = không tin ai.
//
// TestServer không đặt RemoteIpAddress — và ForwardedHeadersMiddleware cố ý CHẤP NHẬN bước đầu tiên khi địa chỉ đó
// rỗng (cho máy chủ không hỗ trợ). Để kiểm được vế "không tin", một bộ lọc khởi động đặt địa chỉ kết nối từ header
// thử TRƯỚC toàn bộ pipeline của Core, rồi sau khi pipeline chạy xong ghi lại scheme và IP mà ứng dụng đã thấy.
//
// Triệu chứng thật được kiểm song song: sau reverse proxy TLS, app không nhận ra request là HTTPS thì endpoint phát
// token antiforgery (cookie SecurePolicy.Always) không phát được — không request ghi nào đi qua
// (docs/wiki-core/be/ly-do/be-architecture.md §3.1).
public sealed class ForwardedHeadersTrustTests
{
    private const string Proxy = "10.20.30.40";
    private const string Stranger = "10.20.30.99";
    private const string Client = "203.0.113.7";

    [Fact]
    public async Task DeclaredProxy_ForwardedProtoAndFor_AreApplied()
    {
        var seen = new SeenRequest();
        using var factory = CreateFactory(seen, (NetworkKey("KnownProxies"), Proxy));

        var response = await SendThroughAsync(factory, remoteIp: Proxy);

        seen.Scheme.ShouldBe("https");
        seen.RemoteIp.ShouldBe(IPAddress.Parse(Client));
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeclaredNetwork_ForwardedHeaders_AreApplied()
    {
        var seen = new SeenRequest();
        using var factory = CreateFactory(seen, (NetworkKey("KnownNetworks"), "10.20.30.0/24"));

        await SendThroughAsync(factory, remoteIp: Proxy);

        seen.Scheme.ShouldBe("https");
        seen.RemoteIp.ShouldBe(IPAddress.Parse(Client));
    }

    // Loopback là mục MẶC ĐỊNH của framework — phải bị xoá khi nạp danh sách đã khai, không cộng dồn.
    [Theory]
    [InlineData(Stranger)]
    [InlineData("127.0.0.1")]
    [InlineData("::1")]
    public async Task UndeclaredSource_ForwardedHeaders_AreIgnored(string remoteIp)
    {
        var seen = new SeenRequest();
        using var factory = CreateFactory(seen, (NetworkKey("KnownProxies"), Proxy));

        await SendThroughAsync(factory, remoteIp);

        seen.Scheme.ShouldBe("http");
        seen.RemoteIp.ShouldBe(IPAddress.Parse(remoteIp));
    }

    // Mặc định an toàn: không khai gì thì KHÔNG nguồn nào được tin, kể cả loopback. Bẫy của framework: hai danh sách
    // cùng rỗng mà cờ vẫn bật thì middleware bỏ luôn phép đối chiếu nguồn và tin tất cả.
    [Theory]
    [InlineData(Proxy)]
    [InlineData("127.0.0.1")]
    public async Task NothingDeclared_ForwardedHeaders_AreIgnored(string remoteIp)
    {
        var seen = new SeenRequest();
        using var factory = CreateFactory(seen);

        await SendThroughAsync(factory, remoteIp);

        seen.Scheme.ShouldBe("http");
        seen.RemoteIp.ShouldBe(IPAddress.Parse(remoteIp));
    }

    [Theory]
    [InlineData("KnownProxies", "khong-phai-ip")]
    [InlineData("KnownNetworks", "10.20.30.0")]
    [InlineData("KnownNetworks", "10.20.30.0/99")]
    public void InvalidEntry_PreventsStartup_AndNamesTheKey(string key, string value)
    {
        using var factory = CreateFactory(new SeenRequest(), (NetworkKey(key), value));

        var exception = Should.Throw<Exception>(() => factory.CreateClient());

        Messages(exception).ShouldContain(m => m.Contains(key, StringComparison.Ordinal));
    }

    private static string NetworkKey(string name) => $"Core:Network:{name}:0";

    private static CoreWebApplicationFactory CreateFactory(SeenRequest seen, params (string Key, string Value)[] config)
    {
        var overrides = new Dictionary<string, string?> { ["ConnectionStrings:Core"] = CoreWebApplicationFactory.UnreachableConnectionString };
        foreach (var (key, value) in config)
            overrides[key] = value;

        return new CoreWebApplicationFactory(overrides, services =>
        {
            // Key ring mặc định nằm trong DB (không tới được ở đây) — token antiforgery cần khoá, nên giữ trong bộ nhớ.
            services.AddDataProtection().UseEphemeralDataProtectionProvider();
            services.AddSingleton(seen);
            services.AddTransient<IStartupFilter, RemoteIpFromTestHeader>();
        });
    }

    // HTTP thường (không phải https://) — đúng cảnh reverse proxy kết thúc TLS rồi chuyển tiếp HTTP vào app.
    private static Task<HttpResponseMessage> SendThroughAsync(CoreWebApplicationFactory factory, string remoteIp)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/core/antiforgery/token");
        request.Headers.Add(RemoteIpFromTestHeader.Header, remoteIp);
        request.Headers.Add("X-Forwarded-Proto", "https");
        request.Headers.Add("X-Forwarded-For", Client);
        return client.SendAsync(request);
    }

    private static IEnumerable<string> Messages(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            yield return current.Message;
            if (current is AggregateException aggregate)
            {
                foreach (var inner in aggregate.InnerExceptions)
                {
                    foreach (var message in Messages(inner))
                        yield return message;
                }
            }
        }
    }

    private sealed class SeenRequest
    {
        public string? Scheme { get; set; }

        public IPAddress? RemoteIp { get; set; }
    }

    private sealed class RemoteIpFromTestHeader(SeenRequest seen) : IStartupFilter
    {
        public const string Header = "X-Test-Remote-Ip";

        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use(async (context, nextMiddleware) =>
            {
                if (context.Request.Headers.TryGetValue(Header, out var ip))
                    context.Connection.RemoteIpAddress = IPAddress.Parse(ip.ToString());

                try
                {
                    await nextMiddleware(context);
                }
                finally
                {
                    // ForwardedHeadersMiddleware sửa tại chỗ và không hoàn lại — sau pipeline là thứ ứng dụng đã thấy.
                    seen.Scheme = context.Request.Scheme;
                    seen.RemoteIp = context.Connection.RemoteIpAddress;
                }
            });
            next(app);
        };
    }
}
