using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Application.Files;
using CoreAndSkill.Core.Application.Import;
using CoreAndSkill.Core.Application.Jobs;
using CoreAndSkill.Core.Application.Notifications;
using CoreAndSkill.Core.Application.Outbox;
using CoreAndSkill.Core.Application.Tabular;
using CoreAndSkill.Core.Infrastructure.Files;
using CoreAndSkill.Core.Infrastructure.Jobs;
using CoreAndSkill.Core.Infrastructure.Outbox;
using CoreAndSkill.Core.IntegrationTests.Support;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Web;

// Lắp ghép của pha B4 trong host thật: thứ tự khởi động của các dịch vụ nền, health check của outbox, và các seam mà
// Core KHÔNG có bản mặc định (email, mẫu thông báo…) — thiếu bản của module thì phải nổ to, không im lặng bỏ qua.
public sealed class B4HostWiringTests : IClassFixture<InMemoryHostFixture>
{
    private readonly IServiceProvider _services;

    public B4HostWiringTests(InMemoryHostFixture fixture)
        => _services = fixture.Host.Factory.Services;

    [Fact]
    public void JobRecovery_StartsBeforeTheOutboxDispatcher_BecauseTheOtherOrderMarksLiveJobsAsInterrupted()
    {
        var order = _services.GetServices<IHostedService>().Select(s => s.GetType()).ToList();

        var recovery = order.IndexOf(typeof(JobRecoveryHostedService));
        var dispatcher = order.IndexOf(typeof(OutboxDispatcherHostedService));

        recovery.ShouldBeGreaterThanOrEqualTo(0, "JobRecoveryHostedService không được đăng ký");
        dispatcher.ShouldBeGreaterThanOrEqualTo(0, "OutboxDispatcherHostedService không được đăng ký");
        recovery.ShouldBeLessThan(dispatcher);
    }

    [Fact]
    public void EveryB4BackgroundService_IsRegistered()
    {
        var registered = _services.GetServices<IHostedService>().Select(s => s.GetType()).ToHashSet();

        registered.ShouldContain(typeof(BackgroundJobQueueHostedService));
        registered.ShouldContain(typeof(JobRecoveryHostedService));
        registered.ShouldContain(typeof(OutboxDispatcherHostedService));
        registered.ShouldContain(typeof(FileMaintenanceHostedService));
        registered.ShouldContain(typeof(FilePurposeCatalogValidationHostedService));
    }

    // Seam động (T7) — RULES.md S15, ADR-0050: phép cấm tệp nén nằm trong FilePurposeCatalog, nhưng nó chỉ chặn được
    // tiến trình khởi động khi hosted service dựng catalog LÚC KHỞI ĐỘNG được nối vào DI. Thiếu đăng ký thì catalog chỉ
    // dựng ở lần tải tệp đầu tiên — purpose nhận zip lộ ra ở người dùng, không ở lúc triển khai.
    [Fact]
    public void FilePurposeCatalogValidationHostedService_IsRegistered_InTheContainer()
        => _services.GetServices<IHostedService>()
            .ShouldContain(service => service is FilePurposeCatalogValidationHostedService,
                "Không có hosted service dựng catalog purpose lúc khởi động — phép cấm zip (S15) là luật ép bằng niềm tin.");

    [Fact]
    public void TheOutboxHealthCheck_IsRegistered_UnderTheReadyTag()
    {
        var registrations = _services.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations;

        var outbox = registrations.SingleOrDefault(r => r.Name == "outbox");

        outbox.ShouldNotBeNull("Outbox có dòng `dead` mà /health/ready không hiện ra là một thông báo không bao giờ tới mà không ai biết");
        outbox.Tags.ShouldContain("ready");
    }

    [Fact]
    public void ThereIsNoEmailSenderByDefault_TheModuleMustSupplyOne_SoNothingIsSilentlyDropped()
    {
        _services.GetService<IEmailSender>().ShouldBeNull(
            "Core không chọn nhà cung cấp email (quyết định kiến trúc) — có bản mặc định 'nuốt thư' là lỗi im lặng");
    }

    [Fact]
    public async Task TheDefaultTemplateRenderer_FailsLoudly_ForAnUnknownTemplate_NeverAnEmptyMail()
    {
        using var scope = _services.CreateScope();
        var renderer = scope.ServiceProvider.GetRequiredService<INotificationTemplateRenderer>();

        var result = await renderer.RenderAsync("template.khong.ton.tai", "vi", new Dictionary<string, string>(), default);

        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe(NotificationErrors.TemplateMissing.Code);
    }

    [Fact]
    public void TheCoreSeamsOfB4_AreAllResolvable_InAScope()
    {
        using var scope = _services.CreateScope();
        var sp = scope.ServiceProvider;

        sp.GetRequiredService<IFileStorage>().ShouldNotBeNull();
        sp.GetRequiredService<IFileRepository>().ShouldNotBeNull();
        sp.GetRequiredService<FileAccessPolicy>().ShouldNotBeNull();
        sp.GetRequiredService<FilePurposeCatalog>().ShouldNotBeNull();
        sp.GetRequiredService<IJobRepository>().ShouldNotBeNull();
        sp.GetRequiredService<IBackgroundJobScheduler>().ShouldNotBeNull();
        sp.GetRequiredService<INotificationRepository>().ShouldNotBeNull();
        sp.GetRequiredService<INotificationPublisher>().ShouldNotBeNull();
        sp.GetRequiredService<ITabularReader>().ShouldNotBeNull();
        sp.GetRequiredService<ITabularWriterFactory>().ShouldNotBeNull();
        sp.GetRequiredService<IImportRowWriter>().ShouldNotBeNull();
        sp.GetRequiredService<IOutboxReplayService>().ShouldNotBeNull();
        sp.GetRequiredService<INotificationPreferences>().ShouldNotBeNull();
    }

    [Fact]
    public void TheOutboxHandlers_OfCore_AreRegistered_ForTheJobEvents()
    {
        using var scope = _services.CreateScope();

        var handlers = scope.ServiceProvider.GetServices<IOutboxEventHandler>().ToList();

        handlers.ShouldNotBeEmpty();
        handlers.Select(h => h.EventType).ShouldContain("core.job.queued.v1");
        handlers.Select(h => h.EventType).ShouldContain("core.job.finished.v1");
    }

    [Fact]
    public void OnlyOneHandlerOwnsEachEventType_SoADispatchIsNeverAmbiguous()
    {
        using var scope = _services.CreateScope();

        var duplicates = scope.ServiceProvider.GetServices<IOutboxEventHandler>()
            .GroupBy(h => h.EventType)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        duplicates.ShouldBeEmpty();
    }
}
