using CoreAndSkill.Core.Web.Security;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.IntegrationTests.Security;

// Quy tắc origin chung — docs/quy-uoc/be-api-controller.md §7.3, ADR-0129. Hàm thuần, không host.
public sealed class OriginAllowlistTests
{
    private static readonly string[] Listed = ["https://localhost:4200", "https://app.example.com"];

    [Theory]
    [InlineData("https://localhost:55697")]
    [InlineData("https://localhost")]
    [InlineData("https://LOCALHOST:55697")]
    public void Development_AcceptsAnyHttpsLocalhostPort(string origin)
        => OriginAllowlist.IsAllowed(origin, Listed, isDevelopment: true).ShouldBeTrue();

    [Theory]
    [InlineData("https://evil.example")]
    [InlineData("https://localhost.evil.example")]
    [InlineData("https://localhost.evil.example:55697")]
    [InlineData("https://localhost@evil.example")]
    [InlineData("http://localhost:55697")]
    [InlineData("https://127.0.0.1:55697")]
    [InlineData("https://[::1]:55697")]
    [InlineData("https://loopback:55697")]
    [InlineData("https://x.localhost:55697")]
    [InlineData("https://localhost:55697/")]
    [InlineData("null")]
    [InlineData("")]
    [InlineData(null)]
    public void Development_RejectsEverythingElse(string? origin)
        => OriginAllowlist.IsAllowed(origin, Listed, isDevelopment: true).ShouldBeFalse();

    [Fact]
    public void OutsideDevelopment_LocalhostNotInTheList_IsRejected()
        => OriginAllowlist.IsAllowed("https://localhost:55697", Listed, isDevelopment: false).ShouldBeFalse();

    [Fact]
    public void OutsideDevelopment_LocalhostInTheList_IsAccepted()
        => OriginAllowlist.IsAllowed("https://localhost:55697", ["https://localhost:55697"], isDevelopment: false).ShouldBeTrue();

    [Theory]
    [InlineData("https://APP.example.com", true)]
    [InlineData("https://app.example.com", true)]
    [InlineData("https://app.example.com:8443", false)]
    public void ListedEntries_AreMatchedIgnoringCase_InEveryEnvironment(string origin, bool expected)
    {
        OriginAllowlist.IsAllowed(origin, Listed, isDevelopment: false).ShouldBe(expected);
        OriginAllowlist.IsAllowed(origin, Listed, isDevelopment: true).ShouldBe(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void NullOrEmptyOrigin_IsRejectedEvenIfTheListHoldsAnEmptyEntry(string? origin)
        => OriginAllowlist.IsAllowed(origin, [""], isDevelopment: false).ShouldBeFalse();

    [Fact]
    public void NullList_OutsideDevelopment_RejectsEverything()
        => OriginAllowlist.IsAllowed("https://localhost:4200", null, isDevelopment: false).ShouldBeFalse();
}
