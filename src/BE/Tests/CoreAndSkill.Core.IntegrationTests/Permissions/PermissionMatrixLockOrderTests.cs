using CoreAndSkill.Core.Infrastructure.Permissions;
using CoreAndSkill.Core.IntegrationTests.Support;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Permissions;

// PUT /permissions/matrix — docs/wiki-core/be/06-concurrency-control.md §6.3 luật 3: so `version` với database TRONG
// CÙNG transaction ghi, lệch thì không ghi gì. Với READ COMMITTED, "cùng transaction" chưa đủ: hai lượt lưu cùng giữ
// một version đều đọc thấy trạng thái cũ, cùng qua phép so, cùng commit. Phép kiểm-rồi-ghi phải được tuần tự hoá theo
// đơn vị — khoá TRƯỚC khi đọc bất cứ thứ gì dùng để so.
//
// Test này chỉ chứng minh THỨ TỰ lệnh (không cần database). Hai request song song thật trên PostgreSQL — đúng một bên
// nhận 409 — là PermissionMatrixDatabaseTests (RequiresDocker).
public sealed class PermissionMatrixLockOrderTests
{
    private static readonly IReadOnlyDictionary<Guid, IReadOnlyList<Guid>> AnyEntries = new Dictionary<Guid, IReadOnlyList<Guid>>();

    [Fact]
    public async Task ReplaceMatrix_TakesTheTenantLock_BeforeReadingAnything()
    {
        var tenantId = Guid.NewGuid();
        var recorder = new SqlRecorder();
        await using var db = OfflineCoreDbContext.Create(tenantId, recorder.Interceptors);
        await using var transaction = await db.Database.BeginTransactionAsync();

        await Should.ThrowAsync<SqlRecorder.ReaderReachedException>(
            () => new PermissionMatrixService(db).ReplaceMatrixAsync("sha256:00000000", AnyEntries, CancellationToken.None));

        // Lệnh đọc đầu tiên ném — mọi lệnh đứng trước nó là thứ chạy TRƯỚC khi service đọc được gì.
        var beforeFirstRead = recorder.Executed.Take(recorder.Executed.Count - 1).ToList();

        var lockIndex = beforeFirstRead.FindIndex(sql => sql.Contains("pg_advisory_xact_lock", StringComparison.Ordinal));
        lockIndex.ShouldBeGreaterThanOrEqualTo(0, $"Không có khoá trước lệnh đọc đầu tiên. Trình tự: {string.Join(" | ", recorder.Executed)}");
        beforeFirstRead[lockIndex].ShouldContain(tenantId.ToString(), Case.Insensitive, "khoá theo ĐƠN VỊ — hai đơn vị không chặn nhau");

        // 06-concurrency-control.md §7 quy tắc 3: luôn có thời hạn chờ.
        beforeFirstRead.Take(lockIndex).ShouldContain(sql => sql.Contains("lock_timeout", StringComparison.Ordinal));
    }

    // Khoá tư vấn cấp transaction ngoài transaction là nhả ngay sau câu lệnh — bảo vệ biến mất trong im lặng.
    [Fact]
    public async Task ReplaceMatrix_OutsideATransaction_Throws_BeforeAnySql()
    {
        var recorder = new SqlRecorder();
        await using var db = OfflineCoreDbContext.Create(Guid.NewGuid(), recorder.Interceptors);

        await Should.ThrowAsync<InvalidOperationException>(
            () => new PermissionMatrixService(db).ReplaceMatrixAsync("sha256:00000000", AnyEntries, CancellationToken.None));

        recorder.Executed.ShouldBeEmpty();
    }
}
