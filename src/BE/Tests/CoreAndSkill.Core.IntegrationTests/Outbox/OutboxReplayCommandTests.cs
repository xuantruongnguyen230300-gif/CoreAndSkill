using CoreAndSkill.Core.Application.Outbox;
using CoreAndSkill.Core.Domain.Common;
using CoreAndSkill.Core.Domain.Outbox;
using CoreAndSkill.Core.Web.Commands;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Outbox;

// `core outbox-replay` — docs/database/script-runbook.md §10. Phần kiểm ĐẦU VÀO của lệnh: sai cú pháp thì không ghi
// dòng nào và thoát mã 1. Phần đặt `dead` về `pending` và ghi kiểm toán là OutboxReplayService — chỉ được chứng minh ở
// mức logic domain (OutboxMessageTests) và ở mức SQL bởi OutboxDatabaseTests (RequiresDocker, chưa chạy).
//
// Environment.ExitCode và Console.Error là trạng thái TOÀN CỤC của tiến trình — lớp này khoá chúng trong một
// collection riêng và luôn trả về nguyên trạng, nếu không tiến trình test thoát với mã 1.
[Collection("Console")]
public sealed class OutboxReplayCommandTests : IDisposable
{
    private readonly IOutboxReplayService _replay = Substitute.For<IOutboxReplayService>();
    private readonly IServiceProvider _services;
    private readonly TextWriter _originalError = Console.Error;
    private readonly TextWriter _originalOut = Console.Out;
    private readonly StringWriter _error = new();
    private readonly StringWriter _out = new();

    public OutboxReplayCommandTests()
    {
        _services = new ServiceCollection().AddSingleton(_replay).BuildServiceProvider();
        Console.SetError(_error);
        Console.SetOut(_out);
        Environment.ExitCode = 0;
    }

    public void Dispose()
    {
        Console.SetError(_originalError);
        Console.SetOut(_originalOut);
        Environment.ExitCode = 0;
    }

    [Fact]
    public async Task NoOption_IsRejected_WritesNothing_ExitsOne()
    {
        await CoreCommandRunner.RunOutboxReplayAsync(_services, []);

        Environment.ExitCode.ShouldBe(1);
        _error.ToString().ShouldContain("MỘT");
        await _replay.DidNotReceiveWithAnyArgs().ReplayAsync(default, default);
        await _replay.DidNotReceiveWithAnyArgs().ReplayAllDeadAsync(default);
    }

    [Fact]
    public async Task BothOptions_AreRejected_BecauseReplayingEverythingAndOneRowAtOnceIsAmbiguous()
    {
        await CoreCommandRunner.RunOutboxReplayAsync(_services, ["--all-dead", "--id", Guid.NewGuid().ToString()]);

        Environment.ExitCode.ShouldBe(1);
        await _replay.DidNotReceiveWithAnyArgs().ReplayAsync(default, default);
        await _replay.DidNotReceiveWithAnyArgs().ReplayAllDeadAsync(default);
    }

    [Theory]
    [InlineData("--id")]
    [InlineData("--id", "khong-phai-guid")]
    [InlineData("--id", "")]
    public async Task AnIdOptionWithoutAValidGuid_IsRejected_WritesNothing(params string[] args)
    {
        await CoreCommandRunner.RunOutboxReplayAsync(_services, args);

        Environment.ExitCode.ShouldBe(1);
        _error.ToString().ShouldContain("GUID");
        await _replay.DidNotReceiveWithAnyArgs().ReplayAsync(default, default);
    }

    [Fact]
    public async Task ValidId_ReplaysExactlyThatRow_AndSucceeds()
    {
        var id = Guid.NewGuid();
        _replay.ReplayAsync(id, Arg.Any<CancellationToken>()).Returns(Result.Success());

        await CoreCommandRunner.RunOutboxReplayAsync(_services, ["--id", id.ToString()]);

        Environment.ExitCode.ShouldBe(0);
        await _replay.Received(1).ReplayAsync(id, Arg.Any<CancellationToken>());
        await _replay.DidNotReceiveWithAnyArgs().ReplayAllDeadAsync(default);
        _out.ToString().ShouldContain(id.ToString());
    }

    [Fact]
    public async Task ReplayOfARowThatIsNotDead_IsReportedAsAFailure_NotSwallowed()
    {
        var id = Guid.NewGuid();
        _replay.ReplayAsync(id, Arg.Any<CancellationToken>()).Returns(Result.Failure(OutboxErrors.NotDead));

        await CoreCommandRunner.RunOutboxReplayAsync(_services, ["--id", id.ToString()]);

        Environment.ExitCode.ShouldBe(1);
        _error.ToString().ShouldContain(OutboxErrors.NotDead.Code);
    }

    [Fact]
    public async Task AllDead_ReplaysEveryDeadRow_AndReportsTheCount()
    {
        _replay.ReplayAllDeadAsync(Arg.Any<CancellationToken>()).Returns(Result.Success(3));

        await CoreCommandRunner.RunOutboxReplayAsync(_services, ["--all-dead"]);

        Environment.ExitCode.ShouldBe(0);
        _out.ToString().ShouldContain("3");
    }

    [Fact]
    public async Task AllDead_WhenTheServiceFails_ExitsOne_WithTheErrorCode()
    {
        _replay.ReplayAllDeadAsync(Arg.Any<CancellationToken>()).Returns(Result.Failure<int>(OutboxErrors.NotFound));

        await CoreCommandRunner.RunOutboxReplayAsync(_services, ["--all-dead"]);

        Environment.ExitCode.ShouldBe(1);
        _error.ToString().ShouldContain(OutboxErrors.NotFound.Code);
    }
}
