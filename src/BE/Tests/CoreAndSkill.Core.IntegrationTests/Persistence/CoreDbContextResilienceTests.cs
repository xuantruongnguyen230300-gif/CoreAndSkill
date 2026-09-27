using System.Data;
using System.Data.Common;
using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Infrastructure.Persistence;
using CoreAndSkill.Core.IntegrationTests.Support;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Persistence;

// docs/quy-uoc/be-performance.md §7.2: CoreDbContext dùng strategy thử lại của Core và khai CommandTimeout tường minh.
// docs/quy-uoc/be-cqrs-handler.md §4 luật 2 (luật E12): lỗi đường truyền lúc commit không làm operation chạy lại.
// Mọi test ở đây chạy KHÔNG cần Docker — DbContext dựng từ đúng DI của ứng dụng (CoreWebApplicationFactory, chuỗi kết nối
// không tới được hoặc kết nối giả), không truy vấn nào tới database.
//
// ĐIỂM MÙ: lỗi tạm thời ở đây là ngoại lệ Npgsql do test ném, không phải một kết nối đứt thật. Kết nối đứt giữa
// transaction trên PostgreSQL thật (kết nối Scoped dùng chung phải mở lại được) là việc của
// PersistenceResilienceDatabaseTests (RequiresDocker, chưa chạy).
public sealed class CoreDbContextResilienceTests
{
    [Fact]
    public void CoreDbContext_FromApplicationDi_RetriesOnFailure_AndDeclaresCommandTimeout()
    {
        using var factory = new CoreWebApplicationFactory(keepProductionRetryStrategy: true);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CoreDbContext>();

        var strategy = db.Database.CreateExecutionStrategy();
        strategy.ShouldBeOfType<CoreExecutionStrategy>();
        strategy.RetriesOnFailure.ShouldBeTrue();
        db.Database.GetCommandTimeout().ShouldBe(30);
    }

    // ADR-0053 quyết định 4: chế độ PostgreSQL thật giữ ĐÚNG strategy của production. "Chế độ PostgreSQL thật" ở factory
    // là: chuỗi kết nối khác UnreachableConnectionString — đúng thứ B4DockerHost/PostgresFixture truyền vào. Chuỗi ở đây
    // cũng không tới được, nhưng factory không được phép biết điều đó: nó chỉ nhìn cấu hình. Vì chuỗi không tới được, test tự
    // thay bộ kiểm migration lúc khởi động (luật E8 — không kết nối được thì từ chối khởi động): nó chỉ cần DI của host.
    [Fact]
    public void DockerModeFactory_KeepsProductionRetryStrategy()
    {
        using var factory = new CoreWebApplicationFactory(
            new Dictionary<string, string?>
            {
                ["ConnectionStrings:Core"] = "Host=127.0.0.1;Port=1;Database=docker_mode_probe;Username=test;Password=test;Timeout=2",
            },
            services => services.AddScoped<ISchemaVerifier, NoDriftSchemaVerifier>());
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CoreDbContext>();

        var strategy = db.Database.CreateExecutionStrategy();
        strategy.ShouldBeOfType<CoreExecutionStrategy>();
        strategy.RetriesOnFailure.ShouldBeTrue();
    }

    // Chiều ngược lại — thiếu nó thì test trên xanh cả khi factory không bao giờ thay strategy.
    [Fact]
    public void UnreachableModeFactory_UsesNonRetryingStrategy_AndKeepsCommandTimeout()
    {
        using var factory = new CoreWebApplicationFactory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CoreDbContext>();

        var strategy = db.Database.CreateExecutionStrategy();
        strategy.ShouldBeOfType<NonRetryingExecutionStrategy>();
        strategy.RetriesOnFailure.ShouldBeFalse();
        db.Database.GetCommandTimeout().ShouldBe(30, "đổi strategy không được làm mất cấu hình Npgsql còn lại");
    }

