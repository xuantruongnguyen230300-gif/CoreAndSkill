using System.Text.Json;
using CoreAndSkill.Core.Application.Configuration;
using CoreAndSkill.Core.Application.Identity;
using CoreAndSkill.Core.Application.Notifications;
using CoreAndSkill.Core.Domain.Common;
using CoreAndSkill.Core.Domain.Notifications;
using CoreAndSkill.Core.UnitTests.ClientErrors;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.UnitTests.Notifications;

// docs/wiki-core/be/12-notifications.md §3, §4, §5.
public class NotificationPublisherTests
{
    private static readonly IReadOnlyDictionary<string, string> NoParams = new Dictionary<string, string>();

    private sealed class RecordingChannel(NotificationChannels channel, Result? outcome = null) : INotificationChannel
    {
        public NotificationChannels Channel { get; } = channel;

        public List<NotificationDelivery> Deliveries { get; } = [];

        public Task<Result> DeliverAsync(NotificationDelivery delivery, CancellationToken ct)
        {
            Deliveries.Add(delivery);
            return Task.FromResult(outcome ?? Result.Success());
        }
    }

    private sealed class FixedPreferences(Func<Guid, NotificationChannels, bool> enabled) : INotificationPreferences
    {
        public Task<IReadOnlyCollection<Guid>> FilterEnabledAsync(
            IReadOnlyCollection<Guid> userIds, string code, NotificationChannels channel, CancellationToken ct)
            => Task.FromResult<IReadOnlyCollection<Guid>>([.. userIds.Where(id => enabled(id, channel))]);
    }

    private sealed class CountingPreferences : INotificationPreferences
    {
        public int Calls { get; private set; }

        public Task<IReadOnlyCollection<Guid>> FilterEnabledAsync(
            IReadOnlyCollection<Guid> userIds, string code, NotificationChannels channel, CancellationToken ct)
        {
            Calls++;
            return Task.FromResult(userIds);
        }
    }

    private static NotificationPublisher Publisher(INotificationPreferences? preferences, params INotificationChannel[] channels)
        => new(channels, preferences ?? new AlwaysEnabledNotificationPreferences(), NullLogger<NotificationPublisher>.Instance);

    // ---- NotificationPublisher --------------------------------------------------------------

    [Fact]
    public async Task Publish_InApp_DeliversToEveryDistinctRecipient()
    {
        var inApp = new RecordingChannel(NotificationChannels.InApp);
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();

        var result = await Publisher(null, inApp)
            .PublishAsync(new NotificationDraft("core.user.locked", NoParams, [a, b, a, Guid.Empty]), default);

        result.IsSuccess.ShouldBeTrue();
        inApp.Deliveries.ShouldHaveSingleItem().RecipientUserIds.ShouldBe([a, b]);
    }

