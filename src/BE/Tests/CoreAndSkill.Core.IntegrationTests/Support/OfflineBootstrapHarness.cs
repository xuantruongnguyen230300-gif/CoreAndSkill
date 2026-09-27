using System.Data;
using System.Data.Common;
using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Application.Configuration;
using CoreAndSkill.Core.Application.Tenants;
using CoreAndSkill.Core.Infrastructure.Audit;
using CoreAndSkill.Core.Infrastructure.DependencyInjection;
using CoreAndSkill.Core.Infrastructure.Execution;
using CoreAndSkill.Core.Infrastructure.Identity;
using CoreAndSkill.Core.Infrastructure.Permissions;
using CoreAndSkill.Core.Infrastructure.Persistence;
using CoreAndSkill.Core.Infrastructure.Tenants;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;

namespace CoreAndSkill.Core.IntegrationTests.Support;

// Lệnh `core bootstrap` (CoreCommandRunner.RunBootstrapAsync) chạy trọn đường — runner THẬT, TenantProvisioningService THẬT,
// Identity THẬT — mà không chạm database. CoreDbContext thật trên OfflineCoreDbContext; mọi câu đọc trả tập rỗng (chưa có đơn
// vị, chưa có tài khoản, chưa ai dùng email). IUnitOfWork là bản thu âm: cùng điều kiện commit với UnitOfWork thật, đếm commit;
// mọi lượt SaveChanges bị WriteRecorder chặn và đếm.
//
// Identity lấy từ ĐĂNG KÝ THẬT (AddCoreIdentity): bộ kiểm người dùng của Identity, RequireUniqueEmail = true, AppUserStore,
// chính sách mật khẩu theo giá trị mặc định của CoreIdentityPasswordOptions. UserManager dựng tay với danh sách bộ kiểm rỗng và
// IdentityOptions mặc định (RequireUniqueEmail = false) từng cho ca thành công xanh trong khi lệnh thật thất bại trên mọi host:
// email null bị bộ kiểm thật trả InvalidEmail.
internal sealed class OfflineBootstrapHarness
{
    public const string Password = "Passw0rd-Tam1";

    public required IServiceProvider Services { get; init; }
    public required RecordingUnitOfWork UnitOfWork { get; init; }
    public required WriteRecorder Writes { get; init; }
    public required CoreDbContext Db { get; init; }

    // Bộ cấu hình hợp lệ; tham số nào truyền vào thay đúng khoá đó. Truyền null cho một email là "khoá vắng mặt" — bộ gắn cấu
    // hình để nguyên null khi không có khoá.
    public static CoreBootstrapOptions ValidOptions(
        string? operatorEmail = "vanhanh@he-thong.example.com",
        string? adminEmail = "quantri@dv-dau.example.com",
        string operatorPassword = Password,
        string adminPassword = Password) => new()
        {
            SystemTenantCode = "HE-THONG",
            SystemTenantName = "Đơn vị hệ thống",
            FirstTenantCode = "DV-DAU",
            FirstTenantName = "Đơn vị đầu tiên",
            OperatorUserName = "vanhanh",
            OperatorPassword = operatorPassword,
            OperatorEmail = operatorEmail!,
            AdminUserName = "quantri",
            AdminPassword = adminPassword,
            AdminEmail = adminEmail!,
        };

    public static OfflineBootstrapHarness Build(ITenantSeedSource seedSource, CoreBootstrapOptions? options = null)
    {
        var writes = new WriteRecorder();
        var recorder = new SqlRecorder();
        var db = OfflineCoreDbContext.Create(null, [.. recorder.Interceptors, new EmptyReader(), writes]);

        var identity = CoreIdentityOver(db);

        var provisioning = new TenantProvisioningService(
            new ExecutionContextScope(), db,
            identity.GetRequiredService<UserManager<AppUser>>(), identity.GetRequiredService<RoleManager<AppRole>>(),
            [seedSource], [new CorePermissionCatalogSource()],
            Substitute.For<ICurrentUser>(), Substitute.For<ITenantContext>(), Substitute.For<IClientAddressAccessor>(),
            TimeProvider.System, new CrossTenantActorScope(), NullLogger<TenantProvisioningService>.Instance);

        var unitOfWork = new RecordingUnitOfWork(db);

        var services = new ServiceCollection()
            .AddSingleton(Microsoft.Extensions.Options.Options.Create(options ?? ValidOptions()))
            .AddSingleton<ITenantProvisioningService>(provisioning)
            .AddSingleton<IUnitOfWork>(unitOfWork)
            .BuildServiceProvider();

        return new OfflineBootstrapHarness { Services = services, UnitOfWork = unitOfWork, Writes = writes, Db = db };
    }

