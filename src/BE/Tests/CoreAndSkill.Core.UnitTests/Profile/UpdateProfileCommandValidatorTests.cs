using CoreAndSkill.Core.Application.Common;
using CoreAndSkill.Core.Application.Profile;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.UnitTests.Profile;

public class UpdateProfileCommandValidatorTests
{
    private readonly UpdateProfileCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_Succeeds()
    {
        var result = _validator.Validate(new UpdateProfileCommand("Nguyễn Văn An", "0912345678", "vi", "token"));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_ValidCommand_NullOptionalFields_Succeeds()
    {
        var result = _validator.Validate(new UpdateProfileCommand("Nguyễn Văn An", null, null, "token"));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_EmptyFullName_FailsWithRequired()
    {
        var result = _validator.Validate(new UpdateProfileCommand("", null, null, "token"));

        result.Errors.ShouldContain(e => e.PropertyName == "FullName" && e.ErrorCode == CommonErrors.Required.Code);
    }

    [Fact]
    public void Validate_FullNameTooLong_FailsWithMaxLength()
    {
        var result = _validator.Validate(new UpdateProfileCommand(new string('a', 201), null, null, "token"));

        result.Errors.ShouldContain(e => e.PropertyName == "FullName" && e.ErrorCode == CommonErrors.MaxLength.Code);
    }

    [Theory]
    [InlineData("0912345678")]
    [InlineData("091-234-5678")]
    [InlineData("+84 91 234 5678")]
    [InlineData("(024) 3822 5588")]
    public void Validate_PhoneNumberValidFormats_Succeed(string phoneNumber)
    {
        var result = _validator.Validate(new UpdateProfileCommand("An", phoneNumber, null, "token"));

        result.Errors.ShouldNotContain(e => e.PropertyName == "PhoneNumber");
    }

    [Fact]
    public void Validate_PhoneNumberInvalidCharacters_FailsWithFormat()
    {
        var result = _validator.Validate(new UpdateProfileCommand("An", "abc@def", null, "token"));

        result.Errors.ShouldContain(e => e.PropertyName == "PhoneNumber" && e.ErrorCode == CommonErrors.Format.Code);
    }

    [Fact]
    public void Validate_PhoneNumberTooLong_FailsWithMaxLength()
    {
        var result = _validator.Validate(new UpdateProfileCommand("An", new string('0', 21), null, "token"));

        result.Errors.ShouldContain(e => e.PropertyName == "PhoneNumber" && e.ErrorCode == CommonErrors.MaxLength.Code);
    }

    [Theory]
    [InlineData("vi")]
    [InlineData("en-US")]
    public void Validate_PreferredLanguageValidFormats_Succeed(string language)
    {
        var result = _validator.Validate(new UpdateProfileCommand("An", null, language, "token"));

        result.Errors.ShouldNotContain(e => e.PropertyName == "PreferredLanguage");
    }

    [Fact]
    public void Validate_PreferredLanguageInvalidFormat_FailsWithFormat()
    {
        var result = _validator.Validate(new UpdateProfileCommand("An", null, "VI", "token"));

        result.Errors.ShouldContain(e => e.PropertyName == "PreferredLanguage" && e.ErrorCode == CommonErrors.Format.Code);
    }

    [Fact]
    public void Validate_MissingVersion_DoesNotFailValidation()
    {
        // "Thiếu hoặc lệch ⇒ 409" — không phải lỗi validate. docs/contracts/profile.md §2.
        var result = _validator.Validate(new UpdateProfileCommand("An", null, null, null));

        result.Errors.ShouldNotContain(e => e.PropertyName == "Version");
    }
}
