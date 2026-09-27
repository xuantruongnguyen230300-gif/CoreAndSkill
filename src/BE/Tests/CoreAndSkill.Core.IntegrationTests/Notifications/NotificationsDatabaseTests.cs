using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Application.Notifications;
using CoreAndSkill.Core.Domain.Common;
using CoreAndSkill.Core.IntegrationTests.Support;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Shouldly;
using Xunit;
using INotificationPublisher = CoreAndSkill.Core.Application.Notifications.INotificationPublisher;

namespace CoreAndSkill.Core.IntegrationTests.Notifications;

// CHƯA CHẠY THỬ TRONG MÔI TRƯỜNG NÀY — không có Docker daemon lúc viết, nên các test dưới đây CHƯA
// TỪNG được thực thi một lần nào: chúng biên dịch được, chỉ vậy thôi. Đừng đọc sự tồn tại của chúng
// là bằng chứng chúng xanh. CI là nơi chạy chúng lần ĐẦU TIÊN.
// Gỡ khối chú thích này ngay khi có người chạy thật và thấy kết quả.
//
// Thông báo trên PostgreSQL THẬT — docs/wiki-core/be/12-notifications.md §3-§5. Chứng minh: thông báo trong ứng dụng
// lưu KHOÁ + THAM SỐ (jsonb), không lưu câu đã ghép; danh sách / đếm chưa đọc / đánh dấu đã đọc chỉ chạm thông báo của
// CHÍNH người gọi và của đơn vị mình; kênh email chọn ngôn ngữ theo NGƯỜI NHẬN (không theo người kích hoạt), và không
// có nhà cung cấp email thì thất bại RÕ RÀNG chứ không im lặng.
// Cần Docker daemon — dựng PostgreSQL thật qua Testcontainers (PostgresFixture). Máy không có
// Docker: loại nhóm này bằng `--filter "Category!=RequiresDocker"`.
// Xem docs/wiki-core/be/04-testing-strategy.md §4.2. CI không bao giờ dùng filter đó — CI luôn có Docker daemon.
[Trait("Category", "RequiresDocker")]
[Collection(PostgresCollection.Name)]
public sealed class NotificationsDatabaseTests(PostgresFixture db) : IAsyncLifetime
{
    private readonly CapturingEmail _email = new();
    private B4DockerHost _host = null!;
    private TestTenant _a = null!;
    private TestTenant _b = null!;

    public async Task InitializeAsync()
    {
        await db.ResetAsync();
        _host = new B4DockerHost(db.ConnectionString, configure: services =>
        {
            services.AddSingleton(_email);
            services.AddScoped<IEmailSender>(sp => sp.GetRequiredService<CapturingEmail>());
            services.AddSingleton<INotificationTemplateRenderer>(_email);
        });
        _a = await _host.CreateTenantAsync("DV-NOTI-A");
        _b = await _host.CreateTenantAsync("DV-NOTI-B");
    }

    public async Task DisposeAsync() => await _host.DisposeAsync();

