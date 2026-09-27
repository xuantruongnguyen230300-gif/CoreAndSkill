using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Domain.Audit;
using CoreAndSkill.Core.Domain.Files;
using CoreAndSkill.Core.Domain.Jobs;
using CoreAndSkill.Core.Domain.Menu;
using CoreAndSkill.Core.Domain.Notifications;
using CoreAndSkill.Core.Domain.Outbox;
using CoreAndSkill.Core.Domain.Permissions;
using CoreAndSkill.Core.Domain.Tenants;
using CoreAndSkill.Core.Infrastructure.Identity;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CoreAndSkill.Core.Infrastructure.Persistence;

// Nguồn duy nhất của chữ ký CoreDbContext — docs/quy-uoc/be-entity-domain.md §5.1. Dạng khai ĐỦ
// KIỂU để năm kiểu join tenant-aware thay kiểu mặc định của Identity.
public sealed class CoreDbContext(
    DbContextOptions<CoreDbContext> options,
    ITenantContext tenantContext)
    : IdentityDbContext<AppUser, AppRole, Guid, AppUserClaim, AppUserRole, AppUserLogin, AppRoleClaim, AppUserToken>(options),
      ITenantFilteredContext,
      IDataProtectionKeyContext
{
    public Guid? CurrentTenantId => tenantContext.TenantId;

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<PermissionResource> PermissionResources => Set<PermissionResource>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<MenuItemRole> MenuItemRoles => Set<MenuItemRole>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<StoredFile> Files => Set<StoredFile>();
    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NotificationRecipient> NotificationRecipients => Set<NotificationRecipient>();

    // IDataProtectionKeyContext — docs/adr/0014-mot-instance-key-ring-postgres.md §2.
    public DbSet<Microsoft.AspNetCore.DataProtection.EntityFrameworkCore.DataProtectionKey> DataProtectionKeys
        => Set<Microsoft.AspNetCore.DataProtection.EntityFrameworkCore.DataProtectionKey>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema(CoreSchema.Name);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CoreDbContext).Assembly);

        modelBuilder.ApplyCoreQueryFilters(this); // PHẢI chạy SAU dòng trên
    }
}
