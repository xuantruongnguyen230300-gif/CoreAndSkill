using System.Text.Json;
using CoreAndSkill.Core.Application.Outbox;
using CoreAndSkill.Core.Domain.Jobs;
using CoreAndSkill.Core.Infrastructure.Jobs;
using CoreAndSkill.Core.IntegrationTests.Support;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Jobs;

// Câu quét việc dở dang lúc khởi động — ADR-0071 phương án C. KHÔNG cần Docker: CoreDbContext thật với mô hình và hai bộ
// lọc toàn cục thật, chỉ đọc câu SQL EF sinh ra (OfflineCoreDbContext).
//
// Thứ nhóm này canh là điều KHÔNG lộ ra khi chạy: câu quét chạy lúc khởi động, khi CHƯA có đơn vị nào trong ngữ cảnh. Bỏ
// lời gọi IgnoreQueryFilters([Tenant]) đi thì bộ lọc thành `tenant_id = NULL` và EF gấp cả câu về `WHERE FALSE` — phép
// khôi phục không đánh dấu gì, không ném gì, không log gì. Mọi việc `running` cũ treo mãi và không ai biết. Ca đối chứng
// bên dưới dựng lại đúng hình dạng hỏng đó để khẳng định ở trên không xanh rỗng (luật T6).
//
// Thứ nhóm này KHÔNG chứng minh: câu chạy được trên PostgreSQL và chọn đúng tập dòng — đó là việc của
// JobRecoveryCandidateParityDatabaseTests (RequiresDocker), nơi bản SQL thô cũ đứng làm oracle.
public sealed class JobRecoveryCandidateQueryTests
{
    private static string CandidateSql(Guid? tenantInContext)
    {
        using var db = OfflineCoreDbContext.Create(tenantInContext);
        return JobRecoveryHostedService.Candidates(db).ToQueryString();
    }

    [Theory]
    [InlineData(false)] // đúng trạng thái lúc khởi động: chưa request nào, chưa đơn vị nào
    [InlineData(true)]  // có đơn vị trong ngữ cảnh: hồi quy chỉ lộ ra ở đây nếu lời gọi bỏ lọc biến mất
    public void TheStartupScan_ReadsEveryTenant_OnBothTablesItTouches(bool tenantInContext)
    {
        var sql = CandidateSql(tenantInContext ? Guid.NewGuid() : null);

        // Hai bảng có tenant_id, không bảng nào được lọc theo đơn vị.
        sql.ShouldNotContain("tenant_id =", Case.Insensitive,
            "câu quét lúc khởi động phải đọc MỌI đơn vị — phạm vi đơn vị được mở lại theo TỪNG việc ở bước sau");
        sql.ShouldNotContain("ef_filter", Case.Insensitive,
            "không tham số bộ lọc toàn cục nào được còn lại trên câu này");
        sql.ShouldNotContain("WHERE FALSE", Case.Insensitive,
            "bộ lọc đơn vị bị gấp về FALSE nghĩa là câu quét không bao giờ trả dòng nào");

        // Bộ lọc XOÁ MỀM thì GIỮ — nó thay cho `is_deleted = false` của bản SQL thô cũ.
        sql.ShouldContain("is_deleted");

        // Cấu trúc còn lại giữ nguyên bản SQL thô cũ.
        sql.ShouldContain("FROM core.job");
        sql.ShouldContain("NOT EXISTS");
        sql.ShouldContain("FROM core.outbox_message");
        sql.ShouldContain($"'{JobStatus.Running}'");
        sql.ShouldContain($"'{JobStatus.Queued}'");
        sql.ShouldContain($"'{JobQueuedEvent.TypeKey}'");
    }

