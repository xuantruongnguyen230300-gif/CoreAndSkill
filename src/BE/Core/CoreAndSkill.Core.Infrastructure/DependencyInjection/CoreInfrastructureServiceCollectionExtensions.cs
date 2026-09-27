using System.Data.Common;
using CoreAndSkill.Core.Application.Auth;
using CoreAndSkill.Core.Application.Audit;
using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Application.Configuration;
using CoreAndSkill.Core.Application.Files;
using CoreAndSkill.Core.Application.Identity;
using CoreAndSkill.Core.Application.Import;
using CoreAndSkill.Core.Application.Jobs;
using CoreAndSkill.Core.Application.Menu;
using CoreAndSkill.Core.Application.Notifications;
using CoreAndSkill.Core.Application.Outbox;
using CoreAndSkill.Core.Application.Permissions;
using CoreAndSkill.Core.Application.Profile;
using CoreAndSkill.Core.Application.Roles;
using CoreAndSkill.Core.Application.Tabular;
using CoreAndSkill.Core.Application.Tenants;
using CoreAndSkill.Core.Application.Users;
using CoreAndSkill.Core.Infrastructure.Audit;
using CoreAndSkill.Core.Infrastructure.Diagnostics;
using CoreAndSkill.Core.Infrastructure.Execution;
using CoreAndSkill.Core.Infrastructure.Files;
using CoreAndSkill.Core.Infrastructure.Identity;
using CoreAndSkill.Core.Infrastructure.Import;
using CoreAndSkill.Core.Infrastructure.Jobs;
using CoreAndSkill.Core.Infrastructure.Menu;
using CoreAndSkill.Core.Infrastructure.Notifications;
using CoreAndSkill.Core.Infrastructure.Outbox;
using CoreAndSkill.Core.Infrastructure.Permissions;
using CoreAndSkill.Core.Infrastructure.Persistence;
using CoreAndSkill.Core.Infrastructure.Persistence.Interceptors;
using CoreAndSkill.Core.Infrastructure.Roles;
using CoreAndSkill.Core.Infrastructure.Tabular;
using CoreAndSkill.Core.Infrastructure.Tenants;
using CoreAndSkill.Core.Infrastructure.Users;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Npgsql;

namespace CoreAndSkill.Core.Infrastructure.DependencyInjection;

public static class CoreInfrastructureServiceCollectionExtensions
{
    // Kết nối chung, DbContext, UnitOfWork, interceptor — docs/quy-uoc/be-cqrs-handler.md §4,
    // docs/quy-uoc/be-entity-domain.md §5.1. Đọc CoreConnectionOptions qua IOptions<T> đã bind ở
    // Core.Web (AddCoreOptions) — Infrastructure KHÔNG inject IConfiguration trực tiếp
    // (docs/quy-uoc/be-architecture.md §4.1).
    public static IServiceCollection AddCorePersistence(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("database", tags: ["ready"]);

        services.AddSingleton<IExecutionContextScope, ExecutionContextScope>();

        // Một kết nối dùng chung cho mọi DbContext của request — docs/quy-uoc/be-cqrs-handler.md §4.
        services.AddScoped<DbConnection>(sp =>
            new NpgsqlConnection(sp.GetRequiredService<IOptions<CoreConnectionOptions>>().Value.Core));

        services.AddScoped<AuditInterceptor>();
        services.AddScoped<TenantAssignmentInterceptor>();
        services.AddScoped<AuditLogInterceptor>();
        services.AddScoped<PasswordRehashScope>(); // cùng phạm vi với AuditLogInterceptor và IdentityService
        services.AddScoped<CrossTenantActorScope>(); // luật M14 — cùng phạm vi với hai đường ghi nhật ký và TenantProvisioningService
        services.AddScoped<OutboxInterceptor>();

        services.AddDbContext<CoreDbContext>((sp, options) =>
        {
            // docs/quy-uoc/be-performance.md §7.2. Có chiến lược thử lại thì transaction chỉ được mở trong
            // IUnitOfWork.ExecuteInTransactionAsync (bọc bằng CreateExecutionStrategy) — mở ở chỗ khác là ném lúc chạy.
            // Strategy riêng của Core, KHÔNG EnableRetryOnFailure trần: hết thời hạn chờ không được thử lại (luật E11).
            options.UseNpgsql(sp.GetRequiredService<DbConnection>(), npgsql =>
            {
                npgsql.ExecutionStrategy(dependencies => new CoreExecutionStrategy(
                    dependencies, CoreExecutionStrategy.CoreMaxRetryCount, CoreExecutionStrategy.CoreMaxRetryDelay));
                npgsql.CommandTimeout(30);
                npgsql.MigrationsHistoryTable("__ef_migrations_history", CoreSchema.Name);
            });
            options.UseSnakeCaseNamingConvention();
            // THỨ TỰ LÀ LUẬT: AuditLogInterceptor PHẢI đứng SAU TenantAssignmentInterceptor — đọc TenantId đã
            // được gán đúng của entity, không đọc ambient (xem comment trong AuditLogInterceptor.cs). Cuối
            // cùng là OutboxInterceptor: TenantId của dòng outbox lấy từ entity đã được gán đơn vị đúng.
            options.AddInterceptors(
                sp.GetRequiredService<AuditInterceptor>(),
                sp.GetRequiredService<TenantAssignmentInterceptor>(),
                sp.GetRequiredService<AuditLogInterceptor>(),
                sp.GetRequiredService<OutboxInterceptor>());
        });

        services.AddScoped<DbContext>(sp => sp.GetRequiredService<CoreDbContext>());
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ISchemaVerifier, SchemaVerifier>();

        services.AddDataProtection()
            .PersistKeysToDbContext<CoreDbContext>();

        return services;
    }

