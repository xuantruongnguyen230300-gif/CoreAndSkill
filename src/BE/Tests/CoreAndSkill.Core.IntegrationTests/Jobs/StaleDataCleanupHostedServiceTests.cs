using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Application.Configuration;
using CoreAndSkill.Core.Application.Diagnostics;
using CoreAndSkill.Core.Application.Maintenance;
using CoreAndSkill.Core.Infrastructure.DependencyInjection;
using CoreAndSkill.Core.Infrastructure.Execution;
using CoreAndSkill.Core.Infrastructure.Jobs;
using CoreAndSkill.Tests.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Jobs;

// RunOnceAsync không chạm DB — kiểm bằng IStaleDataCleaner giả, không cần Postgres.
//
// Job lấy từ ĐÚNG lời đăng ký của Core (AddCoreBackgroundJobs) trên một container bật ValidateScopes — như Development.
// Dựng tay bằng `new` thì không test nào thấy được job giữ cleaner Scoped trong một Singleton
// (docs/quy-uoc/be-architecture.md §5.2 "luật cứng").
public sealed class StaleDataCleanupHostedServiceTests
{
    private sealed class CountingCleaner(int deletedCount) : IStaleDataCleaner
    {
        public string Name => "counting-cleaner";
        public int CallCount { get; private set; }

        public Task<int> CleanAsync(CancellationToken ct)
        {
            CallCount++;
            return Task.FromResult(deletedCount);
        }
    }

    private sealed class ThrowingCleaner : IStaleDataCleaner
    {
        public string Name => "throwing-cleaner";

        public Task<int> CleanAsync(CancellationToken ct)
            => throw new InvalidOperationException("lỗi giả lập");
    }

    // Hết thời gian chờ của một lời gọi bên trong cleaner (vd. HttpClient.Timeout) đi ra thành TaskCanceledException bọc
    // TimeoutException — trong khi job CHƯA được yêu cầu dừng.
    private sealed class TimingOutCleaner : IStaleDataCleaner
    {
        public string Name => "timing-out-cleaner";

        public Task<int> CleanAsync(CancellationToken ct)
            => throw new TaskCanceledException("", new TimeoutException());
    }

    // Tiến trình dừng đúng lúc cleaner đang chạy: token của lượt quét huỷ, cleaner ném theo token đó.
    private sealed class StoppingCleaner(CancellationTokenSource stopping) : IStaleDataCleaner
    {
        public string Name => "stopping-cleaner";

        public Task<int> CleanAsync(CancellationToken ct)
        {
            stopping.Cancel();
            throw new OperationCanceledException(stopping.Token);
        }
    }

    // Đóng vai DbContext của một module: Scoped, bị huỷ khi phạm vi đóng.
    private sealed class ScopedResource : IDisposable
    {
        public bool Disposed { get; private set; }

        public void Dispose() => Disposed = true;
    }

    // Đúng hình dạng một cleaner hạ nguồn thật: Scoped vì nó cần DbContext (ở đây là ScopedResource).
    private sealed class ScopedCleaner(ScopedResource resource, List<ScopedCleaner> seen) : IStaleDataCleaner
    {
        public string Name => "scoped-cleaner";

        public ScopedResource Resource { get; } = resource;

        public bool ResourceWasAliveDuringClean { get; private set; }

        public Task<int> CleanAsync(CancellationToken ct)
        {
            ResourceWasAliveDuringClean = !Resource.Disposed;
            seen.Add(this);
            return Task.FromResult(0);
        }
    }

    private static ServiceProvider BuildProvider(Action<IServiceCollection> registerCleaners)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(TimeProvider.System);
        services.AddOptions<CoreBackgroundJobOptions>();
        services.AddOptions<CoreJobOptions>();
        services.AddSingleton<IExecutionContextScope, ExecutionContextScope>();
        registerCleaners(services);
        services.AddCoreBackgroundJobs();

        // ValidateScopes — đúng thứ Development bật: Singleton tiêu thụ Scoped thì ném ngay lúc resolve.
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    private static StaleDataCleanupHostedService Job(IServiceProvider provider)
        => provider.GetServices<IHostedService>().OfType<StaleDataCleanupHostedService>().Single();

    [Fact]
    public async Task RunOnceAsync_NoCleaners_DoesNothing()
    {
        await using var provider = BuildProvider(_ => { });

        await Job(provider).RunOnceAsync(CancellationToken.None); // không ném là đủ — không có gì để kiểm thêm
    }

