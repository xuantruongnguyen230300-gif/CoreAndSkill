namespace CoreAndSkill.Core.Domain.Outbox;

// Khoảng lùi giữa các lần thử lại — docs/wiki-core/be/12-notifications.md §2.4. Tăng theo cấp số,
// có trần, kèm một chút ngẫu nhiên: không lùi dần thì một dịch vụ đang quá tải bị chính cơ chế thử
// lại đánh sập; không ngẫu nhiên thì nhiều dòng cùng hỏng một lúc sẽ cùng thử lại một lúc.
//
// Hàm thuần: nguồn ngẫu nhiên truyền vào (jitterSample trong [0,1]) để test cố định được kết quả.
public static class OutboxRetryPolicy
{
    public const int DefaultMaxAttempts = 5;

    public static readonly TimeSpan BaseDelay = TimeSpan.FromSeconds(5);
    public static readonly TimeSpan MaxDelay = TimeSpan.FromMinutes(15);

    // Mỗi lần thử hỏng nhân khoảng lùi lên chừng này: 5s, 30s, 3 phút, 15 phút (chạm trần).
    private const double Growth = 6;

    // Ngẫu nhiên +-20% quanh khoảng lùi.
    private const double JitterRatio = 0.2;

    // attemptCount: số lần đã thử, TÍNH CẢ lần vừa hỏng (>= 1).
    public static TimeSpan Delay(int attemptCount, double jitterSample)
    {
        var exponent = Math.Max(attemptCount - 1, 0);
        var seconds = Math.Min(
            BaseDelay.TotalSeconds * Math.Pow(Growth, exponent),
            MaxDelay.TotalSeconds);

        var factor = 1 - JitterRatio + 2 * JitterRatio * Math.Clamp(jitterSample, 0, 1);
        return TimeSpan.FromSeconds(seconds * factor);
    }
}