    // Identity lõi — AddIdentityCore (KHÔNG AddIdentity), store, UserManager/RoleManager, chính
    // sách mật khẩu, khoá tài khoản — docs/adr/0026-ranh-gioi-identity-va-cookie.md,
    // docs/wiki-core/be/02-identity-auth.md §4.
    public static IServiceCollection AddCoreIdentity(this IServiceCollection services)
    {
        services.AddIdentityCore<AppUser>(options =>
            {
                options.User.RequireUniqueEmail = true; // docs/database/schema-core.md §4.1
            })
            .AddRoles<AppRole>()
            .AddEntityFrameworkStores<CoreDbContext>();

        // Kho người dùng của Core thay kho mặc định AddEntityFrameworkStores vừa đăng ký: dịch vi phạm unique 23505 trên hai
        // index của core.app_user thành đúng lỗi trùng của Identity —
        // docs/adr/0089-trung-unique-khi-ghi-dong-thoi-dich-o-kho-identity.md.
        // Replace, không Add: một đăng ký duy nhất, không phụ thuộc thứ tự TryAdd bên trong Identity.
        services.Replace(ServiceDescriptor.Scoped<IUserStore<AppUser>, AppUserStore>());

        // AddDefaultTokenProviders() chưa cần ở B1 — không có luồng nào dùng token của Identity
        // (đặt lại mật khẩu tự phục vụ, xác nhận email, 2FA đều ngoài v1). Thêm khi cần.

        services.AddOptions<IdentityOptions>()
            .Configure<IOptions<CoreIdentityPasswordOptions>, IOptions<CoreIdentityLockoutOptions>>(
                (identityOptions, passwordOptions, lockoutOptions) =>
                {
                    var password = passwordOptions.Value;
                    identityOptions.Password.RequiredLength = password.RequiredLength;
                    identityOptions.Password.RequireDigit = password.RequireDigit;
                    identityOptions.Password.RequireLowercase = password.RequireLowercase;
                    identityOptions.Password.RequireUppercase = password.RequireUppercase;
                    identityOptions.Password.RequireNonAlphanumeric = password.RequireNonAlphanumeric;
                    identityOptions.Password.RequiredUniqueChars = password.RequiredUniqueChars;

                    var lockout = lockoutOptions.Value;
                    identityOptions.Lockout.AllowedForNewUsers = true;
                    identityOptions.Lockout.MaxFailedAccessAttempts = lockout.MaxFailedAttempts;
                    identityOptions.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(lockout.DurationMinutes);
                });

        // ICurrentUser / ITenantContext KHÔNG đăng ký ở đây — hiện thực (HttpContextCurrentUser /
        // HttpContextTenantContext) sống ở Core.Web, nơi DUY NHẤT được đọc HttpContext
        // (docs/quy-uoc/be-architecture.md §1.1, §2.1). Infrastructure chỉ tiêu thụ interface qua DI.
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IUserLookupService, UserLookupService>();
        services.AddScoped<IUserProfileService, UserProfileService>();
        services.AddScoped<IPermissionChecker, PermissionChecker>();
        services.AddScoped<ITenantLookup, TenantLookupService>();
        services.AddScoped<ITenantProvisioningService, TenantProvisioningService>();
        services.AddScoped<ITenantAdminQueryService, TenantAdminQueryService>();
        services.AddScoped<ISessionPrincipalFactory, SessionPrincipalFactory>();
        services.AddSingleton<ITenantSeedSource, CoreTenantSeedSource>();

        // B2 — docs/wiki-core/be/trien-khai/03-b2-phan-quyen-va-bien.md.
        services.AddSingleton<IPermissionCatalogSource, CorePermissionCatalogSource>();
        services.AddHostedService<PermissionCatalogValidationHostedService>();

        // Nguồn seed đã gộp đối chiếu với danh mục đã gộp — docs/quy-uoc/be-architecture.md §1.1.
        services.AddHostedService<TenantSeedValidationHostedService>();

        // Bộ đếm trong bộ nhớ tiến trình — Singleton, docs/quy-uoc/be-architecture.md §5.2.
        services.AddSingleton<ILoginAttemptLimiter, LoginAttemptLimiter>();

        services.AddScoped<IUserAdminService, UserAdminService>();
        services.AddScoped<IUserQueryService, UserQueryService>();
        services.AddScoped<IRoleQueryService, RoleQueryService>();
        services.AddScoped<IRoleAdminService, RoleAdminService>();
        services.AddScoped<IPermissionMatrixService, PermissionMatrixService>();
        services.AddScoped<IMenuQueryService, MenuQueryService>();

        return services;
    }

