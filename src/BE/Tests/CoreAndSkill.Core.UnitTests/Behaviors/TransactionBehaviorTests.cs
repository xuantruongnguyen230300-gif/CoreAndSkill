using CoreAndSkill.Core.Application.Common.Behaviors;
using CoreAndSkill.Core.Application.Common.Cqrs;
using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Domain.Common;
using NSubstitute;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.UnitTests.Behaviors;

// docs/quy-uoc/be-cqrs-handler.md §5.3.
public class TransactionBehaviorTests
{
    private sealed record FakeCommand : ICommandBase;

    private sealed record FakeNoTransactionCommand : ICommandBase, INoTransaction;

    [Fact]
    public async Task Handle_NoTransactionMarker_BypassesUnitOfWork()
    {
        var uow = Substitute.For<IUnitOfWork>();
        var behavior = new TransactionBehavior<FakeNoTransactionCommand, Result>(uow);
        var expected = Result.Success();

        var response = await behavior.Handle(new FakeNoTransactionCommand(), _ => Task.FromResult(expected), CancellationToken.None);

        response.ShouldBe(expected);
        await uow.DidNotReceiveWithAnyArgs().ExecuteInTransactionAsync<Result>(default!, default);
    }

    [Fact]
    public async Task Handle_NormalCommand_Success_CommitsThroughUnitOfWork()
    {
        var uow = Substitute.For<IUnitOfWork>();
        TransactionOutcome<Result>? captured = null;

        uow.ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task<TransactionOutcome<Result>>>>(), Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                var operation = callInfo.ArgAt<Func<CancellationToken, Task<TransactionOutcome<Result>>>>(0);
                var outcome = await operation(CancellationToken.None);
                captured = outcome;
                return outcome.Value;
            });

        var behavior = new TransactionBehavior<FakeCommand, Result>(uow);
        var expected = Result.Success();

        var response = await behavior.Handle(new FakeCommand(), _ => Task.FromResult(expected), CancellationToken.None);

        response.ShouldBe(expected);
        captured.ShouldNotBeNull();
        captured!.Value.ShouldCommit.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_NormalCommand_Failure_DoesNotCommit()
    {
        var uow = Substitute.For<IUnitOfWork>();
        TransactionOutcome<Result>? captured = null;

        uow.ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task<TransactionOutcome<Result>>>>(), Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                var operation = callInfo.ArgAt<Func<CancellationToken, Task<TransactionOutcome<Result>>>>(0);
                var outcome = await operation(CancellationToken.None);
                captured = outcome;
                return outcome.Value;
            });

        var behavior = new TransactionBehavior<FakeCommand, Result>(uow);
        var failure = Result.Failure(new Error("TEST.FAILED", "lỗi giả lập", ErrorType.BusinessRule));

        var response = await behavior.Handle(new FakeCommand(), _ => Task.FromResult(failure), CancellationToken.None);

        response.ShouldBe(failure);
        captured!.Value.ShouldCommit.ShouldBeFalse();
    }
}
