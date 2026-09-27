using CoreAndSkill.Core.Domain.Common;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.UnitTests.Common;

public class ErrorTests
{
    private static readonly Error Original = new("TEST.SAMPLE.ERROR", "Email '{Email}' đã được dùng.", ErrorType.Conflict);

    [Fact]
    public void WithParams_ReturnsNewInstance_OriginalUnchanged()
    {
        var withParams = Original.WithParams(("Email", "an@vd.vn"));

        withParams.Params["Email"].ShouldBe("an@vd.vn");
        Original.Params.ShouldBeEmpty(); // Error bất biến — catalog dùng static readonly an toàn
    }

    [Fact]
    public void WithParams_NullValue_BecomesEmptyString()
    {
        var withParams = Original.WithParams(("Email", null));

        withParams.Params["Email"].ShouldBe(string.Empty);
    }

    [Fact]
    public void WithFieldErrors_ReturnsNewInstance_OriginalUnchanged()
    {
        var fieldErrors = new Dictionary<string, IReadOnlyList<FieldError>>
        {
            ["Email"] = [new FieldError("CORE.VALIDATION.FORMAT", new Dictionary<string, string>())],
        };

        var withFieldErrors = Original.WithFieldErrors(fieldErrors);

        withFieldErrors.FieldErrors.ShouldContainKey("Email");
        Original.FieldErrors.ShouldBeEmpty();
    }
}
