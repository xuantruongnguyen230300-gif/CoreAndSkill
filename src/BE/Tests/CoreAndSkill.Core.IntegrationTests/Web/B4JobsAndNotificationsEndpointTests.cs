using System.Net;
using System.Net.Http.Json;
using CoreAndSkill.Core.Domain.Jobs;
using CoreAndSkill.Core.IntegrationTests.Support;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Web;

// Pha B4, tầng HTTP — docs/contracts/jobs.md, docs/contracts/notifications.md. Cùng giới hạn chứng minh với
// B4FilesEndpointTests: pipeline thật, repository trong bộ nhớ — không chứng minh SQL và bộ lọc đơn vị.
public sealed class B4JobsAndNotificationsEndpointTests(InMemoryHostFixture fixture) : B4EndpointTestBase(fixture), IClassFixture<InMemoryHostFixture>
{
    // ---- Việc nền ---------------------------------------------------------------------------

    private Job SeedJob(Guid createdBy)
    {
        var job = Job.Create("core.test.import", createdBy, "_tmp/x").Value;
        _host.Jobs.Items[job.Id] = job;
        return job;
    }

    [Fact]
    public async Task GetJob_Unauthenticated_Returns401()
    {
        using var client = _host.CreateClient(userId: null);

        var response = await client.GetAsync($"/api/v1/core/jobs/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetJob_ByTheInitiator_ReturnsTheContractFields()
    {
        var user = Guid.NewGuid();
        var job = SeedJob(user);
        using var client = _host.CreateClient(user);

        var response = await client.GetAsync($"/api/v1/core/jobs/{job.Id}");
        var envelope = await ReadEnvelope(response);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        envelope.Data.GetProperty("id").GetGuid().ShouldBe(job.Id);
        envelope.Data.GetProperty("kind").GetString().ShouldBe("core.test.import");
        envelope.Data.GetProperty("status").GetString().ShouldBe("queued");
        envelope.Data.GetProperty("progress").GetInt32().ShouldBe(0);
        envelope.Data.TryGetProperty("input", out _).ShouldBeFalse("khoá tệp tạm nằm trong input — không bao giờ trả ra ngoài");
    }

    [Fact]
    public async Task GetJob_ByAnotherUser_Returns404_SameAsANonexistentJob()
    {
        var job = SeedJob(Guid.NewGuid());
        using var other = _host.CreateClient(Guid.NewGuid());

        var denied = await other.GetAsync($"/api/v1/core/jobs/{job.Id}");
        var missing = await other.GetAsync($"/api/v1/core/jobs/{Guid.NewGuid()}");

        denied.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        missing.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await ReadEnvelope(denied)).Error!.Code.ShouldBe((await ReadEnvelope(missing)).Error!.Code);
    }

    [Fact]
    public async Task GetJob_ResultFile_OfAJob_IsDownloadableOnlyByTheInitiator_ThroughTheRealJobOwnerChecker()
    {
        var initiator = Guid.NewGuid();
        var job = SeedJob(initiator);
        using var initiatorClient = _host.CreateClient(initiator, "nguoi.nhap");
        var fileId = await UploadOk(initiatorClient);
        _host.Files.Items[fileId].AttachTo("core.job", job.Id).IsSuccess.ShouldBeTrue();
        using var other = _host.CreateClient(Guid.NewGuid(), "nguoi.la");

        (await initiatorClient.GetAsync($"/api/v1/core/files/{fileId}")).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await other.GetAsync($"/api/v1/core/files/{fileId}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ---- Thông báo --------------------------------------------------------------------------

    private static readonly Dictionary<string, string> Params = new() { ["FileName"] = "don-hang.xlsx" };

    [Fact]
    public async Task Notifications_Unauthenticated_AllFourEndpoints_Return401()
    {
        using var client = _host.CreateClient(userId: null);

        (await client.GetAsync("/api/v1/core/notifications")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await client.GetAsync("/api/v1/core/notifications/unread-count")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await client.PutAsync($"/api/v1/core/notifications/{Guid.NewGuid()}/read", null)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await client.PutAsync("/api/v1/core/notifications/read-all", null)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Notifications_List_ReturnsOnlyTheCallersOwn_AsCodeAndParams_NeverAComposedSentence()
    {
        var me = Guid.NewGuid();
        var someoneElse = Guid.NewGuid();
        _host.Notifications.Seed(me, "CORE.JOB.SUCCEEDED", Params);
        _host.Notifications.Seed(someoneElse, "CORE.JOB.FAILED", Params);
        using var client = _host.CreateClient(me);

        var response = await client.GetAsync("/api/v1/core/notifications");
        var envelope = await ReadEnvelope(response);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var items = envelope.Data.GetProperty("items").EnumerateArray().ToList();
        items.Count.ShouldBe(1);
        items[0].GetProperty("code").GetString().ShouldBe("CORE.JOB.SUCCEEDED");
        items[0].GetProperty("params").GetProperty("FileName").GetString().ShouldBe("don-hang.xlsx");
        items[0].EnumerateObject().Select(p => p.Name).ShouldNotContain("message");
        items[0].EnumerateObject().Select(p => p.Name).ShouldNotContain("title");
        envelope.Data.GetProperty("totalCount").GetInt32().ShouldBe(1);
    }

    [Fact]
    public async Task Notifications_UnreadCount_CountsOnlyTheCallersUnread()
    {
        var me = Guid.NewGuid();
        _host.Notifications.Seed(me, "A", Params);
        _host.Notifications.Seed(me, "B", Params);
        _host.Notifications.Seed(me, "C", Params, read: true);
        _host.Notifications.Seed(Guid.NewGuid(), "D", Params);
        using var client = _host.CreateClient(me);

        var envelope = await ReadEnvelope(await client.GetAsync("/api/v1/core/notifications/unread-count"));

        envelope.Data.GetInt32().ShouldBe(2);
    }

    [Fact]
    public async Task Notifications_MarkRead_MarksOwnOnly_AndTheCountDrops()
    {
        var me = Guid.NewGuid();
        _host.Notifications.Seed(me, "A", Params);
        var id = _host.Notifications.LastNotificationIdFor(me);
        using var client = _host.CreateClient(me);

        var response = await client.PutAsync($"/api/v1/core/notifications/{id}/read", null);
        var count = await ReadEnvelope(await client.GetAsync("/api/v1/core/notifications/unread-count"));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        count.Data.GetInt32().ShouldBe(0);
    }

    [Fact]
    public async Task Notifications_MarkRead_OfSomeoneElsesNotification_Returns404_AndLeavesItUnread()
    {
        var owner = Guid.NewGuid();
        _host.Notifications.Seed(owner, "A", Params);
        var id = _host.Notifications.LastNotificationIdFor(owner);
        using var intruder = _host.CreateClient(Guid.NewGuid());

        var response = await intruder.PutAsync($"/api/v1/core/notifications/{id}/read", null);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await _host.Notifications.CountUnreadAsync(owner, default)).ShouldBe(1);
    }

    [Fact]
    public async Task Notifications_MarkAllRead_TouchesOnlyTheCallersRows()
    {
        var me = Guid.NewGuid();
        var other = Guid.NewGuid();
        _host.Notifications.Seed(me, "A", Params);
        _host.Notifications.Seed(me, "B", Params);
        _host.Notifications.Seed(other, "C", Params);
        using var client = _host.CreateClient(me);

        var response = await client.PutAsync("/api/v1/core/notifications/read-all", null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await _host.Notifications.CountUnreadAsync(me, default)).ShouldBe(0);
        (await _host.Notifications.CountUnreadAsync(other, default)).ShouldBe(1);
    }

    [Fact]
    public async Task Notifications_WriteEndpoints_WithoutAntiforgeryToken_Return403()
    {
        using var client = _host.CreateClient(Guid.NewGuid(), withCsrf: false);

        (await client.PutAsync("/api/v1/core/notifications/read-all", null)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await client.PutAsync($"/api/v1/core/notifications/{Guid.NewGuid()}/read", null)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Notifications_ThereIsNoEndpointToCreateOrDelete()
    {
        using var client = _host.CreateClient(Guid.NewGuid());

        (await client.PostAsJsonAsync("/api/v1/core/notifications", new { code = "X" })).StatusCode.ShouldBe(HttpStatusCode.MethodNotAllowed);
        (await client.DeleteAsync($"/api/v1/core/notifications/{Guid.NewGuid()}")).StatusCode.ShouldBeOneOf(HttpStatusCode.NotFound, HttpStatusCode.MethodNotAllowed);
    }

}
