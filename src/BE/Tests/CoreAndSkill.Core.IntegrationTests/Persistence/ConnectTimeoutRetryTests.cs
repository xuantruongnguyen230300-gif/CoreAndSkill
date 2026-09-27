using System.Net;
using System.Net.Sockets;
using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NSubstitute;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Persistence;

// Luật E11 phần hết hạn MỞ KẾT NỐI — docs/adr/0055-commit-khong-nhan-token-huy-va-het-han-mo-ket-noi-khong-thu-lai.md
// quyết định 2: máy chủ không trả lời trong thời hạn mở kết nối thì strategy của Core KHÔNG thử lại. Lớp 1
// (CoreExecutionStrategyTests) ghim phân loại theo hình dạng dựng tay; test này cho Npgsql THẬT gặp một máy chủ nhận kết
// nối TCP rồi im lặng — đúng ca "không trả lời trong thời hạn" — để thấy hình dạng mà Npgsql thật ném ra.
//
// Không cần Docker: "máy chủ" là một TcpListener trong tiến trình test, chấp nhận kết nối và không gửi byte nào.
public sealed class ConnectTimeoutRetryTests
{
    [Fact]
    public async Task ConnectTimeout_SurfacesAsNpgsqlExceptionWrappingTimeout_AndIsNotRetried()
    {
        using var silentServer = new TcpListener(IPAddress.Loopback, 0);
        silentServer.Start();
        var accepted = new List<Socket>();
        using var stop = new CancellationTokenSource();
        var acceptLoop = AcceptAndStaySilentAsync(silentServer, accepted, stop.Token);

        var connectionString = new NpgsqlConnectionStringBuilder
        {
            Host = IPAddress.Loopback.ToString(),
            Port = ((IPEndPoint)silentServer.LocalEndpoint).Port,
            Database = "silent",
            Username = "x",
            Password = "x",
            Timeout = 1,
            Pooling = false,
        }.ConnectionString;

        await using var context = new CoreDbContext(
            new DbContextOptionsBuilder<CoreDbContext>()
                .UseNpgsql(connectionString, npgsql => npgsql.ExecutionStrategy(dependencies => new CoreExecutionStrategy(
                    dependencies, CoreExecutionStrategy.CoreMaxRetryCount, TimeSpan.FromMilliseconds(1))))
                .UseSnakeCaseNamingConvention()
                .Options,
            Substitute.For<ITenantContext>());

        var strategy = context.Database.CreateExecutionStrategy();
        strategy.ShouldBeOfType<CoreExecutionStrategy>();

        var attempts = 0;
        var thrown = await Should.ThrowAsync<Exception>(() => strategy.ExecuteAsync(async () =>
        {
            attempts++;
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
        }));

        await stop.CancelAsync();
        silentServer.Stop();
        await acceptLoop;
        foreach (var socket in accepted)
            socket.Dispose();

        accepted.Count.ShouldBeGreaterThan(0, "chống test rỗng: Npgsql phải thật sự tới được máy chủ im lặng");
        attempts.ShouldBe(1, $"hết hạn mở kết nối đã bị thử lại — hình dạng thật: {thrown}");
        thrown.ShouldBeOfType<NpgsqlException>($"hình dạng thật: {thrown}");
        thrown.InnerException.ShouldBeOfType<TimeoutException>($"hình dạng thật: {thrown}");
    }

    private static async Task AcceptAndStaySilentAsync(TcpListener listener, List<Socket> accepted, CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
                accepted.Add(await listener.AcceptSocketAsync(ct));
        }
        catch (Exception ex) when (ex is OperationCanceledException or SocketException or ObjectDisposedException)
        {
            // Dừng nghe — hết việc.
        }
    }
}
