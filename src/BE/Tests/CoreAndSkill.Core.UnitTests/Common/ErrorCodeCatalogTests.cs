using System.Reflection;
using System.Text.RegularExpressions;
using CoreAndSkill.Core.Application.Auth;
using CoreAndSkill.Core.Application.Common;
using CoreAndSkill.Core.Application.Permissions;
using CoreAndSkill.Core.Application.Roles;
using CoreAndSkill.Core.Application.Users;
using CoreAndSkill.Core.Domain.Common;
using CoreAndSkill.Core.Domain.Tenants;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.UnitTests.Common;

// Luật R3 (docs/RULES-BE.md §5, be-cqrs-handler.md §7.3–§7.4): mọi mã lỗi nghiệp vụ khớp khuôn MIỀN.TÀI_NGUYÊN.LÝ_DO,
// duy nhất trong toàn Core, và mã BE KHÔNG mang tài nguyên `CLIENT` (dành riêng cho bảng mã phía client).
//
// Khác B4SupportTests: catalog được TÌM bằng reflection — mọi lớp static tên `*Errors` trong hai assembly mà project test
// này tham chiếu (Core.Domain, Core.Application) — không liệt kê tay. Thêm một catalog mới thì nó vào tầm quét mà không
// ai phải nhớ sửa test.
//
// RANH GIỚI ĐÃ BIẾT: project test này không tham chiếu Core.Infrastructure và Core.Web. Catalog nằm ở đó
// (hôm nay: `SecurityErrors` của Core.Web) nằm NGOÀI tầm quét. Muốn phủ cần một project tham chiếu cả năm assembly
// (ArchTests có sẵn) hoặc thêm tham chiếu ở đây — quyết định hạ tầng test, không tự đổi.
//
// T12:cap-xanh-do — mỗi canary kỳ vọng ĐỎ (mã trùng, mã CLIENT, sai khuôn) có một canary XANH cùng phép dò, chỉ khác đúng
// mã bị cắm.
public partial class ErrorCodeCatalogTests
{
    [GeneratedRegex(@"^[A-Z][A-Z0-9]*(\.[A-Z][A-Z0-9_]*){2}$")]
    private static partial Regex CodePattern();

    private const string ClientResource = "CLIENT";

    // ===== Luật thật =====

    [Fact]
    public void EveryCoreErrorCode_MatchesTheDocumentedFormat()
        => FormatViolations(RealCatalogs()).ShouldBeEmpty();

    [Fact]
    public void EveryCoreErrorCode_IsUnique_AcrossAllCoreCatalogs()
        => DuplicateViolations(RealCatalogs()).ShouldBeEmpty();

    [Fact]
    public void NoCoreErrorCode_CarriesTheClientResource()
        => ClientViolations(RealCatalogs()).ShouldBeEmpty();

    // ===== T6 =====

    [Fact]
    public void ErrorCatalogDiscovery_FindsTheKnownCatalogs_AndAtLeastOneCodePerCatalog()
    {
        var catalogs = RealCatalogs();

        catalogs.ShouldNotBeEmpty();

        // Không liệt kê tay để QUÉT; liệt kê ở đây chỉ để chứng minh việc tìm bằng reflection không mù: catalog đã biết
        // phải có mặt, kể cả catalog mà B4SupportTests không nhắc tới (User, Role, Tenant, Permission, Auth, Common).
        catalogs.ShouldContain(typeof(UserErrors));
        catalogs.ShouldContain(typeof(RoleErrors));
        catalogs.ShouldContain(typeof(TenantErrors));
        catalogs.ShouldContain(typeof(PermissionErrors));
        catalogs.ShouldContain(typeof(AuthErrors));
        catalogs.ShouldContain(typeof(CommonErrors));

        // Catalog được tìm thấy mà không đọc ra mã nào thì phép dò khuôn/trùng/CLIENT chạy trên rỗng.
        foreach (var catalog in catalogs)
            ErrorsOf(catalog).ShouldNotBeEmpty($"Catalog {catalog.FullName} không đọc ra mã lỗi nào.");

        ErrorsOf(typeof(UserErrors)).ShouldContain(e => e.Code == UserErrors.NotFound.Code);
    }

    [Fact]
    public void ErrorCatalogDiscovery_ReadsEveryCatalogFromBothAssemblies()
    {
        var catalogs = RealCatalogs();

        catalogs.ShouldContain(t => t.Assembly == typeof(Error).Assembly);
        catalogs.ShouldContain(t => t.Assembly == typeof(CommonErrors).Assembly);
    }

    // ===== Canary (T1 + T12) =====

    [Fact]
    public void Detector_R3_Green_WellFormedUniqueCatalogs_ReportNothing()
    {
        Type[] catalogs = [typeof(FakeGoodErrors), typeof(FakeOtherGoodErrors)];

        FormatViolations(catalogs).ShouldBeEmpty();
        DuplicateViolations(catalogs).ShouldBeEmpty();
        ClientViolations(catalogs).ShouldBeEmpty();
    }

    [Fact]
    public void Detector_R3_Red_DuplicateCodeAcrossTwoCatalogs_IsReported()
        => DuplicateViolations([typeof(FakeGoodErrors), typeof(FakeDuplicateOfGoodErrors)])
            .ShouldBe(["CORE.FAKE.ONE"]);

    [Fact]
    public void Detector_R3_Red_DuplicateCodeInsideOneCatalog_IsReported()
        => DuplicateViolations([typeof(FakeSelfDuplicateErrors)]).ShouldBe(["CORE.FAKE.TWINS"]);

