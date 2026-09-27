using System.Diagnostics;
using System.Text.Json;
using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Domain.Jobs;
using CoreAndSkill.Core.Domain.Outbox;
using CoreAndSkill.Core.Infrastructure.Persistence;
using CoreAndSkill.Core.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Outbox;

// docs/wiki-core/be/12-notifications.md §2.1-§2.2. Chứng minh phần LOGIC của bộ chặn: sự kiện entity ghi nhận được chuyển
// thành dòng outbox NGAY TRONG lượt SaveChanges đang chạy (cùng ChangeTracker => cùng giao dịch). Không cần database:
// thêm entity vào ChangeTracker không mở kết nối.
//
// Thứ test này KHÔNG chứng minh: dòng outbox thật sự cùng commit/rollback với dữ liệu — việc của
// OutboxDatabaseTests (RequiresDocker).
public sealed class OutboxInterceptorTests : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 9, 21, 3, 0, 0, TimeSpan.Zero);

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private readonly Guid _tenant = Guid.NewGuid();
    private readonly Guid _user = Guid.NewGuid();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly CoreDbContext _db;

    public OutboxInterceptorTests()
    {
        _currentUser.UserId.Returns(_user);
        _currentUser.UserName.Returns("an.nv");
        _tenantContext.TenantId.Returns(_tenant);

        _db = new CoreDbContext(
            new DbContextOptionsBuilder<CoreDbContext>()
                .UseNpgsql("Host=127.0.0.1;Port=1;Database=x;Username=x;Password=x;Timeout=1")
                .UseSnakeCaseNamingConvention()
                .Options,
            _tenantContext);
    }

    public void Dispose() => _db.Dispose();

    private OutboxInterceptor Interceptor() => new(new FixedClock(Now), _currentUser, _tenantContext);

    private static Job NewJob(string input = "_tmp/abc")
        => Job.Create("banhang.don-hang.import", Guid.NewGuid(), input).Value;

    [Fact]
    public void Stage_TurnsARecordedEventIntoOneOutboxRow_InTheSameChangeSet()
    {
        var job = NewJob();
        _db.Jobs.Add(job);

        Interceptor().Stage(_db);

        var outbox = _db.ChangeTracker.Entries<OutboxMessage>().Single();
        outbox.State.ShouldBe(EntityState.Added, "dòng outbox nằm trong CÙNG ChangeTracker nên cùng lượt SaveChanges, cùng giao dịch");
        outbox.Entity.EventType.ShouldBe(JobQueuedEvent.TypeKey);
        outbox.Entity.Status.ShouldBe(OutboxStatus.Pending);
        outbox.Entity.OccurredAt.ShouldBe(Now);
        outbox.Entity.NextAttemptAt.ShouldBe(Now);
        outbox.Entity.AttemptCount.ShouldBe(0);
    }

    [Fact]
    public void Stage_ThePayloadIsSelfContained_CamelCase_AndCarriesNoTypeName()
    {
        var job = NewJob("_tmp/abc");
        _db.Jobs.Add(job);

        Interceptor().Stage(_db);

        var payload = _db.ChangeTracker.Entries<OutboxMessage>().Single().Entity.Payload;
        using var document = JsonDocument.Parse(payload);
        document.RootElement.GetProperty("jobId").GetGuid().ShouldBe(job.Id);
        document.RootElement.GetProperty("jobType").GetString().ShouldBe("banhang.don-hang.import");
        document.RootElement.GetProperty("input").GetString().ShouldBe("_tmp/abc");
        document.RootElement.EnumerateObject().Select(p => p.Name).ShouldNotContain("eventType", "khoá hợp đồng nằm ở cột event_type, không lặp trong nội dung");
        payload.ShouldNotContain("Core.Domain", Case.Insensitive, "không rò tên kiểu CLR: đổi tên lớp không được làm hàng chờ đang có mồ côi");
    }

    [Fact]
    public void Stage_CapturesTheTriggeringUser_AndTheTraceId_ForTheDispatcherToReopenTheContext()
    {
        using var activity = new Activity("test-request").Start();
        _db.Jobs.Add(NewJob());

        Interceptor().Stage(_db);

        var row = _db.ChangeTracker.Entries<OutboxMessage>().Single().Entity;
        row.TriggeredByUserId.ShouldBe(_user);
        row.TriggeredByUserName.ShouldBe("an.nv");
        row.TraceId.ShouldBe(activity.TraceId.ToHexString());
    }

    [Fact]
    public void Stage_NoPersonInContext_LeavesTheTriggerEmpty_WhichTheDispatcherReadsAsSystem()
    {
        _currentUser.UserId.Returns((Guid?)null);
        _currentUser.UserName.Returns((string?)null);
        _db.Jobs.Add(NewJob());

        Interceptor().Stage(_db);

        var row = _db.ChangeTracker.Entries<OutboxMessage>().Single().Entity;
        row.TriggeredByUserId.ShouldBeNull();
        row.TriggeredByUserName.ShouldBeNull();
    }

    [Fact]
    public void Stage_TheTenantIsTheEmittingEntitys_WhenItAlreadyHasOne()
    {
        var entityTenant = Guid.NewGuid();
        var job = NewJob();
        _db.Jobs.Add(job);
        // TenantAssignmentInterceptor gán TenantId qua đường này — mô phỏng đúng nó (init không set trực tiếp được).
        _db.Entry(job).Property(nameof(Job.TenantId)).CurrentValue = entityTenant;

        Interceptor().Stage(_db);

        _db.ChangeTracker.Entries<OutboxMessage>().Single().Entity.TenantId.ShouldBe(entityTenant);
    }

    [Fact]
    public void Stage_AnEntityWithoutATenantYet_FallsBackToTheContextTenant()
    {
        _db.Jobs.Add(NewJob());

        Interceptor().Stage(_db);

        _db.ChangeTracker.Entries<OutboxMessage>().Single().Entity.TenantId.ShouldBe(_tenant);
    }

    [Fact]
    public void Stage_NoTenantAnywhere_RefusesToSave_NeverWritingAnEmptyGuid()
    {
        _tenantContext.TenantId.Returns((Guid?)null);
        _db.Jobs.Add(NewJob());

        var ex = Should.Throw<InvalidOperationException>(() => Interceptor().Stage(_db));

        ex.Message.ShouldContain("M8");
        _db.ChangeTracker.Entries<OutboxMessage>().ShouldBeEmpty();
    }

    [Fact]
    public void Stage_ClearsTheEvents_SoASecondSaveOfTheSameEntityDoesNotWriteThemAgain()
    {
        var job = NewJob();
        _db.Jobs.Add(job);
        var interceptor = Interceptor();

        interceptor.Stage(_db);
        interceptor.Stage(_db);

        job.DomainEvents.ShouldBeEmpty();
        _db.ChangeTracker.Entries<OutboxMessage>().Count().ShouldBe(1);
    }

    [Fact]
    public void Stage_EveryEventOnOneEntity_BecomesItsOwnRow_InOrder()
    {
        var job = NewJob();
        job.ClearDomainEvents();
        typeof(Job).GetProperty(nameof(Job.Status))!.SetValue(job, JobStatus.Running);
        job.Complete(Now, "{}", null);
        _db.Jobs.Add(job);

        Interceptor().Stage(_db);

        _db.ChangeTracker.Entries<OutboxMessage>().Select(e => e.Entity.EventType)
            .ShouldBe([JobFinishedEvent.TypeKey]);
    }

    [Fact]
    public void Stage_NothingRecorded_AddsNothing()
    {
        var job = NewJob();
        job.ClearDomainEvents();
        _db.Jobs.Add(job);

        Interceptor().Stage(_db);

        _db.ChangeTracker.Entries<OutboxMessage>().ShouldBeEmpty();
    }

    [Fact]
    public void Stage_ANullContext_IsIgnored()
        => Should.NotThrow(() => Interceptor().Stage(null));

    [Fact]
    public void Stage_TwoEntities_EachKeepTheirOwnTenant()
    {
        var a = NewJob();
        var b = NewJob();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        _db.Jobs.AddRange(a, b);
        _db.Entry(a).Property(nameof(Job.TenantId)).CurrentValue = tenantA;
        _db.Entry(b).Property(nameof(Job.TenantId)).CurrentValue = tenantB;

        Interceptor().Stage(_db);

        _db.ChangeTracker.Entries<OutboxMessage>().Select(e => e.Entity.TenantId).ShouldBe([tenantA, tenantB], ignoreOrder: true);
    }
}
