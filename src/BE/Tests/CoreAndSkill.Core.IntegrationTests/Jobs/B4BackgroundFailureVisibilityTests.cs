using System.Data;
using System.Data.Common;
using CoreAndSkill.Core.Application.Diagnostics;
using CoreAndSkill.Core.Application.Files;
using CoreAndSkill.Core.Infrastructure.Files;
using CoreAndSkill.Core.Infrastructure.Jobs;
using CoreAndSkill.Core.Infrastructure.Outbox;
using CoreAndSkill.Core.IntegrationTests.Support;
using CoreAndSkill.Tests.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using NSubstitute;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Jobs;

// "Không có lỗi im lặng" — mỗi nhánh thất bại của dịch vụ nền pha B4 phải để lại BA thứ: dòng log Error, chỉ số
// core.job.failed, và trạng thái trong bảng (dòng outbox nằm nguyên, việc chờ được khôi phục lần sau). Ba thứ đó là
// điều duy nhất người vận hành nhìn thấy khi DB mất kết nối lúc 3 giờ sáng.
//
// Test này chạy KHÔNG cần Docker: host dùng chuỗi kết nối không tới được (CoreWebApplicationFactory mặc định), nên
// mọi truy vấn DB của dịch vụ nền ném ngoại lệ — đúng nhánh cần kiểm. Nhánh "DB tới được nhưng dòng lỗi" là việc của
// OutboxDatabaseTests (RequiresDocker, chưa chạy).
//
// ĐIỂM MÙ còn lại: kiểm log qua một ILogger thu thập tay, không qua pipeline log của host — nên bộ này không nói
// được dòng log có ra tới đích thật hay không.
//
// Điểm mù thứ hai ĐÃ ĐÓNG: chỉ số trước đây đo qua MeterListener toàn tiến trình nên chỉ dám khẳng định "tăng ít
// nhất một"; nay đếm theo luồng của chính test qua `MeterProbe` (`src/BE/Tests/Shared/MeterProbe.cs`, nối vào project
// này bằng <Compile Include> — ADR-0070) nên khẳng định đúng con số. Lọc theo luồng quan trọng gấp đôi ở đây: host
// trong `CoreWebApplicationFactory` đã khởi động các dịch vụ nền, và chúng cũng phát `core.job.failed`.
public sealed class B4BackgroundFailureVisibilityTests : IDisposable
{
    private readonly CoreWebApplicationFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    private sealed class CollectingLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => Entries.Add((logLevel, formatter(state, exception)));
    }

    // Tên chỉ số đọc ra từ chính instrument, không gõ lại chuỗi — đổi tên bên Core thì khẳng định đi
    // theo, chứ không lặng lẽ đếm 0 trên một cái tên đã chết.
    private static string FailedJobMetric => CoreMetrics.BackgroundJobFailed.Name;

    [Fact]
    public async Task TheOutboxDispatcherTick_WhenTheDatabaseIsUnreachable_DoesNotThrow_LogsAnError_AndCountsTheFailure()
    {
        var logger = new CollectingLogger<OutboxDispatcherHostedService>();
        var service = ActivatorUtilities.CreateInstance<OutboxDispatcherHostedService>(_factory.Services, logger);

        using var probe = new MeterProbe(CoreMetrics.BackgroundJobFailed);

        await service.RunOnceAsync(CancellationToken.None);

        probe.Count(FailedJobMetric).ShouldBe(1);
        logger.Entries.ShouldContain(e => e.Level == LogLevel.Error && e.Message.Contains("outbox", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task RepeatedOutboxTickFailures_AreLoggedThinly_ButEveryOneIsCounted()
    {
        var logger = new CollectingLogger<OutboxDispatcherHostedService>();
        var service = ActivatorUtilities.CreateInstance<OutboxDispatcherHostedService>(_factory.Services, logger);

        using var probe = new MeterProbe(CoreMetrics.BackgroundJobFailed);

        for (var i = 0; i < 3; i++)
            await service.RunOnceAsync(CancellationToken.None);

        probe.Count(FailedJobMetric).ShouldBe(3, "chỉ số đếm MỌI lần");
        logger.Entries.Count(e => e.Level == LogLevel.Error).ShouldBe(1, "log chỉ dòng đầu rồi thưa dần — không ngập log mỗi 5 giây");
    }

    [Fact]
    public async Task TheFileMaintenanceRun_WhenTheDatabaseIsUnreachable_DoesNotThrow_AndReportsEachFailedStep()
    {
        var logger = new CollectingLogger<FileMaintenanceHostedService>();
        var service = ActivatorUtilities.CreateInstance<FileMaintenanceHostedService>(_factory.Services, logger);

        using var probe = new MeterProbe(CoreMetrics.BackgroundJobFailed);

        await service.RunOnceAsync(CancellationToken.None);

        probe.Count(FailedJobMetric).ShouldBe(3, "bốn bước bảo trì, ba bước chạm DB nên hỏng; bước dọn tệp tạm chỉ chạm đĩa nên vẫn xong");
        logger.Entries.ShouldContain(e => e.Level == LogLevel.Error && e.Message.Contains("Bảo trì kho tệp", StringComparison.Ordinal));
    }

    [Fact]
    public async Task TheJobRecoveryAtStartup_WhenTheDatabaseIsUnreachable_DoesNotStopTheProcess_ButSaysSo()
    {
        var logger = new CollectingLogger<JobRecoveryHostedService>();
        var service = ActivatorUtilities.CreateInstance<JobRecoveryHostedService>(_factory.Services, logger);

        using var probe = new MeterProbe(CoreMetrics.BackgroundJobFailed);

        await service.StartAsync(CancellationToken.None);

        probe.Count(FailedJobMetric).ShouldBe(1);
        logger.Entries.ShouldContain(e => e.Level == LogLevel.Error && e.Message.Contains("dở dang", StringComparison.Ordinal));
    }

    // "Đang tắt" là token của CHÍNH lượt chạy đã huỷ, không phải "ngoại lệ có kiểu OperationCanceledException". Một bước
    // hết thời gian chờ (TaskCanceledException bọc TimeoutException) trong lúc tiến trình vẫn chạy là bước HỎNG: báo, rồi
    // chạy bước kế — không thoát khỏi RunOnceAsync làm BackgroundService hỏng và host (StopHost) dừng cả tiến trình.
    [Fact]
    public async Task TheFileMaintenanceRun_WhenAStepTimesOutAsTaskCanceled_WhileNotStopping_ReportsThatStep_AndRunsTheRest()
    {
        var storage = Substitute.For<IFileStorage>();
        storage.PurgeTempAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException<int>(new TaskCanceledException("", new TimeoutException())));
        using var factory = new CoreWebApplicationFactory(configureServices: services =>
        {
            services.RemoveAll<IFileStorage>();
            services.AddSingleton(storage);
            // Bản chạy nền của chính dịch vụ này không được đụng vào bộ kho giả cùng lúc với test.
            services.Remove(services.Single(d => d.ServiceType == typeof(IHostedService) && d.ImplementationType == typeof(FileMaintenanceHostedService)));
        });
        var logger = new CollectingLogger<FileMaintenanceHostedService>();
        var service = ActivatorUtilities.CreateInstance<FileMaintenanceHostedService>(factory.Services, logger);

        await service.RunOnceAsync(CancellationToken.None);

        logger.Entries.ShouldContain(e => e.Level == LogLevel.Error && e.Message.Contains("dọn tệp tạm", StringComparison.Ordinal));
        logger.Entries.ShouldContain(
            e => e.Level == LogLevel.Error && e.Message.Contains("đối soát", StringComparison.Ordinal),
            "bước sau vẫn phải chạy (và hỏng vì DB không tới được)");
    }

    // Cùng khuôn ở phép khôi phục lúc khởi động: lời gọi DB hết thời gian chờ dưới dạng TaskCanceledException trong khi
    // token khởi động CHƯA huỷ không được làm StartAsync ném — ném ở đây là tiến trình không lên.
    [Fact]
    public async Task TheJobRecoveryAtStartup_WhenTheDatabaseCallTimesOutAsTaskCanceled_DoesNotStopTheProcess_ButSaysSo()
    {
        var timingOut = false;
        using var factory = new CoreWebApplicationFactory(configureServices: services =>
            services.AddScoped<DbConnection>(_ => timingOut
                ? new TimingOutConnection()
                : new NpgsqlConnection(CoreWebApplicationFactory.UnreachableConnectionString)));
        _ = factory.Services; // host khởi động trên kết nối thật (không tới được) như mọi test dùng factory mặc định
        timingOut = true;

        var logger = new CollectingLogger<JobRecoveryHostedService>();
        var service = ActivatorUtilities.CreateInstance<JobRecoveryHostedService>(factory.Services, logger);
        using var probe = new MeterProbe(CoreMetrics.BackgroundJobFailed);

        await service.StartAsync(CancellationToken.None);

        probe.Count(FailedJobMetric).ShouldBe(1);
        logger.Entries.ShouldContain(e => e.Level == LogLevel.Error && e.Message.Contains("dở dang", StringComparison.Ordinal));
    }

    // Kết nối mà mọi lần mở đều hết thời gian chờ theo đúng hình dạng HttpClient/Task.WaitAsync để lại: TaskCanceledException
    // bọc TimeoutException, không token nào đã huỷ.
    private sealed class TimingOutConnection : DbConnection
    {
        [System.Diagnostics.CodeAnalysis.AllowNull]
        public override string ConnectionString { get; set; } = string.Empty;

        public override string Database => "het-gio";

        public override string DataSource => "het-gio";

        public override string ServerVersion => "16.0";

        public override ConnectionState State => ConnectionState.Closed;

        public override void ChangeDatabase(string databaseName) => throw new NotSupportedException();

        public override void Close()
        {
        }

        public override void Open() => throw new TaskCanceledException("", new TimeoutException());

        public override Task OpenAsync(CancellationToken cancellationToken)
            => Task.FromException(new TaskCanceledException("", new TimeoutException()));

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => throw new NotSupportedException();

        protected override DbCommand CreateDbCommand() => throw new NotSupportedException("Kết nối chưa bao giờ mở được.");
    }

    [Fact]
    public async Task TheLogLines_NeverCarryTheConnectionString_OrThePassword()
    {
        var logger = new CollectingLogger<OutboxDispatcherHostedService>();
        var service = ActivatorUtilities.CreateInstance<OutboxDispatcherHostedService>(_factory.Services, logger);

        await service.RunOnceAsync(CancellationToken.None);

        logger.Entries.ShouldNotBeEmpty();
        logger.Entries.ShouldAllBe(e => !e.Message.Contains("Password=", StringComparison.OrdinalIgnoreCase));
    }
}
