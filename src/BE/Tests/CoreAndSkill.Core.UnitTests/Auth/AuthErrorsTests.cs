using CoreAndSkill.Core.Application.Auth;
using CoreAndSkill.Core.Domain.Common;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.UnitTests.Auth;

// docs/contracts/auth.md — mã là hợp đồng công khai, đổi mã là breaking change.
public class AuthErrorsTests
{
    [Theory]
    [InlineData(nameof(AuthErrors.InvalidCredentials), "CORE.AUTH.INVALID_CREDENTIALS", ErrorType.BusinessRule)]
    [InlineData(nameof(AuthErrors.LockedOut), "CORE.AUTH.LOCKED_OUT", ErrorType.BusinessRule)]
    [InlineData(nameof(AuthErrors.ChangePasswordFailed), "CORE.AUTH.CHANGE_PASSWORD_FAILED", ErrorType.BusinessRule)]
    [InlineData(nameof(AuthErrors.PasswordChangeNotRequired), "CORE.AUTH.PASSWORD_CHANGE_NOT_REQUIRED", ErrorType.BusinessRule)]
    [InlineData(nameof(AuthErrors.NewPasswordSameAsCurrent), "CORE.AUTH.NEW_PASSWORD_SAME_AS_CURRENT", ErrorType.Validation)]
    [InlineData(nameof(AuthErrors.PasswordMismatch), "CORE.AUTH.PASSWORD_MISMATCH", ErrorType.Validation)]
    [InlineData(nameof(AuthErrors.PasswordTooShort), "CORE.AUTH.PASSWORD_TOO_SHORT", ErrorType.Validation)]
    [InlineData(nameof(AuthErrors.PasswordRequiresDigit), "CORE.AUTH.PASSWORD_REQUIRES_DIGIT", ErrorType.Validation)]
    [InlineData(nameof(AuthErrors.PasswordRequiresLower), "CORE.AUTH.PASSWORD_REQUIRES_LOWER", ErrorType.Validation)]
    [InlineData(nameof(AuthErrors.PasswordRequiresUpper), "CORE.AUTH.PASSWORD_REQUIRES_UPPER", ErrorType.Validation)]
    [InlineData(nameof(AuthErrors.PasswordRequiresNonAlphanumeric), "CORE.AUTH.PASSWORD_REQUIRES_NON_ALPHANUMERIC", ErrorType.Validation)]
    [InlineData(nameof(AuthErrors.PasswordRequiresUniqueChars), "CORE.AUTH.PASSWORD_REQUIRES_UNIQUE_CHARS", ErrorType.Validation)]
    [InlineData(nameof(AuthErrors.PasswordPolicyViolation), "CORE.AUTH.PASSWORD_POLICY_VIOLATION", ErrorType.Validation)]
    public void Catalog_HasExpectedCodeAndType(string propertyName, string expectedCode, ErrorType expectedType)
    {
        var error = (Error)typeof(AuthErrors).GetField(propertyName)!.GetValue(null)!;

        error.Code.ShouldBe(expectedCode);
        error.Type.ShouldBe(expectedType);
    }
}
