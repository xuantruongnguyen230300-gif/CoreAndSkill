using System.Text.Encodings.Web;
using CoreAndSkill.Core.IntegrationTests.Support;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Web;

// docs/wiki-core/be/07-observability.md §2.3: ngoài Development console ghi JSON một dòng; Development giữ dạng chữ.
public sealed class ConsoleLogFormatTests
{
    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    public void OutsideDevelopment_ConsoleFormatter_IsJson_WithTheThreeSettings(string environment)
    {
        using var factory = new CoreWebApplicationFactory(environment: environment);

        var console = factory.Services.GetRequiredService<IOptions<ConsoleLoggerOptions>>().Value;
        var json = factory.Services.GetRequiredService<IOptionsMonitor<JsonConsoleFormatterOptions>>().CurrentValue;

        console.FormatterName.ShouldBe(ConsoleFormatterNames.Json);
        json.IncludeScopes.ShouldBeTrue();
        json.UseUtcTimestamp.ShouldBeTrue();
        json.TimestampFormat.ShouldNotBeNullOrWhiteSpace();
        json.JsonWriterOptions.Indented.ShouldBeFalse();
        json.JsonWriterOptions.Encoder.ShouldBeSameAs(JavaScriptEncoder.UnsafeRelaxedJsonEscaping);
    }

    [Fact]
    public void InDevelopment_ConsoleFormatter_IsNotJson()
    {
        using var factory = new CoreWebApplicationFactory(environment: "Development");

        var console = factory.Services.GetRequiredService<IOptions<ConsoleLoggerOptions>>().Value;

        console.FormatterName.ShouldNotBe(ConsoleFormatterNames.Json);
    }
}
