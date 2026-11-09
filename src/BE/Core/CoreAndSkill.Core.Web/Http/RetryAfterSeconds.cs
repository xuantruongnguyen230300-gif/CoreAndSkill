using System.Globalization;

namespace CoreAndSkill.Core.Web.Http;

// Chỗ DUY NHẤT đổi một khoảng chờ thành chuỗi số giây của header Retry-After và messageParams.RetryAfterSeconds
// (be-api-controller.md §6.5, luật B24). Làm tròn lên, rồi chặn sàn 1 giây: Retry-After: 0 (hay số âm) bảo client thử
// lại ngay trong khi bộ đếm vẫn đang chặn. Sàn nằm ở biên HTTP để KHÔNG phụ thuộc bản cài ILoginAttemptLimiter nào đang
// chạy — một bản đăng ký sau AddCore ném 0 hay số âm vẫn ra header ≥ 1.
internal static class RetryAfterSeconds
{
    public static string Format(TimeSpan retryAfter)
    {
        var seconds = Math.Max(1, Math.Ceiling(retryAfter.TotalSeconds));
        return ((int)seconds).ToString(CultureInfo.InvariantCulture);
    }
}
