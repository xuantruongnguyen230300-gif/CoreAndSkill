using CoreAndSkill.Core.Application.Maintenance;
using CoreAndSkill.Core.Application.Tenants;
using CoreAndSkill.Core.Infrastructure.Jobs;
using CoreAndSkill.Core.IntegrationTests.Support;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Jobs;

// CHƯA CHẠY THỬ TRONG MÔI TRƯỜNG NÀY — không có Docker daemon lúc viết, nên các test dưới đây CHƯA
// TỪNG được thực thi một lần nào: chúng biên dịch được, chỉ vậy thôi. Đừng đọc sự tồn tại của chúng
// là bằng chứng chúng xanh. CI là nơi chạy chúng lần ĐẦU TIÊN.
// Gỡ khối chú thích này ngay khi có người chạy thật và thấy kết quả.
//
// Nghiệm thu B3 §5 mục 3 (docs/wiki-core/be/trien-khai/04-b3-van-hanh.md): "Xoá dữ liệu quá hạn theo
// chính sách → thứ nhật ký đang tham chiếu KHÔNG bị xoá theo".
//
// Core v1 KHÔNG đăng ký IStaleDataCleaner nào (docs/wiki-core/be/10-data-retention.md §8: job xoá
// cứng theo chính sách lưu giữ "cần chính sách nghiệp vụ trước" — nó thuộc dự án hạ nguồn). Nên mục
// nghiệm thu này KHÔNG kiểm được bằng thứ Core tự sinh ra: phải đóng vai dự án hạ nguồn, ĐĂNG KÝ một
// cleaner giả vào DI thật rồi xem điều gì xảy ra. Đó là việc lớp này làm.
//
// Cleaner giả ở đây KHÔNG mang chính sách lưu giữ nào (bao lâu là "quá hạn", bảng nào được dọn) — đó
// đúng là phần Core chưa có và test không được tự bịa. Nó chỉ đóng vai "một cleaner xoá cứng một bản
// ghi nghiệp vụ", vừa đủ để ba điều dưới đây kiểm được:
//   §1. StaleDataCleanupHostedService THẬT SỰ chạy cleaner đã đăng ký — không phải chỉ dựng lên rồi thôi;
//   §2. cleaner xoá đúng thứ nó nhắm, và cú xoá đó THẬT SỰ lan theo khoá ngoại CASCADE;
//   §3. dòng nhật ký kiểm toán đang tham chiếu tới bản ghi bị xoá KHÔNG biến mất theo.
// §1 và §2 là TIỀN ĐỀ: thiếu chúng thì §3 xanh vì không có gì xảy ra cả, chứ không phải vì luật đúng.
//
// ⚠ RANH GIỚI của điều §3 — đọc trước khi trích test này làm bằng chứng:
// core.audit_log trỏ tới bản ghi đích bằng CẶP CỘT VĂN BẢN (target_type, target_id), KHÔNG có khoá
// ngoại. AuditLogConfiguration.cs nói thẳng ("không FK tới app_user"), và
// 0003__core__add-audit-log.sql chỉ khai fk_audit_log_tenant_id với fk_audit_log_actor_tenant_id.
// Vì vậy với quan hệ này, "không bị xoá lây" là tính chất của CÁCH VIẾT CLEANER cộng với việc không
// có đường cascade nào dẫn tới audit_log — KHÔNG phải một ràng buộc lược đồ chặn lại. Một cleaner hạ
// nguồn cố tình nhét "DELETE FROM core.audit_log ..." vào lô của nó thì không gì chặn được ở tầng DB
// (chặn thật nằm ở quyền tài khoản ứng dụng — luật M13, docs/database/script-runbook.md §3.6, không
// kiểm được ở đây vì fixture áp schema bằng superuser). Test §3 là thứ bắt được việc đó.
// Quan hệ tenant_id thì ngược lại: có khoá ngoại RESTRICT và DB chặn thật — §4 kiểm đúng nửa đó.
//
// Cần Docker daemon — dựng PostgreSQL thật qua Testcontainers (PostgresFixture). Máy không có
// Docker: loại nhóm này bằng `--filter "Category!=RequiresDocker"`.
// Xem docs/wiki-core/be/04-testing-strategy.md §4.2. CI không bao giờ dùng filter đó — CI luôn có Docker daemon.
[Trait("Category", "RequiresDocker")]
[Collection(PostgresCollection.Name)]
public sealed class StaleDataRetentionAuditSurvivalDatabaseTests : IAsyncLifetime
{
    private const string Password = "Passw0rd-Test1";
    private const string TenantCode = "DV-RETENTION";
    private const string RoleCreateAction = "core.role.create";
    private const string RoleTargetType = "core.role";

