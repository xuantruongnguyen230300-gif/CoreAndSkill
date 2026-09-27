using CoreAndSkill.Core.Application.Common;
using CoreAndSkill.Core.Application.Common.Paging;
using CoreAndSkill.Core.Application.Notifications;
using CoreAndSkill.Core.Domain.Common;
using CoreAndSkill.Core.Domain.Notifications;
using CoreAndSkill.Core.UnitTests.Support;
using NSubstitute;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.UnitTests.Notifications;

// docs/contracts/notifications.md. Mọi use case chỉ chạm thông báo CỦA CHÍNH NGƯỜI GỌI.
public class NotificationHandlersTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 21, 3, 0, 0, TimeSpan.Zero);

    private readonly INotificationRepository _repository = Substitute.For<INotificationRepository>();
    private readonly Guid _me = Guid.NewGuid();
    private readonly FakeCurrentUser _currentUser;

    public NotificationHandlersTests() => _currentUser = new FakeCurrentUser(_me, "an.nv");

    [Fact]
    public async Task List_AsksForTheCallersOwnNotifications_WithTheGivenPaging()
    {
        var page = new PagedList<NotificationDto>
        {
            Items = [new NotificationDto(Guid.NewGuid(), "core.user.locked", new Dictionary<string, string> { ["UserName"] = "an.nv" }, Now, null)],
            Page = 2,
            PageSize = 10,
            TotalCount = 11,
        };
        _repository.ListForUserAsync(_me, true, 2, 10, Arg.Any<CancellationToken>()).Returns(page);

        var result = await new GetNotificationsQueryHandler(_repository, _currentUser)
            .Handle(new GetNotificationsQuery(2, 10, UnreadOnly: true), default);

        result.Value.ShouldBeSameAs(page);
    }

    [Fact]
    public async Task List_WithoutASession_IsAProgrammingError()
        => await Should.ThrowAsync<InvalidOperationException>(() =>
            new GetNotificationsQueryHandler(_repository, new FakeCurrentUser()).Handle(new GetNotificationsQuery(), default));

    [Fact]
    public async Task UnreadCount_CountsForTheCaller_WithoutLoadingAnyList()
    {
        _repository.CountUnreadAsync(_me, Arg.Any<CancellationToken>()).Returns(7);

        var result = await new GetUnreadNotificationCountQueryHandler(_repository, _currentUser)
            .Handle(new GetUnreadNotificationCountQuery(), default);

        result.Value.ShouldBe(7);
        await _repository.DidNotReceiveWithAnyArgs().ListForUserAsync(default, default, default, default, default);
    }

    [Fact]
    public async Task UnreadCount_WithoutASession_IsAProgrammingError()
        => await Should.ThrowAsync<InvalidOperationException>(() =>
            new GetUnreadNotificationCountQueryHandler(_repository, new FakeCurrentUser()).Handle(new GetUnreadNotificationCountQuery(), default));

    [Fact]
    public async Task MarkRead_SetsTheReadTimestamp_OnTheCallersRecipientRow()
    {
        var notificationId = Guid.NewGuid();
        var recipient = NotificationRecipient.Create(notificationId, _me).Value;
        _repository.FindRecipientAsync(notificationId, _me, Arg.Any<CancellationToken>()).Returns(recipient);

        var result = await new MarkNotificationReadCommandHandler(_repository, _currentUser, new FixedTimeProvider(Now))
            .Handle(new MarkNotificationReadCommand(notificationId), default);

        result.IsSuccess.ShouldBeTrue();
        recipient.ReadAt.ShouldBe(Now);
    }

    [Fact]
    public async Task MarkRead_SomeoneElsesNotification_IsNotFound_NotForbidden()
    {
        var notificationId = Guid.NewGuid();
        _repository.FindRecipientAsync(notificationId, _me, Arg.Any<CancellationToken>()).Returns((NotificationRecipient?)null);

        var result = await new MarkNotificationReadCommandHandler(_repository, _currentUser, new FixedTimeProvider(Now))
            .Handle(new MarkNotificationReadCommand(notificationId), default);

        result.Error!.Code.ShouldBe(NotificationErrors.NotFound.Code);
        result.Error.Type.ShouldBe(ErrorType.NotFound);
    }

    [Fact]
    public async Task MarkRead_WithoutASession_IsAProgrammingError()
        => await Should.ThrowAsync<InvalidOperationException>(() =>
            new MarkNotificationReadCommandHandler(_repository, new FakeCurrentUser(), new FixedTimeProvider(Now))
                .Handle(new MarkNotificationReadCommand(Guid.NewGuid()), default));

    [Fact]
    public async Task MarkAllRead_UpdatesOnlyTheCallersRows()
    {
        var result = await new MarkAllNotificationsReadCommandHandler(_repository, _currentUser, new FixedTimeProvider(Now))
            .Handle(new MarkAllNotificationsReadCommand(), default);

        result.IsSuccess.ShouldBeTrue();
        await _repository.Received(1).MarkAllReadAsync(_me, Now, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MarkAllRead_WithoutASession_IsAProgrammingError()
        => await Should.ThrowAsync<InvalidOperationException>(() =>
            new MarkAllNotificationsReadCommandHandler(_repository, new FakeCurrentUser(), new FixedTimeProvider(Now))
                .Handle(new MarkAllNotificationsReadCommand(), default));

    [Theory]
    [InlineData(0, 20, false)]
    [InlineData(1, 0, false)]
    [InlineData(1, 201, false)]
    [InlineData(1, 1, true)]
    [InlineData(1, 200, true)]
    public void ListValidator_FollowsTheSharedPagingRules(int page, int pageSize, bool valid)
    {
        var result = new GetNotificationsQueryValidator().Validate(new GetNotificationsQuery(page, pageSize));

        result.IsValid.ShouldBe(valid);
        result.Errors.ShouldAllBe(e => e.ErrorCode == CommonErrors.Format.Code);
    }

    [Fact]
    public void ErrorCatalog_UsesTheExpectedCodes()
    {
        NotificationErrors.NotFound.Code.ShouldBe("CORE.NOTIFICATION.NOT_FOUND");
        NotificationErrors.NotFound.Type.ShouldBe(ErrorType.NotFound);
    }
}