    [Fact]
    public async Task RunOnceAsync_InvokesEveryRegisteredCleaner()
    {
        var cleaner = new CountingCleaner(deletedCount: 3);
        await using var provider = BuildProvider(services => services.AddSingleton<IStaleDataCleaner>(cleaner));

        await Job(provider).RunOnceAsync(CancellationToken.None);

        cleaner.CallCount.ShouldBe(1);
    }

    [Fact]
    public async Task RunOnceAsync_OneCleanerThrows_OthersStillRun()
    {
        var counting = new CountingCleaner(deletedCount: 1);
        await using var provider = BuildProvider(services =>
        {
            services.AddSingleton<IStaleDataCleaner>(new ThrowingCleaner());
            services.AddSingleton<IStaleDataCleaner>(counting);
        });

        await Job(provider).RunOnceAsync(CancellationToken.None); // KHÔNG ném ra ngoài — một cleaner lỗi không chặn cleaner khác

        counting.CallCount.ShouldBe(1);
    }

    // "Đang tắt" là token của CHÍNH vòng quét đã huỷ, không phải "ngoại lệ có kiểu OperationCanceledException". Một
    // TaskCanceledException vì hết thời gian chờ mà thoát khỏi RunOnceAsync thì BackgroundService hỏng, và host mặc định
    // (BackgroundServiceExceptionBehavior.StopHost) dừng cả tiến trình.
    [Fact]
    public async Task RunOnceAsync_ACleanerTimingOutAsTaskCanceled_WhileNotStopping_IsThatCleanersFailure_NotAShutdown()
    {
        var counting = new CountingCleaner(deletedCount: 1);
        await using var provider = BuildProvider(services =>
        {
            services.AddSingleton<IStaleDataCleaner>(new TimingOutCleaner());
            services.AddSingleton<IStaleDataCleaner>(counting);
        });
        using var probe = new MeterProbe(CoreMetrics.BackgroundJobFailed);

        await Job(provider).RunOnceAsync(CancellationToken.None); // KHÔNG ném — vòng quét sống tiếp

        counting.CallCount.ShouldBe(1, "cleaner đứng sau vẫn phải chạy");
        probe.Count(CoreMetrics.BackgroundJobFailed.Name).ShouldBe(1, "cleaner hết giờ không được hỏng im lặng");
    }

    // Chiều ngược lại — thiếu nó thì một bộ lọc nuốt MỌI ngoại lệ cũng làm test trên xanh.
    [Fact]
    public async Task RunOnceAsync_WhenItsOwnTokenIsCancelled_PropagatesTheCancellation_AndRunsNothingAfter()
    {
        using var stopping = new CancellationTokenSource();
        var counting = new CountingCleaner(deletedCount: 1);
        await using var provider = BuildProvider(services =>
        {
            services.AddSingleton<IStaleDataCleaner>(new StoppingCleaner(stopping));
            services.AddSingleton<IStaleDataCleaner>(counting);
        });

        await Should.ThrowAsync<OperationCanceledException>(() => Job(provider).RunOnceAsync(stopping.Token));

        counting.CallCount.ShouldBe(0);
    }

    // Cleaner hạ nguồn cần DbContext nên đăng ký Scoped. Job là Singleton: nó phải mở MỘT phạm vi mới cho mỗi lượt quét,
    // resolve cleaner trong phạm vi đó, và đóng phạm vi khi xong — nếu không, Development không khởi động được
    // (ValidateScopes) còn Production giữ một DbContext sống suốt tiến trình, không có đơn vị nào trong ngữ cảnh.
    [Fact]
    public async Task ScopedCleaner_IsResolvedInAFreshScope_ForEachRun()
    {
        var seen = new List<ScopedCleaner>();
        await using var provider = BuildProvider(services =>
        {
            services.AddSingleton(seen);
            services.AddScoped<ScopedResource>();
            services.AddScoped<IStaleDataCleaner, ScopedCleaner>();
        });

        var job = Job(provider);
        await job.RunOnceAsync(CancellationToken.None);
        await job.RunOnceAsync(CancellationToken.None);

        seen.Count.ShouldBe(2, "chống test rỗng: cleaner Scoped phải thật sự được gọi ở cả hai lượt");
        seen[0].ShouldNotBeSameAs(seen[1], "hai lượt quét dùng chung một cleaner — phạm vi không được mở mới");
        seen.ShouldAllBe(c => c.ResourceWasAliveDuringClean);
        seen.ShouldAllBe(c => c.Resource.Disposed, "phạm vi của lượt quét không được đóng sau khi xong");
    }
}
