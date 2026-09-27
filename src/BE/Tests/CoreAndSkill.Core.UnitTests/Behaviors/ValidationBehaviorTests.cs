using CoreAndSkill.Core.Application.Common;
using CoreAndSkill.Core.Application.Common.Behaviors;
using CoreAndSkill.Core.Domain.Common;
using FluentValidation;
using FluentValidation.Results;
using NSubstitute;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.UnitTests.Behaviors;

// docs/quy-uoc/be-cqrs-handler.md §5.2.
public class ValidationBehaviorTests
{
    public sealed record FakeRequest(string Value);

    [Fact]
    public async Task Handle_NoValidators_CallsNext()
    {
        var behavior = new ValidationBehavior<FakeRequest, Result<string>>([]);
        var expected = Result.Success("ok");

        var response = await behavior.Handle(new FakeRequest("x"), _ => Task.FromResult(expected), CancellationToken.None);

        response.ShouldBe(expected);
    }

    [Fact]
    public async Task Handle_ValidatorPasses_CallsNext()
    {
        var validator = Substitute.For<IValidator<FakeRequest>>();
        validator.ValidateAsync(Arg.Any<ValidationContext<FakeRequest>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        var behavior = new ValidationBehavior<FakeRequest, Result<string>>([validator]);
        var expected = Result.Success("ok");

        var response = await behavior.Handle(new FakeRequest("x"), _ => Task.FromResult(expected), CancellationToken.None);

        response.ShouldBe(expected);
    }

    [Fact]
    public async Task Handle_ValidatorFails_ReturnsValidationFailed_DoesNotCallNext()
    {
        var failure = new ValidationFailure("Value", "bắt buộc") { ErrorCode = "CORE.VALIDATION.REQUIRED" };
        var validator = Substitute.For<IValidator<FakeRequest>>();
        validator.ValidateAsync(Arg.Any<ValidationContext<FakeRequest>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult([failure]));

        var behavior = new ValidationBehavior<FakeRequest, Result<string>>([validator]);
        var nextCalled = false;

        var response = await behavior.Handle(
            new FakeRequest(""),
            _ =>
            {
                nextCalled = true;
                return Task.FromResult(Result.Success("unused"));
            },
            CancellationToken.None);

        nextCalled.ShouldBeFalse();
        response.IsFailure.ShouldBeTrue();
        response.Error!.Code.ShouldBe(CommonErrors.ValidationFailed.Code);
        response.Error.FieldErrors.ShouldContainKey("Value");
        response.Error.FieldErrors["Value"][0].Code.ShouldBe("CORE.VALIDATION.REQUIRED");
    }

    [Fact]
    public async Task Handle_MultipleValidators_EachGetsOwnContext_ErrorsAccumulate()
    {
        var failure1 = new ValidationFailure("Value", "err1") { ErrorCode = "CODE1" };
        var failure2 = new ValidationFailure("Value", "err2") { ErrorCode = "CODE2" };

        var v1 = Substitute.For<IValidator<FakeRequest>>();
        v1.ValidateAsync(Arg.Any<ValidationContext<FakeRequest>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult([failure1]));

        var v2 = Substitute.For<IValidator<FakeRequest>>();
        v2.ValidateAsync(Arg.Any<ValidationContext<FakeRequest>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult([failure2]));

        var behavior = new ValidationBehavior<FakeRequest, Result<string>>([v1, v2]);

        var response = await behavior.Handle(new FakeRequest(""), _ => Task.FromResult(Result.Success("unused")), CancellationToken.None);

        response.Error!.FieldErrors["Value"].Count.ShouldBe(2);
    }

    [Fact]
    public async Task Handle_ValidatorFailure_FiltersMessageParams_DropsPropertyValue()
    {
        var failure = new ValidationFailure("Value", "quá dài")
        {
            ErrorCode = "CORE.VALIDATION.MAX_LENGTH",
            FormattedMessagePlaceholderValues = new Dictionary<string, object>
            {
                ["PropertyValue"] = "giá trị người dùng gõ",
                ["MaxLength"] = 10,
            },
        };
        var validator = Substitute.For<IValidator<FakeRequest>>();
        validator.ValidateAsync(Arg.Any<ValidationContext<FakeRequest>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult([failure]));

        var behavior = new ValidationBehavior<FakeRequest, Result<string>>([validator]);

        var response = await behavior.Handle(new FakeRequest("x"), _ => Task.FromResult(Result.Success("unused")), CancellationToken.None);

        var fieldError = response.Error!.FieldErrors["Value"][0];
        fieldError.Params.ShouldContainKey("MaxLength");
        fieldError.Params.ShouldNotContainKey("PropertyValue");
    }
}
