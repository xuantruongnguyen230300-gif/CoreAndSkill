using System.Reflection;
using CoreAndSkill.Core.Application.Auth;
using CoreAndSkill.Core.Application.Common.Behaviors;
using CoreAndSkill.Core.Application.Common.Cqrs;
using CoreAndSkill.Core.Application.Files;
using CoreAndSkill.Core.Application.Import;
using CoreAndSkill.Core.Application.Jobs;
using CoreAndSkill.Core.Application.Notifications;
using CoreAndSkill.Core.Application.Outbox;
using CoreAndSkill.Core.Application.Users;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CoreAndSkill.Core.Application;

// Core.Application: MediatR + validator + hai pipeline behavior — MỘT lời quét cho Core và mọi
// module đã ghi nhận assembly (docs/quy-uoc/be-cqrs-handler.md §5.1,
// docs/adr/0025-luu-du-lieu-module-mot-transaction.md). Thứ tự đăng ký behavior là thứ tự chạy:
// Validation TRƯỚC (ngoài cùng), Transaction SAU.
public static class DependencyInjection
{
    public static IServiceCollection AddCoreApplication(this IServiceCollection services)
    {
        var moduleAssemblies = services.SealModuleAssemblies();

        Assembly[] assemblies = [typeof(ICommandBase).Assembly, .. moduleAssemblies];

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblies(assemblies);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>)); // 1 — chạy TRƯỚC
            cfg.AddOpenBehavior(typeof(TransactionBehavior<,>)); // 2 — chạy SAU
        });

        // includeInternalTypes: true là BẮT BUỘC — validator của Core là `internal sealed` (§6.1).
        services.AddValidatorsFromAssemblies(assemblies, ServiceLifetime.Transient, includeInternalTypes: true);

        // Composition nội bộ Application dùng chung ở nhiều handler auth — không phải seam
        // (không interface, không hiện thực ở tầng khác).
        services.AddScoped<SessionDtoFactory>();

        // Năm luật bảo vệ tài khoản quản trị, ĐÚNG MỘT CHỖ — docs/contracts/users.md §2.
        services.AddScoped<UserPrivilegeGuard>();

        services.AddCoreB4Application();

        return services;
    }

    // Pha B4 — tệp, việc chạy nền, outbox, thông báo, nhập/xuất. Tách khỏi AddCoreApplication để đọc
    // được nó gồm những gì; vẫn là MỘT nhóm đăng ký cùng project với các kiểu nó đăng ký
    // (docs/quy-uoc/be-architecture.md §5.1 "project nào sở hữu kiểu thì project đó sở hữu lời đăng ký").
    private static void AddCoreB4Application(this IServiceCollection services)
    {
        // Tệp: catalog purpose gộp mọi nguồn (module khai); quyền tệp = quyền bản ghi chủ.
        services.AddSingleton<FilePurposeCatalog>();
        services.AddScoped<FileAccessPolicy>();
        services.AddScoped<IFileAttachment, FileAttachment>();
        services.AddScoped<IFileOwnerAccessChecker, JobFileOwnerAccessChecker>();

        // Việc chạy nền + bên nhận outbox của chúng. Bên nhận là Scoped: dùng scheduler / publisher Scoped.
        services.AddScoped<JobRunner>();
        services.AddScoped<IJobExecutor, ImportJobExecutor>();
        services.AddScoped<IOutboxEventHandler, JobQueuedOutboxHandler>();
        services.AddScoped<IOutboxEventHandler, JobFinishedOutboxHandler>();

        // Thông báo: hai kênh, một bộ chọn (12-notifications.md §3.2). TryAdd cho hai hiện thực mặc định
        // để dự án đè bằng của mình — dòng đăng ký của module đứng TRƯỚC AddCore nên nó thắng.
        services.AddScoped<INotificationPublisher, NotificationPublisher>();
        services.AddScoped<INotificationChannel, InAppNotificationChannel>();
        services.AddScoped<INotificationChannel, EmailNotificationChannel>();
        services.TryAddSingleton<INotificationPreferences, AlwaysEnabledNotificationPreferences>();
        services.TryAddSingleton<INotificationTemplateRenderer, MissingTemplateRenderer>();
    }
}
