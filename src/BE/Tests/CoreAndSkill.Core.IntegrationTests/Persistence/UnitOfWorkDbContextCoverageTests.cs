using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.IntegrationTests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Persistence;

// Luật B8 — docs/RULES.md §10, docs/quy-uoc/be-cqrs-handler.md §4, docs/adr/0025-luu-du-lieu-module-mot-transaction.md:
// mọi `DbContext` đăng ký trong container phải có mặt trong tập mà `IUnitOfWork` điều phối. Khuôn đăng ký là HAI dòng —
// `AddDbContext<TContext>()` rồi `AddScoped<DbContext>(sp => sp.GetRequiredService<TContext>())`; quên dòng thứ hai thì
// dữ liệu của module ghi NGOÀI transaction chung, hoặc không được lưu, và nhánh thành công vẫn xanh. Hỏng im lặng.
//
// Đo trên DI THẬT của host: một bên là mọi kiểu DbContext có trong bản kê đăng ký, một bên là thứ `IEnumerable<DbContext>`
// (đúng tham số mà UnitOfWork nhận) thật sự trả về khi resolve.
public sealed class UnitOfWorkDbContextCoverageTests
{
    [Fact]
    public void EveryRegisteredDbContext_IsSeenByTheUnitOfWork()
    {
        using var probe = new DbContextRegistrationProbe();

        probe.RegisteredContextTypes.ShouldBeSubsetOf(
            probe.ContextTypesSeenByUnitOfWork,
            "DbContext đăng ký mà IUnitOfWork không thấy: dữ liệu của nó ghi ngoài transaction chung — thiếu dòng "
            + "AddScoped<DbContext>(sp => sp.GetRequiredService<TContext>()) (be-cqrs-handler.md §4)");
    }

    // T6 — hai tập đầu vào khác rỗng và chạm DbContext THẬT của Core. Một phép đo trả rỗng ở cả hai vế luôn là tập con của
    // chính nó: cổng xanh vĩnh viễn mà không kiểm gì.
    [Fact]
    public void EveryRegisteredDbContext_IsSeenByTheUnitOfWork_MeasuresTheRealCoreDbContext()
    {
        using var probe = new DbContextRegistrationProbe();

        probe.RegisteredContextTypes.ShouldContain(typeof(CoreAndSkill.Core.Infrastructure.Persistence.CoreDbContext));
        probe.ContextTypesSeenByUnitOfWork.ShouldContain(typeof(CoreAndSkill.Core.Infrastructure.Persistence.CoreDbContext));
    }

    // Detector: một module đăng ký DbContext của mình mà QUÊN dòng thứ hai — đúng hình dạng lỗi mà luật tồn tại để bắt.
    [Fact]
    public void Detector_B8_Catches_AModuleDbContextThatIsNotWiredIntoTheUnitOfWork()
    {
        using var probe = new DbContextRegistrationProbe(services =>
            services.AddDbContext<FakeModuleDbContext>(options => options.UseNpgsql(CoreWebApplicationFactory.UnreachableConnectionString)));

        probe.RegisteredContextTypes.ShouldContain(typeof(FakeModuleDbContext));
        probe.ContextTypesSeenByUnitOfWork.ShouldNotContain(typeof(FakeModuleDbContext));

        // Và chính KHẲNG ĐỊNH của cổng phải đỏ — không chỉ hai tập lệch nhau.
        Should.Throw<ShouldAssertException>(
            () => probe.RegisteredContextTypes.ShouldBeSubsetOf(probe.ContextTypesSeenByUnitOfWork));
    }

    // Đối chứng: cùng module đó đăng ký ĐỦ hai dòng thì tập con thoả — cổng không báo sai cho cách làm đúng.
    [Fact]
    public void Detector_B8_Ignores_AModuleDbContextRegisteredWithBothLines()
    {
        using var probe = new DbContextRegistrationProbe(services =>
        {
            services.AddDbContext<FakeModuleDbContext>(options => options.UseNpgsql(CoreWebApplicationFactory.UnreachableConnectionString));
            services.AddScoped<DbContext>(sp => sp.GetRequiredService<FakeModuleDbContext>());
        });

        probe.RegisteredContextTypes.ShouldBeSubsetOf(probe.ContextTypesSeenByUnitOfWork);
    }

    // Host thật + bản kê đăng ký của chính nó. ConfigureTestServices chạy SAU đăng ký của ứng dụng nên bản kê chụp ở đó là
    // bản kê đầy đủ — gồm cả phần một "module" thêm vào.
    private sealed class DbContextRegistrationProbe : IDisposable
    {
        private readonly CoreWebApplicationFactory _factory;
        private readonly IServiceScope _scope;

        public DbContextRegistrationProbe(Action<IServiceCollection>? registerModule = null)
        {
            IReadOnlyList<Type> registered = [];

            _factory = new CoreWebApplicationFactory(configureServices: services =>
            {
                registerModule?.Invoke(services);

                registered = [.. services
                    .Select(d => d.ServiceType)
                    .Where(t => t != typeof(DbContext) && typeof(DbContext).IsAssignableFrom(t))
                    .Distinct()];
            });

            _scope = _factory.Services.CreateScope();
            RegisteredContextTypes = registered;
            ContextTypesSeenByUnitOfWork = [.. _scope.ServiceProvider.GetServices<DbContext>().Select(c => c.GetType()).Distinct()];

            // Chống đọc nhầm: tham số của UnitOfWork đúng là IEnumerable<DbContext> — nếu chữ ký đổi, phép đo ở trên đo
            // nhầm chỗ và phải đỏ ngay tại đây.
            typeof(CoreAndSkill.Core.Infrastructure.Persistence.UnitOfWork)
                .GetConstructors().Single()
                .GetParameters().Select(p => p.ParameterType)
                .ShouldContain(typeof(IEnumerable<DbContext>));

            _scope.ServiceProvider.GetRequiredService<IUnitOfWork>().ShouldNotBeNull();
        }

        public IReadOnlyList<Type> RegisteredContextTypes { get; }

        public IReadOnlyList<Type> ContextTypesSeenByUnitOfWork { get; }

        public void Dispose()
        {
            _scope.Dispose();
            _factory.Dispose();
        }
    }

    // Đóng vai DbContext của một module hạ nguồn — không entity nào, chỉ cần là một DbContext thật để DI dựng được.
    public sealed class FakeModuleDbContext(DbContextOptions<FakeModuleDbContext> options) : DbContext(options);
}
