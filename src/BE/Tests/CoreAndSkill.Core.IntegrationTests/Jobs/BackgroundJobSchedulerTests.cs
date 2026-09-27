using System.Threading.Channels;
using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Infrastructure.Jobs;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Jobs;

// BackgroundJobScheduler không chạm DB — kiểm được bằng Channel trong bộ nhớ, không cần Postgres.
// Đặt ở IntegrationTests (không phải Core.UnitTests) vì kiểu internal của Core.Infrastructure —
// InternalsVisibleTo chỉ khai cho project này (docs/quy-uoc/be-architecture.md §9.1).
public sealed class BackgroundJobSchedulerTests
{
    public sealed class TestJob
    {
        public bool WasCalled { get; private set; }

        public Task RunAsync(CancellationToken ct)
        {
            WasCalled = true;
            return Task.CompletedTask;
        }
    }

    // Vòng đời theo bảng docs/quy-uoc/be-architecture.md §5.2 (hàng Transient).
    [Fact]
    public void AddCoreBackgroundJobs_RegistersScheduler_AsTransient()
    {
        var services = new ServiceCollection();
        CoreAndSkill.Core.Infrastructure.DependencyInjection.CoreInfrastructureServiceCollectionExtensions
            .AddCoreBackgroundJobs(services);

        services.Single(d => d.ServiceType == typeof(IBackgroundJobScheduler)).Lifetime
            .ShouldBe(ServiceLifetime.Transient);
    }

    // Phạm vi mà bộ phát outbox mở trước khi gọi bên nhận JobQueuedEvent — lời gọi hợp lệ duy nhất của scheduler.
    private static IExecutionContextScope ScopeOpenedByTheOutboxDispatcher(Guid? userId = null)
    {
        var executionScope = Substitute.For<IExecutionContextScope>();
        executionScope.Current.Returns(new ExecutionContextSnapshot(userId, userId is null ? null : "an", Guid.NewGuid()));
        return executionScope;
    }

    [Fact]
    public async Task EnqueueAsync_WritesRunnableJob_ToChannel()
    {
        var channel = Channel.CreateUnbounded<QueuedJob>();
        var executionScope = ScopeOpenedByTheOutboxDispatcher(Guid.NewGuid());
        var scheduler = new BackgroundJobScheduler(channel, executionScope);
        var testJob = new TestJob();
        var services = new ServiceCollection().AddSingleton(testJob).BuildServiceProvider();

        var jobId = await scheduler.EnqueueAsync<TestJob>((j, ct) => j.RunAsync(ct));

        jobId.ShouldNotBeNullOrWhiteSpace();
        channel.Reader.TryRead(out var queued).ShouldBeTrue();
        queued!.JobId.ShouldBe(jobId);

        await queued.Run(services, CancellationToken.None);
        testJob.WasCalled.ShouldBeTrue();
    }

    [Fact]
    public async Task EnqueueAsync_CapturesAmbientExecutionScope_AtEnqueueTime()
    {
        var channel = Channel.CreateUnbounded<QueuedJob>();
        var executionScope = Substitute.For<IExecutionContextScope>();
        var tenantId = Guid.NewGuid();
        executionScope.Current.Returns(new ExecutionContextSnapshot(Guid.NewGuid(), "an", tenantId));
        var scheduler = new BackgroundJobScheduler(channel, executionScope);

        await scheduler.EnqueueAsync<TestJob>((j, ct) => j.RunAsync(ct));

        channel.Reader.TryRead(out var queued).ShouldBeTrue();
        queued!.Scope!.TenantId.ShouldBe(tenantId);
    }

    [Fact]
    public async Task ScheduleAsync_WithDelay_DoesNotWriteImmediately()
    {
        var channel = Channel.CreateUnbounded<QueuedJob>();
        var executionScope = ScopeOpenedByTheOutboxDispatcher(Guid.NewGuid());
        var scheduler = new BackgroundJobScheduler(channel, executionScope);

        await scheduler.ScheduleAsync<TestJob>((j, ct) => j.RunAsync(ct), TimeSpan.FromMinutes(5));

        channel.Reader.TryRead(out _).ShouldBeFalse();
    }

    // ===== Phòng thủ: gọi ngoài phạm vi ngữ cảnh thực thi ⇒ từ chối, không đẩy gì vào hàng đợi =====
    // Trong một request HTTP đơn vị đến từ claim (HttpContextTenantContext), nên Current là null. Chụp null thì việc nền chạy
    // KHÔNG đơn vị: đọc trả rỗng "thành công", ghi ném. Lời gọi đó là vi phạm be-cqrs-handler.md §10.3 ràng buộc 3.

    [Fact]
    public async Task EnqueueAsync_OutsideAnExecutionScope_IsRefused_AndQueuesNothing()
    {
        var channel = Channel.CreateUnbounded<QueuedJob>();
        var executionScope = Substitute.For<IExecutionContextScope>();
        executionScope.Current.Returns((ExecutionContextSnapshot?)null);
        var scheduler = new BackgroundJobScheduler(channel, executionScope);

        var error = await Should.ThrowAsync<InvalidOperationException>(
            () => scheduler.EnqueueAsync<TestJob>((j, ct) => j.RunAsync(ct)));

        error.Message.ShouldContain(nameof(TestJob));
        channel.Reader.TryRead(out _).ShouldBeFalse();
    }

    // Lối hẹn giờ phải bị chặn NGAY lúc gọi — không phải lúc hẹn giờ trôi qua, khi request đã xong từ lâu và exception
    // rơi vào một Task không ai chờ.
    [Fact]
    public async Task ScheduleAsync_OutsideAnExecutionScope_IsRefused_AtCallTime()
    {
        var channel = Channel.CreateUnbounded<QueuedJob>();
        var executionScope = Substitute.For<IExecutionContextScope>();
        executionScope.Current.Returns((ExecutionContextSnapshot?)null);
        var scheduler = new BackgroundJobScheduler(channel, executionScope);

        await Should.ThrowAsync<InvalidOperationException>(
            () => scheduler.ScheduleAsync<TestJob>((j, ct) => j.RunAsync(ct), TimeSpan.FromMilliseconds(1)));
    }

    // Đối chứng: việc HỆ THỐNG (dòng outbox không có người kích hoạt) vẫn được nhận — phạm vi có đơn vị, người là null.
    // Phép từ chối ở trên KHÔNG được biến thành "phải có người dùng".
    [Fact]
    public async Task EnqueueAsync_SystemTriggeredScope_WithoutAUser_IsAccepted()
    {
        var channel = Channel.CreateUnbounded<QueuedJob>();
        var executionScope = ScopeOpenedByTheOutboxDispatcher(userId: null);
        var scheduler = new BackgroundJobScheduler(channel, executionScope);

        await scheduler.EnqueueAsync<TestJob>((j, ct) => j.RunAsync(ct));

        channel.Reader.TryRead(out var queued).ShouldBeTrue();
        queued!.Scope!.UserId.ShouldBeNull();
        queued.Scope.TenantId.ShouldNotBeNull();
    }
}
