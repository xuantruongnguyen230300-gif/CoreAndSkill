using CoreAndSkill.Core.Application.Identity;
using CoreAndSkill.Core.Infrastructure.Identity;
using CoreAndSkill.Core.IntegrationTests.Support;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Identity;

// Hàng rào 3 — docs/quy-uoc/be-api-controller.md §6.5 (10 lần / cửa sổ trượt 5 phút cho mỗi LoginPartitionKey).
// Bộ đếm là Singleton sống suốt tiến trình, và khoá của nó do NGƯỜI GỌI ẨN DANH chọn (mã đơn vị + tên đăng nhập
// tuỳ ý): khoá không bao giờ được dọn là bộ nhớ tăng theo số cặp mà một kẻ dò thử — không có trần.
public sealed class LoginAttemptLimiterTests
{
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(5);

    [Fact]
    public async Task KeysWhoseAttemptsAllExpired_AreRemoved_AfterTheWindow()
    {
        var clock = new MutableClock(DateTimeOffset.Parse("2026-09-21T08:00:00Z"));
        var limiter = new LoginAttemptLimiter(clock);

        for (var i = 0; i < 50; i++)
            await limiter.EnsureAttemptAllowedAsync("DV-A", $"do-tim-{i}", CancellationToken.None);

        limiter.TrackedKeyCount.ShouldBe(50); // chống test rỗng: có khoá để mà dọn

        clock.Advance(Window + TimeSpan.FromSeconds(1));
        await limiter.EnsureAttemptAllowedAsync("DV-A", "nguoi-that", CancellationToken.None);

        limiter.TrackedKeyCount.ShouldBe(1);
    }