    [Fact]
    public void Detector_R3_Green_SameErrorAliasedInTwoCatalogs_IsNotADuplicate()
        => DuplicateViolations([typeof(FakeGoodErrors), typeof(FakeAliasOfGoodErrors)]).ShouldBeEmpty();

    [Fact]
    public void Detector_R3_Red_ClientResourceCode_IsReported()
        => ClientViolations([typeof(FakeClientErrors)]).ShouldBe(["CORE.CLIENT.NO_CONNECTION"]);

    [Fact]
    public void Detector_R3_Green_ClientAsReasonOrDomain_IsNotTheClientResource()
        => ClientViolations([typeof(FakeClientLookalikeErrors)]).ShouldBeEmpty();

    [Fact]
    public void Detector_R3_Red_MalformedCode_IsReported()
        => FormatViolations([typeof(FakeBadFormatErrors)]).ShouldBe(["core.fake.lower", "CORE.FAKE"]);

    [Fact]
    public void Detector_R3_Red_CatalogWithInternalField_IsStillRead()
        => DuplicateViolations([typeof(FakeGoodErrors), typeof(FakeHiddenDuplicateErrors)]).ShouldBe(["CORE.FAKE.ONE"]);

    [Fact]
    public void Detector_R3_Discovery_SkipsNonStaticAndNonErrorsClasses_ButFindsNested()
    {
        var found = DiscoverIn(typeof(DiscoveryHost).Assembly)
            .Where(t => t.DeclaringType == typeof(DiscoveryHost) || t == typeof(DiscoveryHost.NestedErrors))
            .ToList();

        found.ShouldContain(typeof(DiscoveryHost.NestedErrors));
        found.ShouldNotContain(typeof(DiscoveryHost.NotStaticErrors));
        found.ShouldNotContain(typeof(DiscoveryHost.StaticButNotACatalog));
    }

    // ===== Phép dò — hàm thuần, cả luật thật lẫn canary cùng gọi =====

    private static List<string> FormatViolations(IEnumerable<Type> catalogs)
        => [.. catalogs.SelectMany(ErrorsOf).Where(e => !CodePattern().IsMatch(e.Code)).Select(e => e.Code)];

    private static List<string> DuplicateViolations(IEnumerable<Type> catalogs)
        => [.. catalogs.SelectMany(ErrorsOf)
            .Distinct(ReferenceEqualityComparer.Instance).Cast<Error>() // một Error được gán lại ở catalog khác là bí danh, không phải trùng
            .GroupBy(e => e.Code, StringComparer.Ordinal)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)];

    private static List<string> ClientViolations(IEnumerable<Type> catalogs)
        => [.. catalogs.SelectMany(ErrorsOf)
            .Where(e => e.Code.Split('.') is [_, var resource, ..] && resource == ClientResource)
            .Select(e => e.Code)];

    private static IReadOnlyList<Type> RealCatalogs()
        => [.. DiscoverIn(typeof(Error).Assembly).Concat(DiscoverIn(typeof(CommonErrors).Assembly))
            .OrderBy(t => t.FullName, StringComparer.Ordinal)];

    // Catalog = lớp static (abstract + sealed) tên kết thúc bằng `Errors`, kể cả lớp lồng; bỏ lớp do trình biên dịch sinh.
    private static IEnumerable<Type> DiscoverIn(Assembly assembly)
        => assembly.GetTypes().Where(t =>
            t.IsAbstract && t.IsSealed
            && t.Name.EndsWith("Errors", StringComparison.Ordinal)
            && t.GetCustomAttribute<System.Runtime.CompilerServices.CompilerGeneratedAttribute>() is null);

    private static List<Error> ErrorsOf(Type catalog)
        => [.. catalog
            .GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(f => f.FieldType == typeof(Error))
            .Select(f => (Error)f.GetValue(null)!)];

    // ===== Catalog giả cho canary =====

    private static Error E(string code) => new(code, "msg", ErrorType.Validation);

    private static class FakeGoodErrors { public static readonly Error One = E("CORE.FAKE.ONE"); }

    private static class FakeOtherGoodErrors { public static readonly Error Two = E("CORE.FAKE.TWO"); }

    private static class FakeDuplicateOfGoodErrors { public static readonly Error Again = E("CORE.FAKE.ONE"); }

    private static class FakeAliasOfGoodErrors { public static readonly Error Alias = FakeGoodErrors.One; }

    private static class FakeSelfDuplicateErrors
    {
        public static readonly Error A = E("CORE.FAKE.TWINS");
        public static readonly Error B = E("CORE.FAKE.TWINS");
    }

    private static class FakeHiddenDuplicateErrors { internal static readonly Error Hidden = E("CORE.FAKE.ONE"); }

    private static class FakeClientErrors { public static readonly Error NoConnection = E("CORE.CLIENT.NO_CONNECTION"); }

    private static class FakeClientLookalikeErrors
    {
        public static readonly Error AsReason = E("CORE.FAKE.CLIENT");
        public static readonly Error AsDomain = E("CLIENT.FAKE.REASON");
        public static readonly Error Prefixed = E("CORE.CLIENTELE.REASON");
    }

    private static class FakeBadFormatErrors
    {
        public static readonly Error Lower = E("core.fake.lower");
        public static readonly Error TwoSegments = E("CORE.FAKE");
        public static readonly Error Fine = E("CORE.FAKE.FINE");
    }

    private static class DiscoveryHost
    {
        public static class NestedErrors { public static readonly Error X = E("CORE.NEST.X"); }

        public sealed class NotStaticErrors { public static readonly Error Y = E("CORE.NOTSTATIC.Y"); }

        public static class StaticButNotACatalog { public static readonly Error Z = E("CORE.OTHER.Z"); }
    }
}
