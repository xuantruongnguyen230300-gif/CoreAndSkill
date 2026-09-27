using CoreAndSkill.ArchTests.Support;
using CoreAndSkill.Core.Web.Http;
using Shouldly;
using Xunit;

namespace CoreAndSkill.ArchTests;

// A16 — docs/RULES.md §3, docs/quy-uoc/be-api-controller.md §8.1: tiền tố `api/v<N>/<khu>` của mọi đường dẫn trong code —
// route controller, header Location, so khớp HttpRequest.Path — ghép từ ApiRoutes, không gõ tay. Gõ tay ở một chỗ là hai
// nguồn cho cùng một tiền tố; lên v2 thì chỗ gõ tay ở lại v1 mà không gì báo.
//
// Tầm quét: mọi tệp .cs sản phẩm của năm project Core và host (ProductSourceFiles.Core), TRỪ đúng tệp khai hằng số.
public class HandTypedApiPrefixTests
{
    [Fact]
    public void CoreSource_MustNotContain_HandTypedApiPrefix()
    {
        var offenders = ScannedFiles()
            .SelectMany(file => HandTypedApiPrefixScanner.Scan(File.ReadAllText(file), file))
            .ToList();

        offenders.ShouldBeEmpty();
    }

    // T6 — tầm quét chạm tệp thật: controller dùng hằng số route, controller dựng header Location, và host. Tệp khai hằng
    // số KHÔNG nằm trong tầm, nhưng nó phải còn tồn tại — nếu không, lời loại trừ là loại trừ một tệp ma.
    [Fact]
    public void CoreSource_MustNotContain_HandTypedApiPrefix_ScansRealControllersAndHost()
    {
        var files = ScannedFiles();

        files.ShouldContain(f => ProductSourceFiles.EndsWith(f, "Controllers", "UsersController.cs"));
        files.ShouldContain(f => ProductSourceFiles.EndsWith(f, "CoreAndSkill.Api", "Program.cs"));
        files.ShouldNotContain(f => ProductSourceFiles.EndsWith(f, "Http", "ApiRoutes.cs"));
        ProductSourceFiles.Core().ShouldContain(f => ProductSourceFiles.EndsWith(f, "Http", "ApiRoutes.cs"));
    }

    [Theory]
    [InlineData("""[Route("api/v1/core/users")] public sealed class Fake { }""")]
    [InlineData("""public sealed class Fake { string L(System.Guid id) => $"/api/v1/core/users/{id}"; }""")]
    [InlineData("""public sealed class Fake { const string R = @"API/V2/core"; }""")]
    [InlineData(""""public sealed class Fake { const string R = """ /api/v1/khu """; }"""")]
    public void Detector_A16_Catches_HandTypedPrefix(string source)
        => HandTypedApiPrefixScanner.Scan(source, "fake.cs").ShouldNotBeEmpty();

    // Đúng hình dạng thật: route ghép hằng số; chú thích mô tả route; chuỗi tình cờ chứa "api/v" ở giữa.
    [Theory]
    [InlineData("""[Route(CoreRoutes.Users)] public sealed class Fake { }""")]
    [InlineData("// POST /api/v1/core/users — docs/contracts/users.md §5.\npublic sealed class Fake { }")]
    [InlineData("""/* GET "/api/v1/core/roles" */ public sealed class Fake { }""")]
    [InlineData("""public sealed class Fake { const string Doc = "xem /api/v1 trong tài liệu"; }""")]
    [InlineData("""public sealed class Fake { string L(System.Guid id) => $"/{ApiRoutes.V1}/users/{id}"; }""")]
    public void Detector_A16_Ignores_ConstantsCommentsAndNonPrefixText(string source)
        => HandTypedApiPrefixScanner.Scan(source, "fake.cs").ShouldBeEmpty();

    // be-api-controller.md §8.1: module khai route dưới ApiRoutes.V1 + "/<module>" và KHÔNG BAO GIỜ dưới tiền tố của
    // Core — nên hằng số công khai cho module chỉ có Root và V1. Đoạn "/core" là việc riêng của CoreRoutes (internal):
    // để nó public là mời module đăng ký dưới /core mà trình biên dịch không phản đối.
    [Fact]
    public void ApiRoutes_ExposesOnly_RootAndV1_ToModules()
    {
        var publicConstants = typeof(ApiRoutes)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Select(f => f.Name)
            .Order(StringComparer.Ordinal)
            .ToList();

        publicConstants.ShouldBe([nameof(ApiRoutes.Root), nameof(ApiRoutes.V1)]);
        typeof(ApiRoutes).IsPublic.ShouldBeTrue();
    }

    private static IReadOnlyList<string> ScannedFiles()
        => ProductSourceFiles.Core().Where(f => !ProductSourceFiles.EndsWith(f, "Http", "ApiRoutes.cs")).ToList();
}