    // Lượt quét đầu tiên chạy trên luồng nền lúc host khởi động — chờ TÍN HIỆU từ chính cleaner, không
    // ngủ một khoảng rồi đoán. Hết hạn này là một thất bại CÓ THẬT (job không chạy), không phải "máy
    // chậm": ExecuteAsync gọi RunOnceAsync ngay, trước cả nhịp timer đầu tiên.
    private static readonly TimeSpan FirstScanTimeout = TimeSpan.FromSeconds(30);

    // Thứ tự xoá đi từ con lên cha (docs/wiki-core/be/10-data-retention.md §4 "con trước, cha sau").
    // core.tenant đứng CUỐI — và đó là câu lệnh phải vỡ, vì core.audit_log vẫn giữ đơn vị lại.
    private static readonly string[] TenantPurgeStatements =
    [
        "DELETE FROM core.menu_item_role WHERE tenant_id = @t",
        "DELETE FROM core.menu_item WHERE tenant_id = @t",
        "DELETE FROM core.role_permission WHERE tenant_id = @t",
        "DELETE FROM core.app_user_role WHERE tenant_id = @t",
        "DELETE FROM core.app_user_claim WHERE tenant_id = @t",
        "DELETE FROM core.app_user_login WHERE tenant_id = @t",
        "DELETE FROM core.app_user_token WHERE tenant_id = @t",
        "DELETE FROM core.app_role_claim WHERE tenant_id = @t",
        "DELETE FROM core.app_user WHERE tenant_id = @t",
        "DELETE FROM core.app_role WHERE tenant_id = @t",
        "DELETE FROM core.tenant WHERE id = @t",
    ];

    private readonly PostgresFixture _db;
    private readonly RecordingStaleDataCleaner _cleaner;
    private readonly CoreWebApplicationFactory _factory;

    public StaleDataRetentionAuditSurvivalDatabaseTests(PostgresFixture db)
    {
        _db = db;
        _cleaner = new RecordingStaleDataCleaner(db.ConnectionString);

        // configureServices chạy SAU đăng ký của ứng dụng, nên đây đúng là đường một dự án hạ nguồn
        // đăng ký cleaner của nó: Core không biết gì về kiểu này, chỉ thấy IEnumerable<IStaleDataCleaner>.
        _factory = new CoreWebApplicationFactory(
            new Dictionary<string, string?> { ["ConnectionStrings:Core"] = db.ConnectionString },
            services =>
            {
                services.AddSingleton<ITenantSeedSource, FakeTenantSeedSource>();
                services.AddSingleton<IStaleDataCleaner>(_cleaner);
            });
    }

    public Task InitializeAsync() => _db.ResetAsync();

    public async Task DisposeAsync() => await _factory.DisposeAsync();

    // ---- §1. Job có THẬT SỰ chạy cleaner đã đăng ký không -------------------------------------

    // ADR-0060: job chỉ GỌI cleaner đã đăng ký — không truyền mốc thời gian nào; ngưỡng lưu giữ là của từng cleaner.
    [Fact]
    public async Task HostedService_OnStartup_RunsRegisteredCleaner()
    {
        await StartHostAndWaitForFirstScanAsync();

        _cleaner.CallCount.ShouldBe(1);
    }

    // Gỡ AddHostedService<StaleDataCleanupHostedService>() khỏi DI thì test này đỏ ở đây, trước khi
    // các test dưới kịp đỏ vì một lý do khó đọc hơn.
    [Fact]
    public async Task StaleDataCleanupHostedService_IsRegisteredExactlyOnce()
    {
        await StartHostAndWaitForFirstScanAsync();

        _factory.Services.GetServices<IHostedService>()
            .OfType<StaleDataCleanupHostedService>()
            .Count()
            .ShouldBe(1);
    }

    [Fact]
    public async Task OneMoreScanCycle_InvokesRegisteredCleaner_ExactlyOneMoreTime()
    {
        var tenant = await ArrangeTenantAsync();
        var roleId = await ReadSeedRoleIdAsync(tenant.TenantId);

        _cleaner.CallCount.ShouldBe(1); // lượt quét lúc khởi động

        await RunOneScanDeletingRoleAsync(roleId);

        _cleaner.CallCount.ShouldBe(2);
    }

    // ---- §2. Cleaner xoá ĐÚNG thứ nó nhắm -----------------------------------------------------