    // Đối chứng 1 (T6): bộ lọc đơn vị CÓ THẬT trên cả hai bảng — nên "không thấy tenant_id" ở trên là kết quả của lời gọi
    // bỏ lọc, không phải của một mô hình chưa gắn bộ lọc nào.
    [Fact]
    public void Control_TheTenantFilterIsRealOnBothTablesTheScanTouches()
    {
        using var db = OfflineCoreDbContext.Create(Guid.NewGuid());

        db.Jobs.Select(j => j.Id).ToQueryString().ShouldContain("tenant_id =");
        db.OutboxMessages.Select(o => o.Id).ToQueryString().ShouldContain("tenant_id =");
    }

    // Đối chứng 2 (T6): đúng hình dạng hỏng mà nhóm này canh — không có đơn vị trong ngữ cảnh thì bộ lọc gấp cả câu về
    // FALSE. Đây là lý do lời gọi bỏ lọc phải ở đó, viết ra thành một khẳng định chạy được thay vì một câu chú thích.
    [Fact]
    public void Control_WithoutIgnoringTheTenantFilter_AStartupScanWouldSilentlyFindNothing()
    {
        using var db = OfflineCoreDbContext.Create(null);

        db.Jobs.Where(j => j.Status == JobStatus.Running).Select(j => j.Id).ToQueryString()
            .ShouldContain("WHERE FALSE");
    }

    // Khoá `jobId` trong câu SQL và khoá `jobId` trong thân JSON mà OutboxInterceptor ghi ra phải là MỘT. Lệch nhau thì
    // `NOT EXISTS` luôn đúng và MỌI việc `queued` bị đánh dấu hỏng oan — không lỗi, không cảnh báo. Trước đây chuỗi
    // 'jobId' nằm chép tay trong câu SQL thô và không gì nối nó với bộ tuần tự hoá.
    [Fact]
    public void TheJsonProbe_MatchesThePayloadTheInterceptorActuallyWrites()
    {
        var jobId = Guid.NewGuid();
        var (prefix, suffix) = ProbeLiteral(CandidateSql(null));
        var probe = prefix + jobId + suffix;

        var written = JsonSerializer.Serialize(
            new JobQueuedEvent(jobId, "test.job", "_tmp/x"), typeof(JobQueuedEvent), OutboxJson.Options);

        using var probeDocument = JsonDocument.Parse(probe);
        using var writtenDocument = JsonDocument.Parse(written);

        var members = probeDocument.RootElement.EnumerateObject().ToList();
        members.ShouldNotBeEmpty("phép dò rỗng thì `@>` đúng với MỌI dòng — cổng xanh rỗng");

        foreach (var member in members)
        {
            writtenDocument.RootElement.TryGetProperty(member.Name, out var actual)
                .ShouldBeTrue($"thân JSON của dòng outbox không có khoá '{member.Name}' mà câu SQL đang dò");
            actual.GetString().ShouldBe(member.Value.GetString(), $"giá trị của khoá '{member.Name}'");
        }
    }

    // Tách hai mẩu chuỗi của `payload @> CAST('<prefix>' || <id>::text || '<suffix>' AS jsonb)`. Bám vào dấu nháy chứ
    // không bám vào bí danh bảng EF tự đặt. Không thấy hình dạng này thì test đỏ — đúng điều cần, vì phép dò JSON đã đổi.
    private static (string Prefix, string Suffix) ProbeLiteral(string sql)
    {
        const string opening = "@> CAST('";

        var opened = sql.IndexOf(opening, StringComparison.Ordinal);
        opened.ShouldBeGreaterThanOrEqualTo(0, $"không thấy phép dò chứa JSON trong câu:{Environment.NewLine}{sql}");

        var prefixFrom = opened + opening.Length;
        var prefixTo = sql.IndexOf('\'', prefixFrom);
        var suffixFrom = sql.IndexOf('\'', prefixTo + 1) + 1;
        var suffixTo = sql.IndexOf('\'', suffixFrom);

        prefixTo.ShouldBeGreaterThan(prefixFrom);
        suffixTo.ShouldBeGreaterThan(suffixFrom);

        return (sql[prefixFrom..prefixTo], sql[suffixFrom..suffixTo]);
    }
}
