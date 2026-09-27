using CoreAndSkill.Core.Domain.Common;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.UnitTests.Common;

public class ResultTests
{
    private static readonly Error SampleError = new("TEST.SAMPLE.ERROR", "Lỗi mẫu.", ErrorType.BusinessRule);

    [Fact]
    public void Success_ReturnsIsSuccessTrue_AndNoError()
    {
        var result = Result.Success();

        result.IsSuccess.ShouldBeTrue();
        result.IsFailure.ShouldBeFalse();
        result.Error.ShouldBeNull();
    }

    [Fact]
    public void Failure_ReturnsIsSuccessFalse_AndCarriesError()
    {
        var result = Result.Failure(SampleError);

        result.IsSuccess.ShouldBeFalse();
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(SampleError);
    }

    [Fact]
    public void GenericSuccess_Value_ReturnsTheValue()
    {
        Result<int> result = Result.Success(42);

        result.Value.ShouldBe(42);
    }

    [Fact]
    public void GenericFailure_ReadingValue_Throws()
    {
        var result = Result.Failure<int>(SampleError);

        Should.Throw<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void ImplicitOperator_WrapsValue_AsSuccess()
    {
        Result<string> result = "an.nv";

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("an.nv");
    }

    [Fact]
    public void FromError_ViaResultFactory_ReturnsFailureOfCorrectType()
    {
        var result = CreateFromFactory<Result<int>>(SampleError);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(SampleError);
    }

    // Chứng minh IResultFactory<TSelf> (static abstract) dựng đúng kiểu trả về — cơ chế mà
    // ValidationBehavior/TransactionBehavior dùng để tránh reflection (be-cqrs-handler.md §5.4).
    private static T CreateFromFactory<T>(Error error) where T : IResultFactory<T>
        => T.FromError(error);
}