    // Tiền đề của §3: "nhật ký còn nguyên" chỉ có nghĩa khi cú xoá thật sự đã xảy ra VÀ thật sự lan
    // được theo khoá ngoại. Không có khẳng định này thì §3 cũng xanh trên một database mà mọi cú xoá
    // đều không đi tới đâu cả.
    [Fact]
    public async Task Cleaner_HardDeletesTargetedRole_AndDeleteCascadesToRolePermission()
    {
        var tenant = await ArrangeTenantAsync();
        var roleId = await ReadSeedRoleIdAsync(tenant.TenantId);

        (await CountRolesAsync(roleId)).ShouldBe(1);
        (await CountRolePermissionsAsync(roleId)).ShouldBe(1);

        await RunOneScanDeletingRoleAsync(roleId);

        _cleaner.LastDeletedCount.ShouldBe(1);                  // số bản ghi cleaner báo về cho job
        (await CountRolesAsync(roleId)).ShouldBe(0);            // xoá đúng thứ nó nhắm
        (await CountRolePermissionsAsync(roleId)).ShouldBe(0);  // cascade THẬT SỰ chạy
    }

    // ---- §3. Nghiệm thu B3 §5 mục 3 -----------------------------------------------------------

    [Fact]
    public async Task HardDeletingRole_DoesNotDeleteAuditRowsReferencingThatRole()
    {
        // Arrange
        var tenant = await ArrangeTenantAsync();
        var roleId = await ReadSeedRoleIdAsync(tenant.TenantId);

        var auditBefore = await ReadAuditRowsAsync();

        // Ghim chống "xanh vì chưa bao giờ có dòng nhật ký nào": phải có ít nhất một dòng ĐANG THAM
        // CHIẾU tới đúng vai trò sắp bị xoá, nếu không thì chẳng có gì để mà sống sót.
        auditBefore
            .Where(r => r.TargetType == RoleTargetType && r.TargetId == roleId.ToString())
            .ShouldNotBeEmpty();

        // Act
        await RunOneScanDeletingRoleAsync(roleId);

        // Ghim chống "xanh vì cleaner không xoá gì": bản ghi đích phải thật sự biến mất.
        (await CountRolesAsync(roleId)).ShouldBe(0);

        // Assert — không một dòng nhật ký nào mất đi, kể cả dòng trỏ tới bản ghi vừa bị xoá.
        (await ReadAuditRowsAsync()).ShouldBe(auditBefore, ignoreOrder: true);
    }

    // Dòng nhật ký còn nguyên chưa đủ — nó phải còn ĐỌC ĐƯỢC. Nhãn hiển thị chụp tại thời điểm ghi là
    // thứ giữ cho dòng nhật ký có nghĩa sau khi bản ghi gốc không còn (10-data-retention.md §4, §5.2).
    [Fact]
    public async Task AuditRowOfDeletedRole_KeepsTargetDisplayCapturedAtWriteTime()
    {
        // Arrange
        var tenant = await ArrangeTenantAsync();
        var roleId = await ReadSeedRoleIdAsync(tenant.TenantId);

        var createRowBefore = (await ReadAuditRowsAsync())
            .Single(r => r.ActionCode == RoleCreateAction && r.TargetId == roleId.ToString());
        createRowBefore.TargetDisplay.ShouldBe(FakeTenantSeedSource.RoleName);

        // Act
        await RunOneScanDeletingRoleAsync(roleId);

        // Assert
        (await CountRolesAsync(roleId)).ShouldBe(0); // ghim: bản ghi gốc đã không còn

        (await ReadAuditRowsAsync())
            .Single(r => r.ActionCode == RoleCreateAction && r.TargetId == roleId.ToString())
            .ShouldBe(createRowBefore);
    }

    // ---- §4. Nửa còn lại của cùng một luật: quan hệ CÓ khoá ngoại ------------------------------

