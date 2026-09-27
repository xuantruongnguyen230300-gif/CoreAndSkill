using CoreAndSkill.ArchTests.Support;
using Shouldly;
using Xunit;

namespace CoreAndSkill.ArchTests;

// B6 — docs/RULES.md: mọi lời gọi IgnoreQueryFilters nêu tên filter được bỏ; dạng không tham số bị cấm tuyệt đối. Luật M5
// canh "có nằm trong allowlist không", không canh "bỏ filter nào".
//
// Tầm quét: mọi tệp .cs sản phẩm của năm project Core và host (ProductSourceFiles.Core).
public class UnnamedQueryFilterBypassTests
{
    [Fact]
    public void EveryIgnoreQueryFilters_NamesTheFilter()
    {
        var offenders = ProductSourceFiles.Core()
            .SelectMany(file => UnnamedQueryFilterBypassScanner.Scan(File.ReadAllText(file), file))
            .ToList();

        offenders.ShouldBeEmpty();
    }

    // T6 — tầm quét phải chạm những lời gọi thật (nêu tên) — OutboxDispatcher là một, theo đúng neo của dòng B6.
    [Fact]
    public void EveryIgnoreQueryFilters_NamesTheFilter_ScansRealCalls()
    {
        var files = ProductSourceFiles.Core();

        files.Sum(f => UnnamedQueryFilterBypassScanner.CountCalls(File.ReadAllText(f))).ShouldBeGreaterThan(0);
        var dispatcher = files.Single(f => f.EndsWith("OutboxDispatcher.cs", StringComparison.Ordinal));
        UnnamedQueryFilterBypassScanner.CountCalls(File.ReadAllText(dispatcher)).ShouldBeGreaterThan(0);
    }

    // Canary — dạng không tham số, cả ba cách viết.
    [Theory]
    [InlineData("db.Files.IgnoreQueryFilters().Where(f => f.IsDeleted)")]
    [InlineData("query?.IgnoreQueryFilters()")]
    [InlineData("EntityFrameworkQueryableExtensions.IgnoreQueryFilters(db.Files)")]
    [InlineData("Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.IgnoreQueryFilters(db.Files)")]
    public void Detector_B6_Catches_UnnamedBypass(string expression)
        => UnnamedQueryFilterBypassScanner.Scan(Wrap(expression), "fake.cs").ShouldNotBeEmpty();

    // Đúng hình dạng thật (FileMaintenanceHostedService, OutboxDispatcher): nêu tên một hoặc hai filter.
    [Theory]
    [InlineData("db.Files.IgnoreQueryFilters([CoreQueryFilters.TenantKey])")]
    [InlineData("db.Files.IgnoreQueryFilters([CoreQueryFilters.TenantKey, CoreQueryFilters.SoftDeleteKey])")]
    [InlineData("EntityFrameworkQueryableExtensions.IgnoreQueryFilters(db.Files, [CoreQueryFilters.SoftDeleteKey])")]
    public void Detector_B6_Ignores_NamedBypass(string expression)
        => UnnamedQueryFilterBypassScanner.Scan(Wrap(expression), "fake.cs").ShouldBeEmpty();

    private static string Wrap(string expression) => $$"""
        public sealed class Fake
        {
            public object Run(FakeDb db, System.Linq.IQueryable<object>? query) => {{expression}};
        }
        """;
}
