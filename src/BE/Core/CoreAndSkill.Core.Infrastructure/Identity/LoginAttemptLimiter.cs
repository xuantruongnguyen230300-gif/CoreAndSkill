using System.Collections.Concurrent;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using CoreAndSkill.Core.Application.Identity;
using Microsoft.AspNetCore.Identity;

namespace CoreAndSkill.Core.Infrastructure.Identity;

// Hàng rào 3 của rate limit — docs/quy-uoc/be-api-controller.md §6.5. Bộ đếm trong BỘ NHỚ TIẾN
// TRÌNH (Singleton — ràng buộc 4 ở §6.3: hạn mức nhân lên nếu chạy nhiều instance, điều kiện bắt
// buộc phải giải trước khi mở instance thứ hai).
//
// Phần tên đăng nhập của khoá chuẩn hoá ĐÚNG như Identity chuẩn hoá normalized_user_name (§6.5): hai dạng
// Unicode của cùng một tên (é liền và e + dấu kết hợp) là MỘT tài khoản với Identity, nên phải chung MỘT bộ
// đếm — lệch thì kẻ dò đổi dạng chữ là được thêm hạn mức. KHÔNG inject ILookupNormalizer — đó là seam
// Scoped, và một Singleton không bao giờ được tiêu thụ một Scoped (docs/quy-uoc/be-architecture.md §5.2
// "luật cứng"); dùng thẳng một thể hiện UpperInvariantLookupNormalizer — lớp không trạng thái, đúng lớp
// Identity đăng ký mặc định. Test LoginAttemptLimiterTests so khoá với chính lớp đó.
//
// Khoá do người gọi ẨN DANH chọn (mã đơn vị + tên đăng nhập tuỳ ý) — không dọn thì bộ nhớ tăng theo số cặp
// bị dò, không có trần. Mỗi cửa sổ quét một lần, gỡ khoá đã hết mọi lần thử trong cửa sổ.
internal sealed class LoginAttemptLimiter(TimeProvider timeProvider) : ILoginAttemptLimiter
{
    // 10 lần / 5 phút cho mỗi LoginPartitionKey — mặc định đã chốt, be-api-controller.md §6.5.
    private const int PermitLimit = 10;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(5);

    private readonly ConcurrentDictionary<string, AttemptWindow> _attempts = new(StringComparer.Ordinal);
    private long _nextSweepUtcTicks;

    // Chỉ cho test đếm và đo khoá đang giữ (InternalsVisibleTo IntegrationTests).
    internal int TrackedKeyCount => _attempts.Count;

    internal IReadOnlyCollection<string> TrackedKeys => [.. _attempts.Keys];

    public ValueTask EnsureAttemptAllowedAsync(string tenantCode, string userName, CancellationToken ct)
    {
        var key = BuildKey(tenantCode, userName);
        var now = timeProvider.GetUtcNow();

        SweepIfDue(now);

        while (true)
        {
            var window = _attempts.GetOrAdd(key, static _ => new AttemptWindow());

            lock (window)
            {
                // Lượt quét vừa gỡ đúng cửa sổ này khỏi từ điển (giữa GetOrAdd và lock) — ghi vào nó là ghi vào
                // một cửa sổ không ai còn thấy, lần thử bị quên. Lấy lại cửa sổ đang có trong từ điển.
                if (window.Retired)
                    continue;

                window.Prune(now);

                if (window.Times.Count >= PermitLimit)
                {
                    var retryAfter = Window - (now - window.Times.Peek());
                    throw new LoginAttemptLimitExceededException(retryAfter > TimeSpan.Zero ? retryAfter : TimeSpan.FromSeconds(1));
                }

                window.Times.Enqueue(now);
                return ValueTask.CompletedTask;
            }
        }
    }

    // Tối đa một lượt quét mỗi cửa sổ, chỉ một luồng thắng — lượt quét là O(số khoá), không chạy theo mỗi request.
    private void SweepIfDue(DateTimeOffset now)
    {
        var due = Interlocked.Read(ref _nextSweepUtcTicks);
        if (now.UtcTicks < due)
            return;

        if (Interlocked.CompareExchange(ref _nextSweepUtcTicks, (now + Window).UtcTicks, due) != due)
            return;

        foreach (var pair in _attempts)
        {
            lock (pair.Value)
            {
                pair.Value.Prune(now);
                if (pair.Value.Times.Count > 0)
                    continue;

                // Gỡ NGUYÊN TỬ theo cặp (khoá, đúng cửa sổ này) và đánh dấu trong lock: luồng nào đã lấy được
                // cửa sổ này mà chưa kịp lock sẽ thấy Retired và lấy cửa sổ mới.
                pair.Value.Retired = true;
                _attempts.TryRemove(pair);
            }
        }
    }

    // Hai phần chuẩn hoá — để hai cặp khác nhau không bao giờ cho cùng một khoá (be-api-controller.md §6.5).
    // Mã đơn vị: đúng quy tắc tra đơn vị lúc đăng nhập (TenantLookupService — Trim + ToUpperInvariant). Tên đăng nhập:
    // đúng bộ chuẩn hoá Identity (NFC + ToUpperInvariant, KHÔNG cắt khoảng trắng — FindByNameAsync cũng không cắt).
    //
    // Ghép theo dạng "<độ dài phần đơn vị>:<phần đơn vị><phần tên>" — độ dài đặt ranh giới, nên không ký tự nào trong
    // hai phần (cả hai do người gọi ẩn danh gõ) dời được ranh giới đó. Ghép bằng ký tự phân cách thì dời được: cặp
    // ("DV␟A", "B") và ("DV", "A␟B") cho cùng một khoá.
    //
    // Khoá GIỮ trong từ điển là SHA-256 của chuỗi ghép, không phải chuỗi ghép: khoá sống suốt cửa sổ, và cỡ của nó không
    // được theo cỡ đầu vào do người gọi ẩn danh chọn. Validator chặn độ dài là lớp trước; lớp này không dựa vào nó.
    private static string BuildKey(string tenantCode, string userName)
    {
        var tenantPart = tenantCode.Trim().ToUpperInvariant();
        var userPart = UserNameNormalizer.NormalizeName(userName);
        var composite = string.Create(CultureInfo.InvariantCulture, $"{tenantPart.Length}:{tenantPart}{userPart}");
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(composite)));
    }

    private static readonly UpperInvariantLookupNormalizer UserNameNormalizer = new();

    private sealed class AttemptWindow
    {
        public Queue<DateTimeOffset> Times { get; } = new();

        public bool Retired { get; set; }

        public void Prune(DateTimeOffset now)
        {
            while (Times.Count > 0 && now - Times.Peek() > Window)
                Times.Dequeue();
        }
    }
}