    // Với tenant_id thì "nhật ký đang tham chiếu" được lược đồ bảo vệ thật: fk_audit_log_tenant_id là
    // ON DELETE RESTRICT (0003__core__add-audit-log.sql). Một cleaner hạ nguồn dọn cả một đơn vị quá
    // hạn sẽ bị DB TỪ CHỐI chừng nào còn dòng nhật ký của đơn vị đó — và job phải chịu được cú từ chối
    // đó: bắt, ghi log, không ném ra ngoài (StaleDataCleanupHostedService.RunOnceAsync).
    [Fact]
    public async Task Cleaner_DeletingTenantStillReferencedByAuditLog_IsRejectedByDatabase_AndNothingIsLost()
    {
        // Arrange
        var tenant = await ArrangeTenantAsync();
        var auditBefore = await ReadAuditRowsAsync();
        auditBefore.ShouldNotBeEmpty();

        _cleaner.Work = async (connection, ct) =>
        {
            // Lô chạy trong MỘT giao dịch tường minh — không dựa vào việc Npgsql có tự bọc nhiều câu
            // lệnh trong một giao dịch ngầm hay không. Vỡ ở câu nào thì cả lô cuốn lại.
            await using var transaction = await connection.BeginTransactionAsync(ct);
            var affected = 0;

            foreach (var sql in TenantPurgeStatements)
            {
                await using var command = new NpgsqlCommand(sql, connection, transaction);
                command.Parameters.Add(new NpgsqlParameter("t", tenant.TenantId));
                affected += await command.ExecuteNonQueryAsync(ct);
            }

            await transaction.CommitAsync(ct);
            return affected;
        };

        // Act — job KHÔNG được ném ra ngoài: một cleaner vỡ là việc của log, không phải của tiến trình.
        await Should.NotThrowAsync(() => Job.RunOnceAsync(CancellationToken.None));

        // Assert — chính DB chặn, và chặn vì đúng ràng buộc của bảng nhật ký (không phải vì bảng khác).
        var failure = _cleaner.LastException.ShouldBeOfType<PostgresException>();
        failure.SqlState.ShouldBe(PostgresErrorCodes.ForeignKeyViolation);
        failure.ConstraintName.ShouldNotBeNull();
        failure.ConstraintName!.ShouldStartWith("fk_audit_log_");

        // Lô cuốn lại trọn vẹn: không dòng nhật ký nào mất, đơn vị vẫn còn.
        (await ReadAuditRowsAsync()).ShouldBe(auditBefore, ignoreOrder: true);
        (await CountTenantsAsync(tenant.TenantId)).ShouldBe(1);
    }

    // ---- Hạ tầng của test ---------------------------------------------------------------------

    // Instance hosted service ĐANG CHẠY trong host, lấy qua DI — không dựng một bản riêng cho test.
    // internal của Core.Infrastructure nhìn thấy được nhờ InternalsVisibleTo (be-architecture.md §9.1).
    private StaleDataCleanupHostedService Job => _factory.Services
        .GetServices<IHostedService>()
        .OfType<StaleDataCleanupHostedService>()
        .Single();

    // Chạm Services => host khởi động => BackgroundService.ExecuteAsync chạy lượt quét đầu tiên NGAY.
    // Lượt đó xảy ra TRƯỚC khi test kịp dựng dữ liệu, nên cleaner lúc này chưa được nạp việc (Work =
    // null) và không xoá gì. Chờ xong lượt đó rồi mới nạp việc — không để hai lượt chồng lên nhau.
    private async Task StartHostAndWaitForFirstScanAsync()
    {
        _ = _factory.Services;

        try
        {
            await _cleaner.FirstScan.WaitAsync(FirstScanTimeout);
        }
        catch (TimeoutException)
        {
            throw new InvalidOperationException(
                $"StaleDataCleanupHostedService không gọi IStaleDataCleaner nào trong {FirstScanTimeout.TotalSeconds:0}s "
              + "sau khi host khởi động. Hoặc job không còn được đăng ký, hoặc nó không còn quét ngay lúc khởi động, "
              + "hoặc IEnumerable<IStaleDataCleaner> không nhìn thấy cleaner mà dự án hạ nguồn đăng ký.");
        }
    }

    // Dựng dữ liệu bằng đường THẬT (TenantProvisioningService) — nhật ký kiểm toán phải do
    // AuditLogInterceptor ghi ra, không do test tự chèn vào bảng. Test tự chèn thì nó chỉ chứng minh
    // được điều nó vừa tự viết.
    private async Task<TenantProvisioningResult> ArrangeTenantAsync()
    {
        await StartHostAndWaitForFirstScanAsync();

        using var scope = _factory.Services.CreateScope();

        var result = await ProvisioningInTransaction.RunAsync(scope.ServiceProvider, (service, ct) => service.CreateTenantAsync(
            new CreateTenantInput(
                Code: TenantCode,
                Name: "Đơn vị kiểm thử lưu giữ",
                IsSystem: false,
                AdminUserName: "admin",
                AdminPassword: Password,
                AdminHasPermissionBypass: true,
                AdminIsSystemOperator: false,
                AdminEmail: "admin@dv-retention.example.com",
                AdminFullName: "Quản trị đơn vị"),
            ct));

        result.IsSuccess.ShouldBeTrue();
        return result.Value;
    }

