using System.Net.Sockets;
using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using NSubstitute;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Persistence;

// Luật E11 (docs/RULES.md §4) — ghim phân loại "thử lại / không thử lại" của strategy Core, định nghĩa gốc ở
// docs/quy-uoc/be-performance.md §7.2 mục "Lỗi KHÔNG được thử lại". Ngoại lệ dựng bằng tay, không database, không Docker.
// Nằm ở IntegrationTests chứ không ở UnitTests: strategy là internal của Core.Infrastructure, và UnitTests chỉ tham
// chiếu Domain + Application (docs/quy-uoc/be-architecture.md, kết cấu thư mục test).
//
// Hai chiều đều ghim: chiều "không thử lại" bắt một lần nới luật; chiều "vẫn thử lại" bắt một lần nâng Npgsql làm hẹp
// phân loại tạm thời, và bắt một bản strategy tắt thử lại hẳn — thứ làm chiều kia xanh rỗng.
//
// Strategy dựng với trễ tối đa 1 ms: phân loại không phụ thuộc trễ, và test không phải chờ nhịp lùi thật.
public sealed class CoreExecutionStrategyTests
{
    private const int MaxRetryCount = 3;

    [Theory]
    [InlineData("lock_timeout")]
    [InlineData("lock_timeout_in_DbUpdateException")]
    [InlineData("lock_timeout_in_InvalidOperationException")]
    [InlineData("command_timeout")]
    [InlineData("command_timeout_in_DbUpdateException")]
    [InlineData("command_timeout_in_InvalidOperationException")]
    public void CoreExecutionStrategy_DoesNotRetry_LockTimeoutOrCommandTimeout(string kind)
    {
        var failure = Build(kind);

        // Chống test rỗng: nếu phân loại gốc của Npgsql coi lỗi dạng trần là KHÔNG tạm thời thì ca này xanh mà không
        // chứng minh được gì về strategy của Core.
        ((NpgsqlException)Unwrap(failure)).IsTransient.ShouldBeTrue(
            "phân loại gốc của Npgsql phải coi lỗi này là tạm thời — nếu không, test không kiểm gì");

        var attempts = Run(failure, out var thrown);

        attempts.ShouldBe(1, $"strategy Core đã thử lại '{kind}'");
        thrown.ShouldBeSameAs(failure);
    }

    [Theory]
    [InlineData("transport_io")]
    [InlineData("transport_socket")]
    [InlineData("deadlock_40P01")]
    [InlineData("serialization_40001")]
    [InlineData("connection_failure_08006")]
    [InlineData("admin_shutdown_57P01")]
    [InlineData("deadlock_in_DbUpdateException")]
    public void CoreExecutionStrategy_Retries_TransportAndDeadlockErrors(string kind)
    {
        var attempts = Run(Build(kind), out var thrown);

        attempts.ShouldBe(MaxRetryCount + 1, $"strategy Core không thử lại '{kind}'");
        thrown.ShouldBeOfType<RetryLimitExceededException>();
    }

    // Lỗi không tạm thời trong phân loại gốc vẫn không tạm thời — strategy chỉ bớt, không thêm.
    [Theory]
    [InlineData("unique_violation_23505")]
    [InlineData("query_canceled_57014")]
    public void CoreExecutionStrategy_DoesNotRetry_NonTransientErrors(string kind)
    {
        var failure = Build(kind);

        Run(failure, out var thrown).ShouldBe(1);
        thrown.ShouldBeSameAs(failure);
    }

    private static Exception Build(string kind) => kind switch
    {
        "lock_timeout" => Postgres("55P03"),
        "lock_timeout_in_DbUpdateException" => new DbUpdateException("lưu hỏng", Postgres("55P03")),
        "lock_timeout_in_InvalidOperationException" => new InvalidOperationException("bọc", Postgres("55P03")),
        "command_timeout" => CommandTimeout(),
        "command_timeout_in_DbUpdateException" => new DbUpdateException("lưu hỏng", CommandTimeout()),
        "command_timeout_in_InvalidOperationException" => new InvalidOperationException("bọc", CommandTimeout()),
        "transport_io" => new NpgsqlException("Đứt kết nối.", new IOException("mất gói")),
        "transport_socket" => new NpgsqlException("Đứt kết nối.", new SocketException((int)SocketError.ConnectionReset)),
        "deadlock_40P01" => Postgres("40P01"),
        "serialization_40001" => Postgres("40001"),
        "connection_failure_08006" => Postgres("08006"),
        "admin_shutdown_57P01" => Postgres("57P01"),
        "deadlock_in_DbUpdateException" => new DbUpdateException("lưu hỏng", Postgres("40P01")),
        "unique_violation_23505" => Postgres("23505"),
        "query_canceled_57014" => Postgres("57014"),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };

    private static PostgresException Postgres(string sqlState) => new("mô phỏng", "ERROR", "ERROR", sqlState);

    // Hình dạng Npgsql dùng khi hết CommandTimeout phía client. Hình dạng thật trên PostgreSQL được xác nhận ở
    // PersistenceTimeoutDatabaseTests (RequiresDocker).
    private static NpgsqlException CommandTimeout() => new("Exception while reading from stream", new TimeoutException());

    private static Exception Unwrap(Exception e) => e is DbUpdateException or InvalidOperationException ? e.InnerException! : e;

    private static int Run(Exception failure, out Exception thrown)
    {
        using var db = CreateContext();
        var strategy = db.Database.CreateExecutionStrategy();
        strategy.ShouldBeOfType<CoreExecutionStrategy>();

        var attempts = 0;
        thrown = Should.Throw<Exception>(() => strategy.Execute(() =>
        {
            attempts++;
            throw failure;
        }));

        return attempts;
    }

    private static CoreDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<CoreDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=offline;Username=x;Password=x", npgsql => npgsql.ExecutionStrategy(
                dependencies => new CoreExecutionStrategy(dependencies, MaxRetryCount, TimeSpan.FromMilliseconds(1))))
            .UseSnakeCaseNamingConvention()
            .Options;

        return new CoreDbContext(options, Substitute.For<ITenantContext>());
    }
}
