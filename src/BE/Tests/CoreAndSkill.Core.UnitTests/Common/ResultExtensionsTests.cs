using CoreAndSkill.Core.Domain.Common;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.UnitTests.Common;

public class ResultExtensionsTests
{
    private static readonly Error SampleError = new("TEST.SAMPLE.ERROR", "Lỗi mẫu.", ErrorType.BusinessRule);

    [Fact]
    public void Map_OnSuccess_TransformsValue()
    {
        Result<int> result = 2;

        var mapped = result.Map(v => v * 10);

        mapped.IsSuccess.ShouldBeTrue();
        mapped.Value.ShouldBe(20);
    }

    [Fact]
    public void Map_OnFailure_PropagatesError_WithoutCallingSelector()
    {
        var result = Result.Failure<int>(SampleError);
        var called = false;

        var mapped = result.Map(v => { called = true; return v * 10; });

        mapped.IsFailure.ShouldBeTrue();
        mapped.Error.ShouldBe(SampleError);
        called.ShouldBeFalse();
    }

    [Fact]
    public void Bind_OnSuccess_ChainsNextStep()
    {
        Result<int> result = 2;

        var bound = result.Bind(v => Result.Success(v.ToString()));

        bound.IsSuccess.ShouldBeTrue();
        bound.Value.ShouldBe("2");
    }

    [Fact]
    public void Bind_OnFailure_ShortCircuits_WithoutCallingBinder()
    {
        var result = Result.Failure<int>(SampleError);
        var called = false;

        var bound = result.Bind(v => { called = true; return Result.Success(v.ToString()); });

        bound.IsFailure.ShouldBeTrue();
        bound.Error.ShouldBe(SampleError);
        called.ShouldBeFalse();
    }

    [Fact]
    public void Ensure_WhenPredicateFails_ReturnsGivenError()
    {
        Result<int> result = 5;

        var ensured = result.Ensure(v => v > 10, SampleError);

        ensured.IsFailure.ShouldBeTrue();
        ensured.Error.ShouldBe(SampleError);
    }

    [Fact]
    public void Ensure_WhenPredicatePasses_KeepsOriginalValue()
    {
        Result<int> result = 20;

        var ensured = result.Ensure(v => v > 10, SampleError);

        ensured.IsSuccess.ShouldBeTrue();
        ensured.Value.ShouldBe(20);
    }

    [Fact]
    public void Ensure_OnAlreadyFailedResult_DoesNotEvaluatePredicate()
    {
        var result = Result.Failure<int>(SampleError);
        var predicateCalled = false;

        var ensured = result.Ensure(v => { predicateCalled = true; return v > 10; }, SampleError);

        ensured.IsFailure.ShouldBeTrue();
        predicateCalled.ShouldBeFalse();
    }

    [Fact]
    public void Tap_OnSuccess_RunsSideEffect_AndReturnsSameResult()
    {
        Result<int> result = 7;
        var observed = 0;

        var tapped = result.Tap(v => observed = v);

        tapped.ShouldBe(result);
        observed.ShouldBe(7);
    }

    [Fact]
    public void Tap_OnFailure_DoesNotRunSideEffect()
    {
        var result = Result.Failure<int>(SampleError);
        var called = false;

        result.Tap(_ => called = true);

        called.ShouldBeFalse();
    }
}