    // UserManager / RoleManager từ ĐĂNG KÝ THẬT của Core (AddCoreIdentity) trên một CoreDbContext cho sẵn — một scope, sống
    // bằng test. Dùng thay cho UserManager dựng tay ở mọi test khẳng định một tài khoản được TẠO thành công.
    public static IServiceProvider CoreIdentityOver(CoreDbContext db)
        => new ServiceCollection()
            .AddLogging()
            .AddSingleton(db)
            .AddCoreIdentity()
            .BuildServiceProvider()
            .CreateScope()
            .ServiceProvider;

    // "Không dòng nào được ghi" theo ba nghĩa: không lượt SaveChanges nào tới database, không transaction nào commit, và
    // không dòng nào còn đang được dàn dựng trong ChangeTracker (tức phép kiểm chạy TRƯỚC cả lệnh Add đầu tiên).
    public void ShouldHaveWrittenNothing()
    {
        Writes.Saves.ShouldBe(0, $"Đã có lượt lưu: {string.Join(", ", Writes.StagedRows)}");
        UnitOfWork.Commits.ShouldBe(0);
        Db.ChangeTracker.Entries().ShouldBeEmpty();
    }

    // Nguồn seed không khai gì — lệnh chỉ còn tạo đơn vị và tài khoản. Không vai trò, không menu: không câu đọc danh mục quyền
    // nào mà EmptyReader sẽ trả rỗng thành SEED_FAILED.
    public sealed class EmptySeed : ITenantSeedSource
    {
        public IReadOnlyCollection<SeedRole> GetRoles() => [];
        public IReadOnlyCollection<SeedRolePermission> GetRolePermissions() => [];
        public IReadOnlyCollection<SeedMenuItem> GetMenuItems() => [];
    }

    // Cùng điều kiện commit với UnitOfWork thật (docs/quy-uoc/be-cqrs-handler.md §4): ChangeTracker.Clear() đầu lượt, lưu +
    // commit chỉ khi outcome cho phép, còn lại rollback. Không dùng UnitOfWork thật vì nó tự mở DbConnection — không có
    // database để mở.
    public sealed class RecordingUnitOfWork(CoreDbContext db) : IUnitOfWork
    {
        public int Commits { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);

        public async Task<T> ExecuteInTransactionAsync<T>(
            Func<CancellationToken, Task<TransactionOutcome<T>>> operation, CancellationToken ct = default)
        {
            db.ChangeTracker.Clear();
            await using var transaction = await db.Database.BeginTransactionAsync(ct);

            var outcome = await operation(ct);
            if (outcome.ShouldCommit)
            {
                await db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
                Commits++;
            }
            else
            {
                await transaction.RollbackAsync(ct);
            }

            return outcome.Value;
        }
    }

    // Chặn MỌI lượt SaveChanges trước database, ghi lại từng dòng đang dàn dựng (loại và chính thực thể), rồi chấp nhận thay đổi
    // như một lượt lưu thành công.
    public sealed class WriteRecorder : SaveChangesInterceptor
    {
        public int Saves { get; private set; }

        public List<string> StagedRows { get; } = [];

        // Chính các thực thể đã lưu — test đọc lại giá trị đã ghi (email của tài khoản nào).
        public List<object> StagedEntities { get; } = [];

        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
            => Record(eventData.Context!);

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(Record(eventData.Context!));

        private InterceptionResult<int> Record(DbContext context)
        {
            var staged = context.ChangeTracker.Entries()
                .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
                .Select(e => e.Entity)
                .ToList();

            if (staged.Count > 0)
                Saves++;

            StagedEntities.AddRange(staged);
            StagedRows.AddRange(staged.Select(e => e.GetType().Name));
            context.ChangeTracker.AcceptAllChanges();
            return InterceptionResult<int>.SuppressWithResult(staged.Count);
        }

        // Mỗi tài khoản một lần — Identity lưu một tài khoản qua nhiều lượt (tạo, rồi cập nhật).
        public IReadOnlyList<AppUser> Accounts => [.. StagedEntities.OfType<AppUser>().Distinct()];
    }

    // Mọi câu đọc trả tập RỖNG: đơn vị chưa có, tên đăng nhập và email chưa dùng.
    private sealed class EmptyReader : DbCommandInterceptor
    {
        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
            => InterceptionResult<DbDataReader>.SuppressWithResult(Empty());

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult(InterceptionResult<DbDataReader>.SuppressWithResult(Empty()));

        private static DbDataReader Empty()
        {
            var table = new DataTable();
            table.Columns.Add("x", typeof(object));
            return table.CreateDataReader();
        }
    }
}