    // Dọn khoá không được làm mất lần thử CÒN trong cửa sổ — hạn mức vẫn đúng 10 lần.
    [Fact]
    public async Task Cleanup_DoesNotForgetAttemptsStillInsideTheWindow()
    {
        var clock = new MutableClock(DateTimeOffset.Parse("2026-09-21T08:00:00Z"));
        var limiter = new LoginAttemptLimiter(clock);

        await limiter.EnsureAttemptAllowedAsync("DV-A", "cu", CancellationToken.None);
        clock.Advance(Window - TimeSpan.FromMinutes(1));

        for (var i = 0; i < 10; i++)
            await limiter.EnsureAttemptAllowedAsync("DV-A", "nan-nhan", CancellationToken.None);

        clock.Advance(TimeSpan.FromMinutes(2)); // lần thử của "cu" hết hạn ⇒ có một lượt dọn; "nan-nhan" thì chưa

        await Should.ThrowAsync<LoginAttemptLimitExceededException>(
            () => limiter.EnsureAttemptAllowedAsync("DV-A", "nan-nhan", CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task AKeyThatWasRemoved_StartsCountingAgain()
    {
        var clock = new MutableClock(DateTimeOffset.Parse("2026-09-21T08:00:00Z"));
        var limiter = new LoginAttemptLimiter(clock);

        for (var i = 0; i < 10; i++)
            await limiter.EnsureAttemptAllowedAsync("DV-A", "quantri", CancellationToken.None);

        clock.Advance(Window + TimeSpan.FromSeconds(1));

        for (var i = 0; i < 10; i++)
            await limiter.EnsureAttemptAllowedAsync("DV-A", "quantri", CancellationToken.None);

        await Should.ThrowAsync<LoginAttemptLimitExceededException>(
            () => limiter.EnsureAttemptAllowedAsync("DV-A", "quantri", CancellationToken.None).AsTask());
    }

    // be-api-controller.md §6.5: phần tên đăng nhập của khoá chuẩn hoá ĐÚNG như Identity chuẩn hoá normalized_user_name.
    // Hai tên mà Identity coi là MỘT tài khoản phải chung một bộ đếm (nếu không, kẻ dò đổi dạng Unicode của cùng tên là
    // được thêm 10 lần thử mỗi dạng); hai tên Identity coi là KHÁC nhau thì không chung.
    //
    // Đáp án lấy từ chính UpperInvariantLookupNormalizer — bộ chuẩn hoá Identity đăng ký mặc định — nên test đỏ nếu hai
    // bên lại lệch nhau, theo bất kỳ chiều nào.
    [Theory]
    [InlineData("Truongé", "Truongé")]  // é liền (NFC) và e + dấu sắc kết hợp (NFD)
    [InlineData("quantri", "QUANTRI")]
    [InlineData("quantri", " quantri")]              // Identity KHÔNG cắt khoảng trắng
    [InlineData("quantri", "quantri2")]
    public async Task UserNamePart_SharesACounter_ExactlyWhenIdentityNormalizesToTheSameName(string first, string second)
    {
        var identity = new Microsoft.AspNetCore.Identity.UpperInvariantLookupNormalizer();
        var sameAccountForIdentity = string.Equals(
            identity.NormalizeName(first), identity.NormalizeName(second), StringComparison.Ordinal);

        var limiter = new LoginAttemptLimiter(new MutableClock(DateTimeOffset.Parse("2026-09-21T08:00:00Z")));
        for (var i = 0; i < 10; i++)
            await limiter.EnsureAttemptAllowedAsync("DV-A", first, CancellationToken.None);

        var sharesCounter = false;
        try
        {
            await limiter.EnsureAttemptAllowedAsync("DV-A", second, CancellationToken.None);
        }
        catch (LoginAttemptLimitExceededException)
        {
            sharesCounter = true;
        }

        sharesCounter.ShouldBe(sameAccountForIdentity,
            $"Identity chuẩn hoá '{first}' → '{identity.NormalizeName(first)}', '{second}' → '{identity.NormalizeName(second)}'");
    }

    // Khoá do người gọi ẨN DANH chọn và được giữ suốt cửa sổ: nếu cỡ khoá theo cỡ đầu vào thì mỗi request mang một mã đơn
    // vị vài chục MB giữ vài chục MB trong bộ nhớ tiến trình tới hết cửa sổ, nhân với số khoá mỗi IP được thử. Cỡ khoá phải
    // cố định, không phụ thuộc đầu vào — validator chặn độ dài là lớp thứ nhất, lớp này không được dựa vào nó.
    [Fact]
    public async Task KeySize_DoesNotDependOnInputSize()
    {
        var limiter = new LoginAttemptLimiter(new MutableClock(DateTimeOffset.Parse("2026-09-21T08:00:00Z")));

        await limiter.EnsureAttemptAllowedAsync("DV-A", "an", CancellationToken.None);
        await limiter.EnsureAttemptAllowedAsync(new string('T', 1_000_000), new string('u', 1_000_000), CancellationToken.None);

        var keys = limiter.TrackedKeys;
        keys.Count.ShouldBe(2, "chống test rỗng: hai cặp khác nhau phải là hai khoá");
        keys.Select(k => k.Length).Distinct().ShouldHaveSingleItem();
        keys.ShouldAllBe(k => k.Length <= 64);
    }

    // be-api-controller.md §6.5 — hai cặp (mã đơn vị, tên đăng nhập) KHÁC nhau không bao giờ chung một bộ đếm. Cả hai phần
    // đều do người gọi ẩn danh gõ, nên ghép bằng một ký tự phân cách thì chính ký tự đó dời được ranh giới giữa hai phần:
    // kẻ dò đốt hạn mức của một cặp bằng cách thử một cặp khác cho ra cùng chuỗi khoá.
    [Theory]
    [InlineData("DV␟A", "quantri", "DV", "A␟quantri")]
    [InlineData("DV", "A␟quantri", "DV␟A", "quantri")]
    [InlineData("", "␟", "␟", "")]
    public async Task TwoDifferentPairs_NeverShareACounter_WhateverCharactersTheyContain(
        string firstTenant, string firstUser, string secondTenant, string secondUser)
    {
        var limiter = new LoginAttemptLimiter(new MutableClock(DateTimeOffset.Parse("2026-09-21T08:00:00Z")));
        for (var i = 0; i < 10; i++)
            await limiter.EnsureAttemptAllowedAsync(firstTenant, firstUser, CancellationToken.None);

        await Should.NotThrowAsync(
            () => limiter.EnsureAttemptAllowedAsync(secondTenant, secondUser, CancellationToken.None).AsTask());
        limiter.TrackedKeyCount.ShouldBe(2);
    }
}