    // Nhánh ChangeTracker.Clear() đầu mỗi lượt của UnitOfWork chỉ sống khi strategy thật sự thử lại. Lượt đầu để lại một
    // entity Added rồi ném lỗi tạm thời; lượt hai phải chạy, và phải thấy bộ theo dõi trống.
    [Fact]
    public async Task UnitOfWork_TransientFailureInsideOperation_RetriesWithClearedChangeTracker()
    {
        using var host = new FakeConnectionHost();
        var (unitOfWork, db) = host.Resolve();

        var attempts = 0;
        var trackedAtRetry = -1;

        var result = await unitOfWork.ExecuteInTransactionAsync(ct =>
        {
            attempts++;
            if (attempts == 1)
            {
                db.DataProtectionKeys.Add(new DataProtectionKey { FriendlyName = "lượt-đầu", Xml = "<key/>" });
                throw new NpgsqlException("Mô phỏng chớp mạng.", new IOException("mất gói"));
            }

            trackedAtRetry = db.ChangeTracker.Entries().Count();
            return Task.FromResult(new TransactionOutcome<string>("xong", ShouldCommit: false));
        });

        result.ShouldBe("xong");
        attempts.ShouldBe(2);
        trackedAtRetry.ShouldBe(0, "entity của lượt hỏng còn trong bộ theo dõi ở lượt thử lại");
    }

    // Luật E12. Lỗi đường truyền đúng lúc commit: không biết máy chủ đã commit chưa — chạy lại operation có thể ghi hai
    // lần. Strategy ở đây là strategy THẬT của production (lỗi IOException vẫn tạm thời với nó), nên nếu UnitOfWork không
    // bọc lỗi commit thì operation chạy bốn lần.
    [Fact]
    public async Task ExecuteInTransaction_CommitTransportFailure_RunsOperationOnce()
    {
        var transportFailure = new NpgsqlException("Đứt kết nối lúc commit.", new IOException("mất gói"));
        using var host = new FakeConnectionHost(commitFailures: [transportFailure]);
        var (unitOfWork, _) = host.Resolve();

        var attempts = 0;
        var thrown = await Should.ThrowAsync<Exception>(() => unitOfWork.ExecuteInTransactionAsync(_ =>
        {
            attempts++;
            return Task.FromResult(new TransactionOutcome<int>(1, ShouldCommit: true));
        }));

        attempts.ShouldBe(1, "operation đã chạy lại sau một lần commit không rõ kết quả");
        thrown.ShouldBeOfType<CommitOutcomeUnknownException>();
        thrown.InnerException.ShouldBeSameAs(transportFailure);
        // Loại KHÔNG tạm thời: phân loại của Npgsql chỉ nhận NpgsqlException và TimeoutException.
        thrown.ShouldNotBeAssignableTo<NpgsqlException>();
        thrown.ShouldNotBeAssignableTo<TimeoutException>();
    }

    // PostgresException lúc commit là máy chủ ĐÃ trả lời rằng nó không commit — vẫn theo phân loại thường. Thiếu test này
    // thì bọc MỌI lỗi commit (kể cả 40001) cũng làm test trên xanh.
    [Fact]
    public async Task ExecuteInTransaction_CommitServerRejection_IsRetriedAsUsual()
    {
        using var host = new FakeConnectionHost(commitFailures: [new PostgresException("mô phỏng", "ERROR", "ERROR", "40001")]);
        var (unitOfWork, _) = host.Resolve();

        var attempts = 0;
        var result = await unitOfWork.ExecuteInTransactionAsync(_ =>
        {
            attempts++;
            return Task.FromResult(new TransactionOutcome<string>("xong", ShouldCommit: true));
        });

        result.ShouldBe("xong");
        attempts.ShouldBe(2);
    }

    // Luật E14 — docs/adr/0055-commit-khong-nhan-token-huy-va-het-han-mo-ket-noi-khong-thu-lai.md quyết định 1. Đơn vị công
    // việc đã tới bước commit thì client rời đi không đổi câu trả lời cho câu hỏi "dữ liệu có nên được ghi không". Commit
    // nhận token của request thì lần huỷ đúng lúc đó thành CommitOutcomeUnknownException — 500 kèm một dòng log Error đòi
    // người đối soát cho một việc không có gì để đối soát.
    [Fact]
    public async Task ExecuteInTransaction_CommitIgnoresRequestCancellation()
    {
        using var host = new FakeConnectionHost();
        var (unitOfWork, _) = host.Resolve();
        using var request = new CancellationTokenSource();

        var result = await unitOfWork.ExecuteInTransactionAsync(_ =>
        {
            request.Cancel(); // client rời đi SAU khi operation xong, TRƯỚC commit
            return Task.FromResult(new TransactionOutcome<string>("xong", ShouldCommit: true));
        }, request.Token);

        result.ShouldBe("xong");
        request.IsCancellationRequested.ShouldBeTrue("chống test rỗng: token của request phải thật sự đã huỷ lúc commit");
        host.CommitTokens.ShouldHaveSingleItem().ShouldBe(CancellationToken.None);
    }

