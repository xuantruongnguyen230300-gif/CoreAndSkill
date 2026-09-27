using CoreAndSkill.Core.Domain.Common;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.UnitTests.Common;

public class MessageTemplateRendererTests
{
    [Fact]
    public void Render_SubstitutesNamedPlaceholder()
    {
        var result = MessageTemplateRenderer.Render(
            "Email '{Email}' đã được dùng.",
            new Dictionary<string, string> { ["Email"] = "an@vd.vn" });

        result.ShouldBe("Email 'an@vd.vn' đã được dùng.");
    }

    [Fact]
    public void Render_NullArgs_ReturnsTemplateUnchanged()
    {
        var result = MessageTemplateRenderer.Render("Không có tham số.", null);

        result.ShouldBe("Không có tham số.");
    }

    [Fact]
    public void Render_EmptyArgs_ReturnsTemplateUnchanged()
    {
        var result = MessageTemplateRenderer.Render(
            "Tối đa {MaxLength} ký tự.", new Dictionary<string, string>());

        result.ShouldBe("Tối đa {MaxLength} ký tự.");
    }

    [Fact]
    public void Render_MissingPlaceholderValue_KeepsPlaceholder()
    {
        var result = MessageTemplateRenderer.Render(
            "Tối đa {MaxLength} ký tự.", new Dictionary<string, string> { ["Other"] = "x" });

        result.ShouldBe("Tối đa {MaxLength} ký tự.");
    }

    [Fact]
    public void Render_MultiplePlaceholders_SubstitutesAll()
    {
        var result = MessageTemplateRenderer.Render(
            "{A} và {B}.",
            new Dictionary<string, string> { ["A"] = "một", ["B"] = "hai" });

        result.ShouldBe("một và hai.");
    }
}
