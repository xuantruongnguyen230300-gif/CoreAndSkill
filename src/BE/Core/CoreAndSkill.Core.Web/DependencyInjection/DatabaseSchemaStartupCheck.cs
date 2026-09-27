using System.Text;
using CoreAndSkill.Core.Application.Configuration;
using CoreAndSkill.Core.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace CoreAndSkill.Core.Web.DependencyInjection;

// Luật E8 lúc khởi động — docs/database/script-runbook.md §5.1–§5.3. DB thiếu migration mà assembly biết ⇒ TỪ CHỐI KHỞI ĐỘNG
// (ném, không bắt). DB có thừa migration mà assembly không biết ⇒ CHỈ cảnh báo, vẫn khởi động (hợp lệ ở bước "mở rộng",
// migration-policy.md §5).
//
// Không kết nối được DB thì phép kiểm không chạy được — và "không kiểm được" KHÔNG được thành "coi như khớp": app lên với một
// DB có thể đang thiếu migration đúng là cách hỏng §5.3 gọi là tệ nhất. Nên:
//   • lỗi kết nối TẠM THỜI (từ chối kết nối, hết hạn kết nối, DB đang khởi động — NpgsqlException.IsTransient): thử lại, cách
//     nhau Core:SchemaCheck:ConnectRetryIntervalSeconds, tổng không quá Core:SchemaCheck:ConnectWaitSeconds (tính từ lần thử
//     đầu, theo TimeProvider). Kết nối được ⇒ chạy phép kiểm E8 như thường. Hết hạn ⇒ ném, tiến trình dừng, bộ điều phối
//     khởi động lại;
//   • lỗi KHÔNG tạm thời (sai mật khẩu, database không tồn tại, thiếu quyền…): ném ngay — chờ thêm không đổi được kết quả.
//
// Liveness/readiness (docs/wiki-core/be/07-observability.md §8) không đổi: /health/live vẫn không chạm DB, /health/ready vẫn
// kiểm DB và migration ở MỖI lần gọi. Phép kiểm này chạy TRƯỚC khi server nhận request, nên trong lúc nó chờ, tiến trình chưa
// trả lời endpoint nào — kể cả /health/live. Bộ điều phối phải cho giai đoạn khởi động dài hơn ConnectWaitSeconds rồi mới áp
// liveness; không thì nó giết tiến trình giữa lúc chờ, đúng vòng lặp khởi động lại mà §8 cảnh báo.
internal sealed class DatabaseSchemaStartupCheck(
    ISchemaVerifier verifier,
    IOptions<CoreSchemaCheckOptions> options,
    TimeProvider timeProvider,
    ILogger<DatabaseSchemaStartupCheck> logger)
{
    public async Task RunAsync(CancellationToken ct)
    {
        var reports = await InspectWithRetryAsync(ct);

        foreach (var report in reports.Where(r => r.UnknownToApplication.Count > 0))
        {
            logger.LogWarning(
                "Database di TRUOC code cho {Context}: {Count} migration database co ma ban build nay khong biet ({List}). " +
                "Hop le neu dang o buoc 'mo rong'; neu khong, kiem tra da chay nham script len nham moi truong chua.",
                report.ContextName,
                report.UnknownToApplication.Count,
                string.Join(", ", report.UnknownToApplication));
        }

        var blocking = reports.Where(r => r.MissingInDatabase.Count > 0).ToList();
        if (blocking.Count == 0)
            return;

        var message = new StringBuilder()
            .AppendLine("KHOI DONG BI TU CHOI — database thieu migration.")
            .AppendLine();

        foreach (var report in blocking)
        {
            message.AppendLine($"  {report.ContextName} thieu {report.MissingInDatabase.Count} migration:");
            foreach (var id in report.MissingInDatabase)
                message.AppendLine($"    - {id}");

            message.AppendLine("    Script can chay: database/scripts/core/");
            message.AppendLine();
        }

        message
            .AppendLine("Cach xu ly: doc docs/database/script-runbook.md muc 3.4 (chay phan con thieu).")
            .AppendLine("Sau khi chay xong, khoi dong lai. KHONG bo qua kiem tra nay.");

        throw new InvalidOperationException(message.ToString());
    }

    private async Task<IReadOnlyList<SchemaDriftReport>> InspectWithRetryAsync(CancellationToken ct)
    {
        var settings = options.Value;
        var interval = TimeSpan.FromSeconds(settings.ConnectRetryIntervalSeconds);
        var startedAt = timeProvider.GetUtcNow();
        var deadline = startedAt + TimeSpan.FromSeconds(settings.ConnectWaitSeconds);

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                var reports = await verifier.InspectAsync(ct);
                if (attempt > 1)
                    logger.LogInformation("Ket noi duoc database de kiem migration (E8) o lan thu {Attempt}.", attempt);

                return reports;
            }
            catch (Exception ex) when (FindNpgsqlException(ex) is { } npgsql)
            {
                var now = timeProvider.GetUtcNow();
                var elapsed = now - startedAt;
                var remaining = deadline - now;

                // Lần thử CUỐI cũng có dòng log của nó: ngoại lệ ném ra dưới đây đi thẳng khỏi Program.cs (không ai bắt), nên
                // nếu không ghi ở đây thì lần thử quyết định dừng tiến trình không để lại dòng nào qua ILogger.
                if (!npgsql.IsTransient)
                {
                    logger.LogError(
                        ex,
                        "Lan thu {Attempt}: khong ket noi duoc database de kiem migration (E8) — loi khong tam thoi. Tu choi khoi dong.",
                        attempt);
                    throw CannotReachDatabase(attempt, elapsed, "loi khong tam thoi — cho them khong doi duoc ket qua", ex);
                }

                if (remaining <= TimeSpan.Zero)
                {
                    logger.LogError(
                        ex,
                        "Lan thu {Attempt}: khong ket noi duoc database de kiem migration (E8) — het {WaitSeconds} giay cho. " +
                        "Tu choi khoi dong.",
                        attempt,
                        settings.ConnectWaitSeconds);
                    throw CannotReachDatabase(attempt, elapsed, $"het {settings.ConnectWaitSeconds} giay cho", ex);
                }

                var wait = remaining < interval ? remaining : interval;
                logger.LogWarning(
                    ex,
                    "Lan thu {Attempt}: khong ket noi duoc database de kiem migration (E8). Thu lai sau {WaitSeconds} giay; " +
                    "con cho toi da {RemainingSeconds} giay truoc khi tu choi khoi dong.",
                    attempt,
                    wait.TotalSeconds,
                    remaining.TotalSeconds);

                await Task.Delay(wait, timeProvider, ct);
            }
        }
    }

    // EF có thể bọc lỗi của Npgsql (chiến lược thử lại, lớp lịch sử migration) — tìm dọc chuỗi InnerException, không chỉ
    // xét tầng ngoài cùng.
    private static NpgsqlException? FindNpgsqlException(Exception ex)
    {
        for (var current = ex; current is not null; current = current.InnerException)
        {
            if (current is NpgsqlException npgsql)
                return npgsql;
        }

        return null;
    }

    // Không đưa chuỗi kết nối vào thông điệp — nó chứa mật khẩu.
    private static InvalidOperationException CannotReachDatabase(int attempts, TimeSpan elapsed, string reason, Exception inner)
        => new(
            new StringBuilder()
                .AppendLine(
                    $"KHOI DONG BI TU CHOI — khong ket noi duoc database de kiem migration (luat E8) sau {attempts} lan thu " +
                    $"trong {elapsed.TotalSeconds:0} giay ({reason}).")
                .AppendLine("Khong kiem duoc migration thi khong phuc vu request: database co the dang thieu migration.")
                .AppendLine(
                    "Cach xu ly: kiem tra ConnectionStrings:Core va database da nhan ket noi chua. Thoi gian cho: " +
                    $"{CoreSchemaCheckOptions.SectionName}:{nameof(CoreSchemaCheckOptions.ConnectWaitSeconds)}, " +
                    $"{CoreSchemaCheckOptions.SectionName}:{nameof(CoreSchemaCheckOptions.ConnectRetryIntervalSeconds)}.")
                .AppendLine("Tien trinh dung lai de bo dieu phoi khoi dong lai.")
                .ToString(),
            inner);
}