    // Job nền — docs/wiki-core/be/trien-khai/04-b3-van-hanh.md §1, §3; docs/quy-uoc/be-architecture.md
    // §1.1. BackgroundService của .NET, KHÔNG thư viện. Hàng đợi trong bộ nhớ tiến trình — v1 một
    // instance (ADR-0014) nên chấp nhận được; không bền qua khởi động lại.
    //
    // KHÔNG đăng ký IStaleDataCleaner nào ở đây — Core chưa có cleaner nào (chính sách lưu giữ chưa
    // chốt, docs/wiki-core/be/10-data-retention.md §8); module đăng ký cleaner của mình khi cần,
    // cùng khuôn IPermissionCatalogSource/ITenantSeedSource (mục A3).
    public static IServiceCollection AddCoreBackgroundJobs(this IServiceCollection services)
    {
        services.AddSingleton(System.Threading.Channels.Channel.CreateUnbounded<QueuedJob>());
        services.AddTransient<IBackgroundJobScheduler, BackgroundJobScheduler>(); // be-architecture.md §5.2
        services.AddHostedService<BackgroundJobQueueHostedService>();
        services.AddHostedService<StaleDataCleanupHostedService>();

        return services;
    }

    // Pha B4 — tệp, việc chạy nền theo yêu cầu, outbox, thông báo, nhập/xuất. Mỗi nhóm nêu tài liệu chủ.
    public static IServiceCollection AddCoreFilesAndMessaging(this IServiceCollection services)
    {
        // Lưu trữ tệp — docs/wiki-core/be/14-file-storage.md. Singleton: không giữ state theo request
        // (đường dẫn gốc đọc từ options, đồng hồ từ TimeProvider).
        services.AddSingleton<IValidateOptions<CoreFileOptions>, CoreFileOptionsValidator>();
        services.AddSingleton<IFileStorage, LocalFileStorage>();
        services.AddScoped<IFileRepository, EfFileRepository>();

        // Việc chạy nền theo jobId — docs/quy-uoc/be-cqrs-handler.md §10.3.
        services.AddScoped<IJobRepository, EfJobRepository>();

        // Outbox — docs/wiki-core/be/12-notifications.md §2. Bộ phát là BackgroundService ĐỊNH KỲ đứng
        // riêng (ADR-0047), không qua IBackgroundJobScheduler.
        services.AddScoped<IOutboxReplayService, OutboxReplayService>();
        services.AddSingleton<OutboxDispatcher>();
        services.AddHealthChecks().AddCheck<OutboxHealthCheck>("outbox", tags: ["ready"]);

        // Thông báo trong ứng dụng — §5.
        services.AddScoped<INotificationRepository, EfNotificationRepository>();

        // Nhập/xuất — docs/wiki-core/be/15-import-export.md. Bộ đọc/ghi không giữ state.
        services.AddSingleton<ITabularReader, TabularReader>();
        services.AddSingleton<ITabularWriterFactory, TabularWriterFactory>();
        services.AddScoped<IImportRowWriter, EfImportRowWriter>();

        // Nhật ký kiểm toán ghi TƯỜNG MINH (xuất, phát lại outbox) — bổ sung cho AuditLogInterceptor.
        services.AddScoped<IAuditTrail, EfAuditTrail>();

        // THỨ TỰ ĐĂNG KÝ LÀ THỨ TỰ KHỞI ĐỘNG (host gọi StartAsync tuần tự): JobRecovery PHẢI chạy xong
        // TRƯỚC bộ phát outbox. Đảo lại thì bộ phát vừa đẩy một việc `queued` vào hàng đợi (dòng outbox
        // chuyển `done`), JobRecovery thấy việc đó không còn dòng `pending` và đánh dấu nó gián đoạn oan.
        services.AddHostedService<JobRecoveryHostedService>();
        services.AddHostedService<FilePurposeCatalogValidationHostedService>();
        services.AddHostedService<OutboxDispatcherHostedService>();
        services.AddHostedService<FileMaintenanceHostedService>();

        return services;
    }
}
