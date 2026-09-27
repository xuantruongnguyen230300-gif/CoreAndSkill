using CoreAndSkill.Core.Application.Common;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.UnitTests.Common;

// docs/quy-uoc/be-cqrs-handler.md §5.2 — PropertyValue KHÔNG BAO GIỜ được lọt ra ngoài; chỉ khoá
// trong allowlist (MaxLength, MinLength) mới ra dây.
public class MessageParamPolicyTests
{
    [Fact]
    public void Filter_EmptyInput_ReturnsEmpty()
    {
        var result = MessageParamPolicy.Filter(new Dictionary<string, object>());

        result.ShouldBeEmpty();
    }

    [Fact]
    public void Filter_AllowlistedKey_PassesThrough()
    {
        var input = new Dictionary<string, object> { ["MaxLength"] = 64 };

        var result = MessageParamPolicy.Filter(input);

        result.ShouldContainKeyAndValue("MaxLength", "64");
    }

    [Fact]
    public void Filter_MinLength_PassesThrough()
    {
        var input = new Dictionary<string, object> { ["MinLength"] = 8 };

        var result = MessageParamPolicy.Filter(input);

        result.ShouldContainKeyAndValue("MinLength", "8");
    }

    [Fact]
    public void Filter_PropertyValue_NeverLeaks()
    {
        var input = new Dictionary<string, object>
        {
            ["PropertyValue"] = "mật khẩu người dùng vừa gõ",
            ["MaxLength"] = 10,
        };

        var result = MessageParamPolicy.Filter(input);

        result.ShouldNotContainKey("PropertyValue");
        result.ShouldContainKey("MaxLength");
    }

    // docs/quy-uoc/be-cqrs-handler.md §7.1: allowlist phải chứa MaxItems — thiếu thì FE nhận CORE.VALIDATION.MAX_ITEMS mà không biết trần là bao nhiêu.
    [Fact]
    public void Filter_MaxItems_PassesThrough()
    {
        var input = new Dictionary<string, object> { ["MaxItems"] = 50 };

        var result = MessageParamPolicy.Filter(input);

        result.ShouldContainKeyAndValue("MaxItems", "50");
    }

    [Fact]
    public void Filter_UnknownKey_IsDropped()
    {
        var input = new Dictionary<string, object> { ["SomeUnknownPlaceholder"] = "x" };

        var result = MessageParamPolicy.Filter(input);

        result.ShouldBeEmpty();
    }
}
