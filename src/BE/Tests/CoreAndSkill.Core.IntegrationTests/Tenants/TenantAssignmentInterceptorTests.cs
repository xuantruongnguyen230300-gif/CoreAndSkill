using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Infrastructure.Identity;
using CoreAndSkill.Core.Infrastructure.Persistence;
using CoreAndSkill.Core.Infrastructure.Persistence.Interceptors;
using CoreAndSkill.Core.IntegrationTests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NSubstitute;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Tenants;

// Luật M8 (docs/wiki-core/be/17-multi-tenant.md) — TenantId bất biến theo GIÁ TRỊ. UserStore.UpdateAsync của Identity gọi
// DbContext.Update(entity), đánh dấu MỌI cột là Modified kể cả TenantId dù giá trị giữ nguyên; interceptor không được
// ném ở ca đó (lỗi 500 ở đổi mật khẩu / đăng nhập sai). CoreDbContext THẬT + interceptor THẬT, không database
// (OfflineCoreDbContext): lượt lưu bị chặn sau khi interceptor chạy, ModifiedProbe chụp tên cột còn bị coi là sửa.
public sealed class TenantAssignmentInterceptorTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly ModifiedProbe _probe = new();

    private CoreDbContext Build()
    {
        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.TenantId.Returns(_tenantId);
        return OfflineCoreDbContext.Create(_tenantId, new TenantAssignmentInterceptor(tenantContext), _probe);
    }

    private AppUser Tracked(CoreDbContext db)
    {
        var user = AppUser.NewAccount("nguyenvana", "a@example.com", "Nguyễn Văn A");
        var entry = db.Entry(user);
        entry.Property(x => x.TenantId).CurrentValue = _tenantId;
        entry.State = EntityState.Unchanged;
        return user;
    }

    [Fact]
    public async Task UpdateMarksEveryColumnModified_ButTenantIdValueUnchanged_DoesNotThrow_AndTenantIdIsNotWritten()
    {
        await using var db = Build();
        var user = Tracked(db);

        db.Update(user);
        db.Entry(user).Property(x => x.TenantId).IsModified.ShouldBeTrue("chống test rỗng: cờ phải thật sự bật");

        await db.SaveChangesAsync();

        _probe.Modified.ShouldNotBeEmpty("chống test rỗng: lượt lưu phải tới được probe");
        _probe.Modified.ShouldNotContain(nameof(AppUser.TenantId));
        _probe.Modified.ShouldContain(nameof(AppUser.UserName), "các cột khác vẫn được ghi");
    }

    [Fact]
    public async Task TenantIdValueReallyChanged_StillThrows()
    {
        await using var db = Build();
        var user = Tracked(db);

        db.Entry(user).Property(x => x.TenantId).CurrentValue = Guid.NewGuid();
        db.Entry(user).Property(x => x.TenantId).IsModified.ShouldBeTrue();

        var ex = await Should.ThrowAsync<InvalidOperationException>(() => db.SaveChangesAsync());
        ex.Message.ShouldContain("TenantId");
    }

    // Chụp tên cột còn mang cờ Modified tại thời điểm lưu (sau interceptor đứng trước), rồi chặn lượt lưu.
    private sealed class ModifiedProbe : SaveChangesInterceptor
    {
        public List<string> Modified { get; } = [];

        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
            => Capture(eventData.Context!);

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(Capture(eventData.Context!));

        private InterceptionResult<int> Capture(DbContext context)
        {
            Modified.AddRange(context.ChangeTracker.Entries<AppUser>()
                .Where(e => e.State == EntityState.Modified)
                .SelectMany(e => e.Properties.Where(p => p.IsModified).Select(p => p.Metadata.Name)));
            context.ChangeTracker.AcceptAllChanges();
            return InterceptionResult<int>.SuppressWithResult(1);
        }
    }
}