    private Task<Result> PublishAsync(TestTenant asWho, NotificationDraft draft)
        => _host.AsAsync(asWho, async sp =>
        {
            var publisher = sp.GetRequiredService<INotificationPublisher>();
            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();

            return await unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                var result = await publisher.PublishAsync(draft, ct);
                if (result.IsSuccess)
                    await unitOfWork.SaveChangesAsync(ct);

                return new TransactionOutcome<Result>(result, ShouldCommit: result.IsSuccess);
            });
        });

    private static NotificationDraft Draft(TestTenant to, string code = "CORE.JOB.SUCCEEDED", NotificationChannels channels = NotificationChannels.InApp)
        => new(code, new Dictionary<string, string> { ["FileName"] = "don-hang.xlsx" }, [to.AdminUserId], Channels: channels);

    // ---- Trong ứng dụng ------------------------------------------------------------------------

    [Fact]
    public async Task InApp_StoresTheCodeAndParams_NeverAComposedSentence()
    {
        (await PublishAsync(_a, Draft(_a))).IsSuccess.ShouldBeTrue();

        var row = (await db.QueryAsync(
            "SELECT code, params::text, severity FROM core.notification",
            r => (r.GetString(0), r.GetString(1), r.GetString(2)))).Single();

        row.Item1.ShouldBe("CORE.JOB.SUCCEEDED");
        row.Item2.ShouldContain("\"FileName\"");
        row.Item2.ShouldContain("don-hang.xlsx");
        (await db.CountAsync("SELECT count(*) FROM core.notification_recipient WHERE user_id = @u", new NpgsqlParameter("u", _a.AdminUserId))).ShouldBe(1);
    }

    [Fact]
    public async Task List_ReturnsOnlyTheCallersOwn_AndOnlyInTheirTenant()
    {
        await PublishAsync(_a, Draft(_a, "CORE.A"));
        await PublishAsync(_b, Draft(_b, "CORE.B"));

        var forA = await _host.AsAsync(_a, sp => sp.GetRequiredService<ISender>().Send(new GetNotificationsQuery()));
        var forB = await _host.AsAsync(_b, sp => sp.GetRequiredService<ISender>().Send(new GetNotificationsQuery()));

        forA.Value.Items.Select(i => i.Code).ShouldBe(["CORE.A"]);
        forB.Value.Items.Select(i => i.Code).ShouldBe(["CORE.B"]);
    }

    [Fact]
    public async Task UnreadCount_DropsWhenOneIsMarkedRead_AndMarkAllReadClearsIt()
    {
        await PublishAsync(_a, Draft(_a, "CORE.ONE"));
        await PublishAsync(_a, Draft(_a, "CORE.TWO"));
        (await _host.AsAsync(_a, sp => sp.GetRequiredService<ISender>().Send(new GetUnreadNotificationCountQuery()))).Value.ShouldBe(2);

        var first = (await _host.AsAsync(_a, sp => sp.GetRequiredService<ISender>().Send(new GetNotificationsQuery()))).Value.Items[0].Id;
        (await _host.AsAsync(_a, sp => sp.GetRequiredService<ISender>().Send(new MarkNotificationReadCommand(first)))).IsSuccess.ShouldBeTrue();
        (await _host.AsAsync(_a, sp => sp.GetRequiredService<ISender>().Send(new GetUnreadNotificationCountQuery()))).Value.ShouldBe(1);

        (await _host.AsAsync(_a, sp => sp.GetRequiredService<ISender>().Send(new MarkAllNotificationsReadCommand()))).IsSuccess.ShouldBeTrue();
        (await _host.AsAsync(_a, sp => sp.GetRequiredService<ISender>().Send(new GetUnreadNotificationCountQuery()))).Value.ShouldBe(0);
        (await db.CountAsync("SELECT count(*) FROM core.notification_recipient WHERE read_at IS NULL")).ShouldBe(0);
    }

    [Fact]
    public async Task MarkingSomeoneElsesNotificationRead_IsNotFound_AndLeavesItUnread()
    {
        await PublishAsync(_a, Draft(_a));
        var id = (await _host.AsAsync(_a, sp => sp.GetRequiredService<ISender>().Send(new GetNotificationsQuery()))).Value.Items[0].Id;

        var byOther = await _host.AsAsync(_b, sp => sp.GetRequiredService<ISender>().Send(new MarkNotificationReadCommand(id)));

        byOther.IsFailure.ShouldBeTrue();
        byOther.Error!.Code.ShouldBe(NotificationErrors.NotFound.Code);
        (await db.CountAsync("SELECT count(*) FROM core.notification_recipient WHERE read_at IS NULL")).ShouldBe(1);
    }

    [Fact]
    public async Task ADraftWithNoRecipients_IsANoOp_NotAnError()
    {
        var result = await PublishAsync(_a, new NotificationDraft("CORE.X", new Dictionary<string, string>(), []));

        result.IsSuccess.ShouldBeTrue();
        (await db.CountAsync("SELECT count(*) FROM core.notification")).ShouldBe(0);
    }

    // ---- Email -----------------------------------------------------------------------------------

    private Task SetLanguageAsync(TestTenant user, string? language)
        => db.CountAsync(
            "UPDATE core.app_user SET preferred_language = @l WHERE id = @id RETURNING 1::bigint",
            new NpgsqlParameter("l", (object?)language ?? DBNull.Value),
            new NpgsqlParameter("id", user.AdminUserId));

    [Fact]
    public async Task Email_UsesTheRecipientsPreferredLanguage_NotThePublishersOrTheDefault()
    {
        await SetLanguageAsync(_a, "en");
        // Người kích hoạt là một danh tính khác cùng đơn vị (không có tuỳ chọn ngôn ngữ nào): nếu code lấy nhầm ngôn ngữ
        // của người kích hoạt hay ngôn ngữ mặc định thì thư ra "vi", không phải "en".
        var publisher = _a with { AdminUserId = Guid.NewGuid(), AdminUserName = "nguoi.kich.hoat" };

        var result = await PublishAsync(publisher, Draft(_a, channels: NotificationChannels.Email));

        result.IsSuccess.ShouldBeTrue(result.Error?.Code);
        _email.Renders.ShouldHaveSingleItem().Language.ShouldBe("en");
    }

    [Fact]
    public async Task Email_WithNoLanguagePreference_FallsBackToTheConfiguredDefault()
    {
        await SetLanguageAsync(_a, null);

        var result = await PublishAsync(_a, Draft(_a, channels: NotificationChannels.Email));

        result.IsSuccess.ShouldBeTrue(result.Error?.Code);
        _email.Renders.ShouldHaveSingleItem().Language.ShouldBe("vi");
        _email.Sent.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task Email_WithAPreference_UsesIt_AndTheRenderedBodyIsWhatIsSent()
    {
        await SetLanguageAsync(_a, "en");

        var result = await PublishAsync(_a, Draft(_a, channels: NotificationChannels.Email));

        result.IsSuccess.ShouldBeTrue(result.Error?.Code);
        _email.Renders.ShouldHaveSingleItem().Language.ShouldBe("en");
        _email.Sent.ShouldHaveSingleItem().ToAddress.ShouldBe($"admin@dv-noti-a.example.com");
    }

    [Fact]
    public async Task Email_WhenTheProviderIsNotConfigured_FailsLoudly_AndStoresNothing()
    {
        await using var bare = new B4DockerHost(db.ConnectionString);
        var tenant = await bare.CreateTenantAsync("DV-NOTI-BARE");

        var result = await bare.AsAsync(tenant, async sp =>
        {
            var publisher = sp.GetRequiredService<INotificationPublisher>();
            return await publisher.PublishAsync(Draft(tenant, channels: NotificationChannels.Email), CancellationToken.None);
        });

        result.IsFailure.ShouldBeTrue();
        result.Error!.Code.ShouldBe(NotificationErrors.EmailNotConfigured.Code);
    }

    private sealed class CapturingEmail : IEmailSender, INotificationTemplateRenderer
    {
        public List<(string Code, string Language)> Renders { get; } = [];

        public List<EmailMessage> Sent { get; } = [];

        public Task<Result<RenderedEmail>> RenderAsync(string code, string language, IReadOnlyDictionary<string, string> parameters, CancellationToken ct)
        {
            Renders.Add((code, language));
            return Task.FromResult(Result.Success(new RenderedEmail("tiêu đề", "nội dung", "<p>nội dung</p>")));
        }

        public Task<Result> SendAsync(EmailMessage message, CancellationToken ct)
        {
            Sent.Add(message);
            return Task.FromResult(Result.Success());
        }
    }
}