    [Fact]
    public async Task Publish_OnlyTheRequestedChannels_AreUsed()
    {
        var inApp = new RecordingChannel(NotificationChannels.InApp);
        var email = new RecordingChannel(NotificationChannels.Email);

        await Publisher(null, inApp, email).PublishAsync(new NotificationDraft("c", NoParams, [Guid.NewGuid()]), default);
        inApp.Deliveries.Count.ShouldBe(1);
        email.Deliveries.ShouldBeEmpty("Core không tự bật email — mặc định chỉ kênh trong ứng dụng");

        await Publisher(null, inApp, email).PublishAsync(
            new NotificationDraft("c", NoParams, [Guid.NewGuid()], Channels: NotificationChannels.InApp | NotificationChannels.Email), default);
        inApp.Deliveries.Count.ShouldBe(2);
        email.Deliveries.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Publish_NoRecipients_IsASuccessfulNoOp()
    {
        var inApp = new RecordingChannel(NotificationChannels.InApp);

        var result = await Publisher(null, inApp).PublishAsync(new NotificationDraft("c", NoParams, []), default);

        result.IsSuccess.ShouldBeTrue();
        inApp.Deliveries.ShouldBeEmpty();
    }

    [Fact]
    public async Task Publish_RecipientPreferences_FilterPerChannel_WithoutTouchingTheBusinessHandler()
    {
        var inApp = new RecordingChannel(NotificationChannels.InApp);
        var email = new RecordingChannel(NotificationChannels.Email);
        var wantsEverything = Guid.NewGuid();
        var noEmail = Guid.NewGuid();
        var muted = Guid.NewGuid();
        var preferences = new FixedPreferences((user, channel) =>
            user == wantsEverything || (user == noEmail && channel == NotificationChannels.InApp));

        await Publisher(preferences, inApp, email).PublishAsync(
            new NotificationDraft("c", NoParams, [wantsEverything, noEmail, muted], Channels: NotificationChannels.InApp | NotificationChannels.Email),
            default);

        inApp.Deliveries.Single().RecipientUserIds.ShouldBe([wantsEverything, noEmail]);
        email.Deliveries.Single().RecipientUserIds.ShouldBe([wantsEverything]);
    }

    [Fact]
    public async Task Publish_EveryoneOptedOut_SkipsTheChannelEntirely()
    {
        var inApp = new RecordingChannel(NotificationChannels.InApp);

        var result = await Publisher(new FixedPreferences((_, _) => false), inApp)
            .PublishAsync(new NotificationDraft("c", NoParams, [Guid.NewGuid()]), default);

        result.IsSuccess.ShouldBeTrue();
        inApp.Deliveries.ShouldBeEmpty();
    }

    [Fact]
    public async Task Publish_ARequestedChannelWithNoImplementation_FailsLoudly_NotSilently()
    {
        var result = await Publisher(null).PublishAsync(new NotificationDraft("c", NoParams, [Guid.NewGuid()]), default);

        result.Error!.Code.ShouldBe(NotificationErrors.ChannelUnavailable.Code);
        result.Error.Params["Channel"].ShouldBe("InApp");
    }

    [Fact]
    public async Task Publish_AChannelFailure_StopsAndIsReturned()
    {
        var inApp = new RecordingChannel(NotificationChannels.InApp, Result.Failure(NotificationErrors.TemplateMissing));
        var email = new RecordingChannel(NotificationChannels.Email);

        var result = await Publisher(null, inApp, email).PublishAsync(
            new NotificationDraft("c", NoParams, [Guid.NewGuid()], Channels: NotificationChannels.InApp | NotificationChannels.Email), default);

        result.Error!.Code.ShouldBe(NotificationErrors.TemplateMissing.Code);
        email.Deliveries.ShouldBeEmpty();
    }

    // Seam sở thích được hỏi MỘT lần cho mỗi kênh, không một lần cho mỗi người nhận. Bản mặc định của Core chạy trong bộ
    // nhớ nên số lời gọi không đo được ở đây bằng thời gian — chỉ đo được bằng ĐẾM. Dự án cài bản đọc database thì mỗi
    // lời gọi là một truy vấn, và một thông báo cho 200 người qua 2 kênh thành 400 truy vấn tuần tự. Hình dạng seam là
    // thứ dự án hạ nguồn KHÔNG sửa được, nên nó phải đúng ở Core.
    [Fact]
    public async Task Publish_AsksThePreferencesSeam_OncePerChannel_NotOncePerRecipient()
    {
        var inApp = new RecordingChannel(NotificationChannels.InApp);
        var email = new RecordingChannel(NotificationChannels.Email);
        var counting = new CountingPreferences();

        await Publisher(counting, inApp, email).PublishAsync(
            new NotificationDraft("c", NoParams, [Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()],
                Channels: NotificationChannels.InApp | NotificationChannels.Email),
            default);

        counting.Calls.ShouldBe(2);
    }

    [Fact]
    public async Task DefaultPreferences_EnableEverything()
    {
        var preferences = new AlwaysEnabledNotificationPreferences();
        var everyone = new[] { Guid.NewGuid(), Guid.NewGuid() };

        (await preferences.FilterEnabledAsync(everyone, "bat-ky", NotificationChannels.Email, default)).ShouldBe(everyone);
    }

    // ---- InAppNotificationChannel -----------------------------------------------------------

    [Fact]
    public async Task InApp_StoresTheKeyAndNamedParams_AsJson_NeverASentence()
    {
        var repository = Substitute.For<INotificationRepository>();
        var recipients = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var draft = new NotificationDraft(
            "core.user.locked", new Dictionary<string, string> { ["UserName"] = "an.nv" }, recipients, ModuleKey: "core");

        var result = await new InAppNotificationChannel(repository).DeliverAsync(new NotificationDelivery(draft, recipients), default);

        result.IsSuccess.ShouldBeTrue();
        var call = repository.ReceivedCalls().Single();
        var notification = (Notification)call.GetArguments()[0]!;
        var stored = (IReadOnlyCollection<NotificationRecipient>)call.GetArguments()[1]!;

        notification.Code.ShouldBe("core.user.locked");
        notification.ModuleKey.ShouldBe("core");
        using var document = JsonDocument.Parse(notification.Params!);
        document.RootElement.GetProperty("UserName").GetString().ShouldBe("an.nv");
        stored.Select(r => r.UserId).ShouldBe(recipients);
        stored.ShouldAllBe(r => r.NotificationId == notification.Id && r.ReadAt == null);
    }

    [Fact]
    public async Task InApp_NoParams_StoresNullRatherThanAnEmptyObject()
    {
        var repository = Substitute.For<INotificationRepository>();
        var draft = new NotificationDraft("c", NoParams, [Guid.NewGuid()]);

        await new InAppNotificationChannel(repository).DeliverAsync(new NotificationDelivery(draft, draft.RecipientUserIds), default);

        ((Notification)repository.ReceivedCalls().Single().GetArguments()[0]!).Params.ShouldBeNull();
    }

    // ---- EmailNotificationChannel: ngôn ngữ của NGƯỜI NHẬN (mục 8 nghiệm thu B4) ---------------------

    private sealed class Email
    {
        public IUserLookupService Users { get; } = Substitute.For<IUserLookupService>();

        public INotificationTemplateRenderer Renderer { get; } = Substitute.For<INotificationTemplateRenderer>();

        public IEmailSender Sender { get; } = Substitute.For<IEmailSender>();

        public RecordingLogger<EmailNotificationChannel> Logger { get; } = new();

        public EmailNotificationChannel Channel(bool withSender = true, string defaultLanguage = "vi")
            => new(
                Users,
                Renderer,
                withSender ? [Sender] : [],
                Options.Create(new CoreNotificationOptions { DefaultLanguage = defaultLanguage }),
                Logger);

        public Email()
        {
            Renderer.RenderAsync(default!, default!, default!, default).ReturnsForAnyArgs(
                Result.Success(new RenderedEmail("Chủ đề", "văn bản", "<p>html</p>")));
            Sender.SendAsync(default!, default).ReturnsForAnyArgs(Result.Success());
        }

        public void HasUser(Guid id, string? email, string? language, string fullName = "Người nhận")
        {
            var summary = new UserSummaryDto(id, "user", fullName, email, false, false, false, language);
            Users.FindByIdsAsync(default!, default).ReturnsForAnyArgs([summary]);
        }
    }

    [Fact]
    public async Task Email_UsesTheRecipientsPreferredLanguage_NotTheInitiatorsAndNotTheRequests()
    {
        var recipientId = Guid.NewGuid();
        var email = new Email();
        email.HasUser(recipientId, "binh@vd.vn", "en");
        var draft = new NotificationDraft(
            "core.job.succeeded", new Dictionary<string, string> { ["JobId"] = "j1" }, [recipientId], Channels: NotificationChannels.Email);

        var result = await email.Channel(defaultLanguage: "vi").DeliverAsync(new NotificationDelivery(draft, [recipientId]), default);

        result.IsSuccess.ShouldBeTrue();
        await email.Renderer.Received(1).RenderAsync("core.job.succeeded", "en", draft.Params, Arg.Any<CancellationToken>());
        await email.Renderer.DidNotReceive().RenderAsync(Arg.Any<string>(), "vi", Arg.Any<IReadOnlyDictionary<string, string>>(), Arg.Any<CancellationToken>());
        var sent = (EmailMessage)email.Sender.ReceivedCalls().Single().GetArguments()[0]!;
        sent.ToAddress.ShouldBe("binh@vd.vn");
        sent.ToDisplayName.ShouldBe("Người nhận");
    }

    [Fact]
    public async Task Email_ARecipientWithoutALanguage_GetsTheSystemDefault()
    {
        var recipientId = Guid.NewGuid();
        var email = new Email();
        email.HasUser(recipientId, "binh@vd.vn", language: null);

        await email.Channel(defaultLanguage: "vi").DeliverAsync(
            new NotificationDelivery(new NotificationDraft("c", NoParams, [recipientId]), [recipientId]), default);

        await email.Renderer.Received(1).RenderAsync("c", "vi", Arg.Any<IReadOnlyDictionary<string, string>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Email_TwoRecipientsWithDifferentLanguages_EachGetTheirOwn()
    {
        var vi = Guid.NewGuid();
        var en = Guid.NewGuid();
        var email = new Email();
        email.Users.FindByIdsAsync(default!, default).ReturnsForAnyArgs([
            new UserSummaryDto(vi, "a", "A", "a@vd.vn", false, false, false, "vi"),
            new UserSummaryDto(en, "b", "B", "b@vd.vn", false, false, false, "en-US"),
        ]);

        await email.Channel().DeliverAsync(
            new NotificationDelivery(new NotificationDraft("c", NoParams, [vi, en]), [vi, en]), default);

        await email.Renderer.Received(1).RenderAsync("c", "vi", Arg.Any<IReadOnlyDictionary<string, string>>(), Arg.Any<CancellationToken>());
        await email.Renderer.Received(1).RenderAsync("c", "en-US", Arg.Any<IReadOnlyDictionary<string, string>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Email_NoSenderRegistered_FailsLoudly_ItNeverSilentlySkips()
    {
        var email = new Email();
        email.HasUser(Guid.NewGuid(), "a@vd.vn", "vi");

        var result = await email.Channel(withSender: false).DeliverAsync(
            new NotificationDelivery(new NotificationDraft("c", NoParams, [Guid.NewGuid()]), [Guid.NewGuid()]), default);

        result.Error!.Code.ShouldBe(NotificationErrors.EmailNotConfigured.Code);
    }

    [Fact]
    public async Task Email_ARecipientWithNoAddress_IsSkipped_WithoutLoggingTheAddress_AndTheRestStillGetMail()
    {
        var withoutAddress = Guid.NewGuid();
        var withAddress = Guid.NewGuid();
        var email = new Email();
        email.Users.FindByIdsAsync(default!, default).ReturnsForAnyArgs([
            new UserSummaryDto(withoutAddress, "a", "A", "  ", false, false, false, "vi"),
            new UserSummaryDto(withAddress, "b", "B", "b@vd.vn", false, false, false, "vi"),
        ]);

        var result = await email.Channel().DeliverAsync(
            new NotificationDelivery(new NotificationDraft("c", NoParams, [withoutAddress, withAddress]), [withoutAddress, withAddress]), default);

        result.IsSuccess.ShouldBeTrue();
        await email.Sender.Received(1).SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
        email.Logger.Entries.ShouldContain(e => e.Level == LogLevel.Information);
        email.Logger.Entries.ShouldAllBe(e => !e.Message.Contains("b@vd.vn"));
    }

    [Fact]
    public async Task Email_AMissingTemplate_FailsInsteadOfSendingAnEmailWithBlanks()
    {
        var email = new Email();
        email.HasUser(Guid.NewGuid(), "a@vd.vn", "vi");
        email.Renderer.RenderAsync(default!, default!, default!, default).ReturnsForAnyArgs(
            Result.Failure<RenderedEmail>(NotificationErrors.TemplateMissing));

        var result = await email.Channel().DeliverAsync(
            new NotificationDelivery(new NotificationDraft("c", NoParams, [Guid.NewGuid()]), [Guid.NewGuid()]), default);

        result.Error!.Code.ShouldBe(NotificationErrors.TemplateMissing.Code);
        await email.Sender.DidNotReceiveWithAnyArgs().SendAsync(default!, default);
    }

    [Fact]
    public async Task Email_ASenderFailure_IsReturned()
    {
        var email = new Email();
        email.HasUser(Guid.NewGuid(), "a@vd.vn", "vi");
        email.Sender.SendAsync(default!, default).ReturnsForAnyArgs(Result.Failure(new Error("X.Y.Z", "từ chối vĩnh viễn", ErrorType.BusinessRule)));

        var result = await email.Channel().DeliverAsync(
            new NotificationDelivery(new NotificationDraft("c", NoParams, [Guid.NewGuid()]), [Guid.NewGuid()]), default);

        result.Error!.Code.ShouldBe("X.Y.Z");
    }

    [Fact]
    public async Task DefaultTemplateRenderer_AlwaysReportsAMissingTemplate_BecauseCoreShipsNoTemplates()
    {
        var result = await new MissingTemplateRenderer().RenderAsync("core.job.succeeded", "vi", NoParams, default);

        result.Error!.Code.ShouldBe(NotificationErrors.TemplateMissing.Code);
        result.Error.Params["Code"].ShouldBe("core.job.succeeded");
        result.Error.Params["Language"].ShouldBe("vi");
    }
}
