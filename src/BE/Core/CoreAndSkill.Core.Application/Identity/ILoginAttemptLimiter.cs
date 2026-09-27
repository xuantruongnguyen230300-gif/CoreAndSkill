namespace CoreAndSkill.Core.Application.Identity;

// Hạn mức đăng nhập theo LoginPartitionKey, kiểm TRONG handler đăng nhập — docs/quy-uoc/be-api-controller.md
// §6.5. Hiện thực ở Core.Infrastructure, khoá dựng từ mã đơn vị + tên đăng nhập THÔ (chuẩn hoá bên
// trong hiện thực).
public interface ILoginAttemptLimiter
{
    ValueTask EnsureAttemptAllowedAsync(string tenantCode, string userName, CancellationToken ct);
}

// Đường RA khi vượt hạn mức — exception, không Result: ResultToHttpMapper không có ErrorType nào về
// 429 (be-api-controller.md §1.1). IExceptionHandler của Core.Web bắt kiểu này và trả 429 kèm
// Retry-After (be-api-controller.md §2.4, §6.5).
public sealed class LoginAttemptLimitExceededException(TimeSpan retryAfter) : Exception
{
    public TimeSpan RetryAfter { get; } = retryAfter;
}
