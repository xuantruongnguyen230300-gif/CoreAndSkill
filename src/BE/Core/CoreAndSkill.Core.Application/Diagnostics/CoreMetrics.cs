using System.Diagnostics.Metrics;

namespace CoreAndSkill.Core.Application.Diagnostics;

// Chỉ số nghiệp vụ — docs/wiki-core/be/07-observability.md §7, §9, §11 "Metric xuất ra hệ ngoài ❌
// chưa ở v1: khai chỉ số trong code trước; đấu nối khi có nơi nhận". System.Diagnostics.Metrics là
// BCL thuần (không package, không ASP.NET Core, không nhà cung cấp cụ thể) — hợp lệ ở Core.Application
// (luật A2). Chưa gọi AddOpenTelemetry()/AddMeter() ở đâu — Meter tồn tại và LISTEN ĐƯỢC ngay
// (vd. `dotnet-counters monitor CoreAndSkill.Core`), nhưng chưa xuất ra hệ ngoài nào cho tới khi có
// nơi nhận (Prometheus/OTLP…). Tên chỉ số theo khuôn OpenTelemetry semantic conventions
// (namespace.resource.action, đơn vị trong {}).
public static class CoreMetrics
{
    public const string MeterName = "CoreAndSkill.Core";

    private static readonly Meter Meter = new(MeterName);

    // docs/wiki-core/be/17-multi-tenant.md §11.1 — MỌI ca trượt của LoginCommandHandler (tenant
    // không tồn tại/ngưng hoạt động, sai mật khẩu, tài khoản bị khoá…) đều tăng chỉ số này.
    public static readonly Counter<long> LoginFailed = Meter.CreateCounter<long>(
        "core.auth.login_failed", unit: "{login}", description: "Số lần đăng nhập thất bại.");

    // docs/wiki-core/be/07-observability.md §6 "Sự kiện outbox thất bại quá N lần | Error | Cần
    // người can thiệp" — mở rộng cho MỌI job nền, không riêng outbox (Outbox chưa dựng ở B3).
    public static readonly Counter<long> BackgroundJobFailed = Meter.CreateCounter<long>(
        "core.job.failed", unit: "{job}", description: "Số lần job nền chạy lỗi, cần người can thiệp.");

    // ---- Outbox — docs/wiki-core/be/12-notifications.md §2, 07-observability.md §7. ----

    public static readonly Counter<long> OutboxDispatched = Meter.CreateCounter<long>(
        "core.outbox.dispatched", unit: "{message}", description: "Số dòng outbox đã phát xong.");

    public static readonly Counter<long> OutboxAttemptFailed = Meter.CreateCounter<long>(
        "core.outbox.attempt_failed", unit: "{attempt}", description: "Số lần thử phát một dòng outbox bị hỏng.");

    public static readonly Counter<long> OutboxDead = Meter.CreateCounter<long>(
        "core.outbox.dead", unit: "{message}", description: "Số dòng outbox chuyển sang `dead` — cần người can thiệp.");

    private static long _outboxPendingCount;
    private static double _outboxOldestPendingAgeSeconds;
    private static long _outboxDeadCount;

    // Chỉ số cảnh báo sớm tốt nhất của cả mảng sự kiện: tuổi dòng chưa phát cũ nhất (07 §7). Bộ phát
    // cập nhật ở MỖI nhịp quét; gauge chỉ ĐỌC giá trị đã đo, không truy vấn DB trong callback.
    public static void SetOutboxSnapshot(long pendingCount, double oldestPendingAgeSeconds, long deadCount)
    {
        Volatile.Write(ref _outboxPendingCount, pendingCount);
        Volatile.Write(ref _outboxOldestPendingAgeSeconds, oldestPendingAgeSeconds);
        Volatile.Write(ref _outboxDeadCount, deadCount);
    }

    public static readonly ObservableGauge<long> OutboxPending = Meter.CreateObservableGauge(
        "core.outbox.pending", () => Volatile.Read(ref _outboxPendingCount), unit: "{message}",
        description: "Số dòng outbox đang chờ phát.");

    public static readonly ObservableGauge<double> OutboxOldestPendingAge = Meter.CreateObservableGauge(
        "core.outbox.pending_oldest_age", () => Volatile.Read(ref _outboxOldestPendingAgeSeconds), unit: "s",
        description: "Tuổi dòng outbox chưa phát cũ nhất.");

    public static readonly ObservableGauge<long> OutboxDeadCount = Meter.CreateObservableGauge(
        "core.outbox.dead_count", () => Volatile.Read(ref _outboxDeadCount), unit: "{message}",
        description: "Số dòng outbox đang ở `dead`.");

    // ---- Tệp — docs/wiki-core/be/14-file-storage.md §5. ----

    public static readonly Counter<long> FileOrphansDeleted = Meter.CreateCounter<long>(
        "core.file.orphans_deleted", unit: "{file}", description: "Số tệp mồ côi (có trên đĩa, không có bản ghi) đã dọn.");

    public static readonly Counter<long> FileUnattachedExpired = Meter.CreateCounter<long>(
        "core.file.unattached_expired", unit: "{file}", description: "Số bản ghi tệp tải lên nhưng không bao giờ được gắn, đã gỡ vì quá hạn.");

    // Chiều "bản ghi có, tệp không": KHÔNG tự xoá — dấu hiệu có gì đó sai (§5.2), chỉ đếm và báo.
    public static readonly Counter<long> FileContentMissing = Meter.CreateCounter<long>(
        "core.file.content_missing", unit: "{file}", description: "Số lần phát hiện bản ghi tệp không còn nội dung trong kho lưu.");
}