    // Một chu kỳ quét, kích hoạt bằng seam internal RunOnceAsync — KHÔNG lái PeriodicTimer thật
    // (chu kỳ mặc định 24 giờ; chờ nó là chờ vô ích, và hạ chu kỳ xuống cho test là đổi cấu hình sản phẩm).
    private async Task RunOneScanDeletingRoleAsync(Guid roleId)
    {
        _cleaner.Work = async (connection, ct) =>
        {
            await using var command = new NpgsqlCommand("DELETE FROM core.app_role WHERE id = @r", connection);
            command.Parameters.Add(new NpgsqlParameter("r", roleId));
            return await command.ExecuteNonQueryAsync(ct);
        };

        await Job.RunOnceAsync(CancellationToken.None);

        // RunOnceAsync NUỐT mọi lỗi của cleaner. Không kiểm ở đây thì một cleaner vỡ giữa chừng sẽ
        // trông y hệt một cleaner chạy xong, và các khẳng định sau đó xanh vì lý do sai.
        _cleaner.LastException.ShouldBeNull();
    }

    private async Task<Guid> ReadSeedRoleIdAsync(Guid tenantId)
    {
        var ids = await _db.QueryAsync(
            "SELECT id FROM core.app_role WHERE tenant_id = @t AND name = @n",
            reader => reader.GetGuid(0),
            new NpgsqlParameter("t", tenantId),
            new NpgsqlParameter("n", FakeTenantSeedSource.RoleName));

        return ids.ShouldHaveSingleItem();
    }

    private Task<long> CountRolesAsync(Guid roleId)
        => _db.CountAsync("SELECT count(*) FROM core.app_role WHERE id = @r", new NpgsqlParameter("r", roleId));

    private Task<long> CountRolePermissionsAsync(Guid roleId)
        => _db.CountAsync("SELECT count(*) FROM core.role_permission WHERE role_id = @r", new NpgsqlParameter("r", roleId));

    private Task<long> CountTenantsAsync(Guid tenantId)
        => _db.CountAsync("SELECT count(*) FROM core.tenant WHERE id = @t", new NpgsqlParameter("t", tenantId));

    private sealed record AuditRow(
        Guid Id, Guid TenantId, string ActionCode, string TargetType, string TargetId, string? TargetDisplay);

    // Đọc THẲNG qua Npgsql — không qua CoreDbContext, để không bộ lọc hay interceptor nào che khuất
    // việc một dòng đã biến mất.
    private Task<IReadOnlyList<AuditRow>> ReadAuditRowsAsync()
        => _db.QueryAsync(
            """
            SELECT id, tenant_id, action_code, target_type, target_id, target_display
            FROM core.audit_log
            ORDER BY occurred_at, id
            """,
            reader => new AuditRow(
                reader.GetGuid(0),
                reader.GetGuid(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4),
                reader.IsDBNull(5) ? null : reader.GetString(5)));

    // Đóng vai một IStaleDataCleaner của dự án hạ nguồn: Core v1 không có cái nào
    // (docs/wiki-core/be/10-data-retention.md §8), nên không có gì khác để đăng ký.
    //
    // Work = null là trạng thái mặc định CÓ CHỦ Ý: lượt quét lúc host khởi động chạy trước khi test
    // dựng xong dữ liệu, và một cleaner xoá ngay tại đó sẽ lấy mất tiền đề của mọi test bên dưới.
    private sealed class RecordingStaleDataCleaner(string connectionString) : IStaleDataCleaner
    {
        private readonly TaskCompletionSource _firstScan = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _callCount;

        public string Name => "test-retention-cleaner";

        // Hoàn thành ở lần CleanAsync đầu tiên — mốc để test biết lượt quét lúc khởi động đã xong,
        // thay cho việc ngủ một khoảng rồi hy vọng.
        public Task FirstScan => _firstScan.Task;

        public int CallCount => Volatile.Read(ref _callCount);

        public int LastDeletedCount { get; private set; }

        public Exception? LastException { get; private set; }

        public Func<NpgsqlConnection, CancellationToken, Task<int>>? Work { get; set; }

        public async Task<int> CleanAsync(CancellationToken ct)
        {
            Interlocked.Increment(ref _callCount);
            LastException = null;
            _firstScan.TrySetResult();

            var work = Work;
            if (work is null)
            {
                LastDeletedCount = 0;
                return 0;
            }

            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(ct);

            try
            {
                LastDeletedCount = await work(connection, ct);
            }
            catch (Exception ex)
            {
                LastException = ex;
                throw; // để RunOnceAsync xử lý đúng như với một cleaner thật vỡ giữa chừng
            }

            return LastDeletedCount;
        }
    }
}