    // Chiều còn lại của E14: huỷ TRƯỚC commit (trong operation) vẫn đi ra như một lần huỷ thường — không commit, không bị
    // bọc thành commit không rõ kết quả.
    [Fact]
    public async Task ExecuteInTransaction_CancellationBeforeCommit_SurfacesAsCancellation()
    {
        using var host = new FakeConnectionHost();
        var (unitOfWork, _) = host.Resolve();
        using var request = new CancellationTokenSource();

        var thrown = await Should.ThrowAsync<Exception>(() => unitOfWork.ExecuteInTransactionAsync(ct =>
        {
            request.Cancel();
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(new TransactionOutcome<int>(1, ShouldCommit: true));
        }, request.Token));

        thrown.ShouldBeAssignableTo<OperationCanceledException>();
        host.CommitTokens.ShouldBeEmpty();
    }

    // Factory với strategy của production và kết nối giả. Kết nối giả chỉ bật SAU khi host đã khởi động — lượt kiểm lược
    // đồ lúc khởi động đi đường kết nối thật (không tới được) như mọi test dùng factory mặc định.
    private sealed class FakeConnectionHost : IDisposable
    {
        private readonly CoreWebApplicationFactory _factory;
        private readonly Queue<Exception> _commitFailures;
        private readonly List<IServiceScope> _scopes = [];
        private bool _useFake;

        public FakeConnectionHost(IEnumerable<Exception>? commitFailures = null)
        {
            _commitFailures = new Queue<Exception>(commitFailures ?? []);
            _factory = new CoreWebApplicationFactory(
                configureServices: services => services.AddScoped<DbConnection>(_ => _useFake
                    ? new FakeDbConnection(_commitFailures, CommitTokens)
                    : new NpgsqlConnection(CoreWebApplicationFactory.UnreachableConnectionString)),
                keepProductionRetryStrategy: true);
            _ = _factory.Services;
            _useFake = true;
        }

        // Token mà mỗi lần commit nhận được, theo thứ tự.
        public List<CancellationToken> CommitTokens { get; } = [];

        public (IUnitOfWork UnitOfWork, CoreDbContext Db) Resolve()
        {
            var scope = _factory.Services.CreateScope();
            _scopes.Add(scope);
            return (scope.ServiceProvider.GetRequiredService<IUnitOfWork>(), scope.ServiceProvider.GetRequiredService<CoreDbContext>());
        }

        public void Dispose()
        {
            foreach (var scope in _scopes)
                scope.Dispose();
            _factory.Dispose();
        }
    }

    // Kết nối giả: mở/đóng/transaction chỉ đổi trạng thái trong bộ nhớ. Lệnh SQL nào chạy tới đây là test sai. Mỗi lần
    // commit lấy một lỗi khỏi hàng đợi (nếu còn) và ném nó.
    private sealed class FakeDbConnection(Queue<Exception> commitFailures, List<CancellationToken> commitTokens) : DbConnection
    {
        private ConnectionState _state = ConnectionState.Closed;

        [System.Diagnostics.CodeAnalysis.AllowNull]
        public override string ConnectionString { get; set; } = string.Empty;

        public override string Database => "fake";

        public override string DataSource => "fake";

        public override string ServerVersion => "16.0";

        public override ConnectionState State => _state;

        public override void ChangeDatabase(string databaseName) => throw new NotSupportedException();

        public override void Close() => _state = ConnectionState.Closed;

        public override void Open() => _state = ConnectionState.Open;

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
            => new FakeDbTransaction(this, isolationLevel, commitFailures, commitTokens);

        protected override DbCommand CreateDbCommand()
            => throw new NotSupportedException("Test này không được chạy lệnh SQL nào.");
    }

    private sealed class FakeDbTransaction(
        DbConnection connection, IsolationLevel isolationLevel, Queue<Exception> commitFailures, List<CancellationToken> commitTokens)
        : DbTransaction
    {
        public override IsolationLevel IsolationLevel => isolationLevel;

        protected override DbConnection DbConnection => connection;

        // Ghi lại token rồi đi đúng đường của lớp cơ sở: token đã huỷ ⇒ Task huỷ, như một provider thật.
        public override Task CommitAsync(CancellationToken cancellationToken = default)
        {
            commitTokens.Add(cancellationToken);
            return base.CommitAsync(cancellationToken);
        }

        public override void Commit()
        {
            if (commitFailures.TryDequeue(out var failure))
                throw failure;
        }

        public override void Rollback()
        {
        }
    }
}
