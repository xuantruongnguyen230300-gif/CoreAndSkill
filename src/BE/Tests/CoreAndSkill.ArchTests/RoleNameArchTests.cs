using System.Runtime.CompilerServices;
using CoreAndSkill.ArchTests.Support;
using Shouldly;
using Xunit;

namespace CoreAndSkill.ArchTests;

// S1 + S2 — docs/RULES.md §6. Core không được khai hằng số vai trò (S1) và không được phân quyền
// bằng cách so sánh tên vai trò (S2) — role là DỮ LIỆU trong bảng core.role, không phải một danh
// tính có thể gắn cứng trong code. docs/wiki-core/be/trien-khai/03-b2-phan-quyen-va-bien.md §3:
// "phân nhánh theo tên vai trò" là lỗi hay gặp nhất ở B2 — cổng này là cơ chế bắt lỗi đó khi ai đó
// thêm nó về sau, thay vì chỉ trông chờ vào review.
//
// Tự kiểm (T6, docs/wiki-core/be/04-testing-strategy.md §3): đã tạm thêm
// `private const string AdminRole = "Admin";` vào CorePermissions.cs và `x.RoleName == "Admin"`
// vào PermissionChecker.cs, chạy `dotnet test` thấy đúng hai test rule thật (không phải chỉ
// Detector_*) chuyển đỏ, rồi gỡ cả hai và chạy lại thấy xanh — không suy diễn.
//
// Tự kiểm mở rộng (core-review đợt 2 — S1 bỏ sót enum/mảng, S2 bỏ sót pattern/switch/Equals/
// Contains): đã lần lượt tạm thêm vào CorePermissions.cs (`public enum SystemRole { Admin,
// Manager, SystemOperator }`, rồi `public static readonly string[] SystemRoles = ["Admin",
// "Manager"];`) và vào TenantProvisioningService.cs, ngay trước dòng dùng thật
// `seedRolePermission.RoleName` (`... is "Admin"`, `switch (...RoleName) { case "Admin": ... }` +
// `...RoleName switch { "Admin" => ... }`, `...RoleName.Equals("Admin")` +
// `new[] { "Admin", "Manager" }.Contains(...RoleName)`) — mỗi hình dạng một vòng riêng, chạy
// `dotnet test` thấy đúng test rule thật chuyển đỏ, gỡ ra, so sánh SHA-256 của cả hai file với bản
// gốc thấy khớp tuyệt đối, rồi chạy lại thấy xanh — không suy diễn.
public class RoleNameArchTests
{
    // ===== S1 — Core_MustNotDeclare_RoleConstants =====

    [Fact]
    public void Core_MustNotDeclare_RoleConstants()
    {
        var offenders = ScanCoreSourceFiles(RoleConstantScanner.Scan);

        offenders.ShouldBeEmpty();
    }

    // T6 — một detector duyệt tập rỗng luôn PASS; test này chứng minh việc quét thật sự chạm ít
    // nhất một file .cs có thật dưới src/BE/Core.
    [Fact]
    public void Core_MustNotDeclare_RoleConstants_ScansAtLeastOneRealFile()
    {
        EnumerateCoreSourceFiles().Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void Detector_S1_Catches_RealViolation()
    {
        const string source = """
            namespace CoreAndSkill.Core.Application.Fake;

            public static class FakeRoleConstants
            {
                public const string AdminRole = "Admin";
            }
            """;

        var violations = RoleConstantScanner.Scan(source, "fake.cs");

        violations.ShouldNotBeEmpty();
    }

    // Detector_S1_Ignores_PermissionCodeConstant — đúng hình dạng thật của
    // CoreAndSkill.Core.Application/Permissions/CorePermissions.cs: tên chứa "Role" nhưng giá trị
    // là MÃ QUYỀN (chữ thường, có dấu chấm), không phải tên vai trò.
    [Fact]
    public void Detector_S1_Ignores_PermissionCodeConstant()
    {
        const string source = """
            namespace CoreAndSkill.Core.Application.Fake;

            public static class FakePermissions
            {
                public const string RoleRead = "core.role.read";
            }
            """;

        var violations = RoleConstantScanner.Scan(source, "fake.cs");

        violations.ShouldBeEmpty();
    }

    // Detector_S1_Ignores_NonRoleConstant — hằng số string bình thường, tên không gợi ý vai trò.
    [Fact]
    public void Detector_S1_Ignores_NonRoleConstant()
    {
        const string source = """
            namespace CoreAndSkill.Core.Application.Fake;

            public static class FakeConstants
            {
                public const string DefaultSortColumn = "createdAt";
            }
            """;

        var violations = RoleConstantScanner.Scan(source, "fake.cs");

        violations.ShouldBeEmpty();
    }

    // Detector_S1_Catches_RoleEnum — enum có TÊN gợi ý vai trò và các THÀNH VIÊN trông như tên vai
    // trò cụ thể. Kịch bản lọt gốc (báo cáo core-reviewer): `public enum SystemRole { Admin, ... }`.
    [Fact]
    public void Detector_S1_Catches_RoleEnum()
    {
        const string source = """
            namespace CoreAndSkill.Core.Application.Fake;

            public enum SystemRole
            {
                Admin,
                Manager,
                SystemOperator,
            }
            """;

        var violations = RoleConstantScanner.Scan(source, "fake.cs");

        violations.ShouldNotBeEmpty();
    }

    // Detector_S1_Ignores_NonRoleEnum — enum có tên không gợi ý vai trò (đúng hình dạng thật của
    // CoreAndSkill.Core.Domain.Common.ErrorType) — dù thành viên là PascalCase, KHÔNG bị bắt vì tên
    // enum không chứa "Role".
    [Fact]
    public void Detector_S1_Ignores_NonRoleEnum()
    {
        const string source = """
            namespace CoreAndSkill.Core.Domain.Fake;

            public enum ErrorType
            {
                Validation,
                NotFound,
                Conflict,
            }
            """;

        var violations = RoleConstantScanner.Scan(source, "fake.cs");

        violations.ShouldBeEmpty();
    }

    // Detector_S1_Catches_RoleArrayField — mảng string static readonly, TÊN gợi ý vai trò và MỌI
    // phần tử literal trông như tên vai trò cụ thể. Kịch bản lọt gốc (báo cáo core-reviewer):
    // `public static readonly string[] SystemRoles = ["Admin", "Manager"];`.
    [Fact]
    public void Detector_S1_Catches_RoleArrayField()
    {
        const string source = """
            namespace CoreAndSkill.Core.Application.Fake;

            public static class FakeRoleCatalog
            {
                public static readonly string[] SystemRoles = ["Admin", "Manager"];
            }
            """;

        var violations = RoleConstantScanner.Scan(source, "fake.cs");

        violations.ShouldNotBeEmpty();
    }

    // Detector_S1_Ignores_NonRoleArrayField — đúng hình dạng thật của GetUsersListQueryValidator:
    // mảng string static readonly, giá trị thường (không PascalCase) và/hoặc tên không gợi ý vai
    // trò — KHÔNG bị bắt.
    [Fact]
    public void Detector_S1_Ignores_NonRoleArrayField()
    {
        const string source = """
            namespace CoreAndSkill.Core.Application.Fake;

            public static class FakeUsersListQueryValidator
            {
                private static readonly string[] StatusAllowlist = ["active", "locked"];
            }
            """;

        var violations = RoleConstantScanner.Scan(source, "fake.cs");

        violations.ShouldBeEmpty();
    }

    // Detector_S1_Ignores_RoleArrayField_WithNonLiteralElement — mảng có TÊN gợi ý vai trò nhưng
    // MỘT phần tử không phải literal string cố định (ví dụ đến từ biến/hàm) — không suy diễn được
    // giá trị thật nên KHÔNG bắt, an toàn hơn báo sai.
    [Fact]
    public void Detector_S1_Ignores_RoleArrayField_WithNonLiteralElement()
    {
        const string source = """
            namespace CoreAndSkill.Core.Application.Fake;

            public static class FakeRoleCatalog
            {
                public static readonly string[] SystemRoles = ["Admin", SomeRuntimeValue];
            }
            """;

        var violations = RoleConstantScanner.Scan(source, "fake.cs");

        violations.ShouldBeEmpty();
    }

    // ===== S2 — Authorization_MustCheck_Permission_NotRoleName =====

    [Fact]
    public void Authorization_MustCheck_Permission_NotRoleName()
    {
        var offenders = ScanCoreSourceFiles(RoleNameComparisonScanner.Scan);

        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void Authorization_MustCheck_Permission_NotRoleName_ScansAtLeastOneRealFile()
    {
        EnumerateCoreSourceFiles().Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void Detector_S2_Catches_RealViolation()
    {
        const string source = """
            namespace CoreAndSkill.Core.Web.Fake;

            public static class FakeAuthorizationCheck
            {
                public static bool IsAllowed(FakeUser user) => user.RoleName == "Admin";
            }

            public sealed class FakeUser
            {
                public string RoleName { get; set; } = "";
            }
            """;

        var violations = RoleNameComparisonScanner.Scan(source, "fake.cs");

        violations.ShouldNotBeEmpty();
    }

    // Detector_S2_Ignores_PermissionCheckerUsage — đúng hình dạng thật của
    // PermissionChecker.cs: so permission code qua IReadOnlySet<string>.Contains, không so tên vai
    // trò bằng ==.
    [Fact]
    public void Detector_S2_Ignores_PermissionCheckerUsage()
    {
        const string source = """
            namespace CoreAndSkill.Core.Infrastructure.Fake;

            public static class FakePermissionCheck
            {
                public static bool IsAllowed(System.Collections.Generic.IReadOnlySet<string> effective, string permissionKey)
                    => effective.Contains(permissionKey);
            }
            """;

        var violations = RoleNameComparisonScanner.Scan(source, "fake.cs");

        violations.ShouldBeEmpty();
    }

    // Detector_S2_Ignores_RoleIdComparison — RoleId (định danh Guid) khác RoleName (chuỗi tên); so
    // sánh RoleId là hợp lệ (UserPrivilegeGuard, PermissionChecker.GetPermissionsForRolesAsync đều
    // làm vậy) và KHÔNG phải hình dạng luật S2 cấm.
    [Fact]
    public void Detector_S2_Ignores_RoleIdComparison()
    {
        const string source = """
            namespace CoreAndSkill.Core.Infrastructure.Fake;

            public static class FakeRoleIdCheck
            {
                public static bool Matches(FakeUserRole ur, System.Guid roleId) => ur.RoleId == roleId;
            }

            public sealed class FakeUserRole
            {
                public System.Guid RoleId { get; set; }
            }
            """;

        var violations = RoleNameComparisonScanner.Scan(source, "fake.cs");

        violations.ShouldBeEmpty();
    }

    // Detector_S2_Ignores_NormalizedNameComparison — so trùng tên khi CRUD vai trò
    // (RoleQueryService.IsNameTakenAsync: r.NormalizedName == normalized) là kiểm trùng dữ liệu,
    // không phải quyết định phân quyền — KHÔNG thuộc hình dạng luật S2 cấm.
    [Fact]
    public void Detector_S2_Ignores_NormalizedNameComparison()
    {
        const string source = """
            namespace CoreAndSkill.Core.Infrastructure.Fake;

            public static class FakeNameUniquenessCheck
            {
                public static bool IsTaken(FakeRole r, string normalized) => r.NormalizedName == normalized;
            }

            public sealed class FakeRole
            {
                public string NormalizedName { get; set; } = "";
            }
            """;

        var violations = RoleNameComparisonScanner.Scan(source, "fake.cs");

        violations.ShouldBeEmpty();
    }

    // Detector_S2_Catches_ConstantPattern — `is "Admin"` (constant pattern). Kịch bản lọt gốc
    // (báo cáo core-reviewer): `if (user.RoleName is "Admin")`.
    [Fact]
    public void Detector_S2_Catches_ConstantPattern()
    {
        const string source = """
            namespace CoreAndSkill.Core.Web.Fake;

            public static class FakeAuthorizationCheck
            {
                public static bool IsAllowed(FakeUser user) => user.RoleName is "Admin";
            }

            public sealed class FakeUser
            {
                public string RoleName { get; set; } = "";
            }
            """;

        var violations = RoleNameComparisonScanner.Scan(source, "fake.cs");

        violations.ShouldNotBeEmpty();
    }

    // Detector_S2_Catches_ConstantPattern_Negated — `is not "Admin"` vẫn là constant pattern, chỉ
    // lồng trong một unary pattern — vẫn phải bắt.
    [Fact]
    public void Detector_S2_Catches_ConstantPattern_Negated()
    {
        const string source = """
            namespace CoreAndSkill.Core.Web.Fake;

            public static class FakeAuthorizationCheck
            {
                public static bool IsDenied(FakeUser user) => user.RoleName is not "Admin";
            }

            public sealed class FakeUser
            {
                public string RoleName { get; set; } = "";
            }
            """;

        var violations = RoleNameComparisonScanner.Scan(source, "fake.cs");

        violations.ShouldNotBeEmpty();
    }

    // Detector_S2_Ignores_TypePattern — `is FakeAdminUser` (type pattern, không phải constant
    // pattern) trên một thành viên Role/RoleName — không phải hình dạng luật S2 cấm.
    [Fact]
    public void Detector_S2_Ignores_TypePattern()
    {
        const string source = """
            namespace CoreAndSkill.Core.Web.Fake;

            public static class FakeCheck
            {
                public static bool IsString(FakeUser user) => user.Role is string;
            }

            public sealed class FakeUser
            {
                public object Role { get; set; } = "";
            }
            """;

        var violations = RoleNameComparisonScanner.Scan(source, "fake.cs");

        violations.ShouldBeEmpty();
    }

    // Detector_S2_Catches_SwitchStatement — kịch bản lọt gốc (báo cáo core-reviewer):
    // `switch (user.RoleName) { case "Admin": ... }`.
    [Fact]
    public void Detector_S2_Catches_SwitchStatement()
    {
        const string source = """
            namespace CoreAndSkill.Core.Web.Fake;

            public static class FakeAuthorizationCheck
            {
                public static bool IsAllowed(FakeUser user)
                {
                    switch (user.RoleName)
                    {
                        case "Admin":
                            return true;
                        default:
                            return false;
                    }
                }
            }

            public sealed class FakeUser
            {
                public string RoleName { get; set; } = "";
            }
            """;

        var violations = RoleNameComparisonScanner.Scan(source, "fake.cs");

        violations.ShouldNotBeEmpty();
    }

    // Detector_S2_Catches_SwitchExpression — cùng hình dạng nhưng viết bằng switch-expression, lối
    // viết đã quen thuộc trong codebase này (RoleQueryService.cs).
    [Fact]
    public void Detector_S2_Catches_SwitchExpression()
    {
        const string source = """
            namespace CoreAndSkill.Core.Web.Fake;

            public static class FakeAuthorizationCheck
            {
                public static bool IsAllowed(FakeUser user) => user.RoleName switch
                {
                    "Admin" => true,
                    _ => false,
                };
            }

            public sealed class FakeUser
            {
                public string RoleName { get; set; } = "";
            }
            """;

        var violations = RoleNameComparisonScanner.Scan(source, "fake.cs");

        violations.ShouldNotBeEmpty();
    }

    // Detector_S2_Ignores_SwitchOnOtherMember — đúng hình dạng thật của RoleQueryService.cs
    // (switch trên tuple (SortBy, SortDescending), không phải trên Role/RoleName) — KHÔNG bị bắt.
    [Fact]
    public void Detector_S2_Ignores_SwitchOnOtherMember()
    {
        const string source = """
            namespace CoreAndSkill.Core.Infrastructure.Fake;

            public static class FakeSort
            {
                public static string Describe(string sortBy, bool descending) => (sortBy, descending) switch
                {
                    ("createdAt", false) => "asc",
                    _ => "other",
                };
            }
            """;

        var violations = RoleNameComparisonScanner.Scan(source, "fake.cs");

        violations.ShouldBeEmpty();
    }

    // Detector_S2_Catches_EqualsCall — kịch bản lọt gốc (báo cáo core-reviewer):
    // `user.RoleName.Equals("Admin")`.
    [Fact]
    public void Detector_S2_Catches_EqualsCall()
    {
        const string source = """
            namespace CoreAndSkill.Core.Web.Fake;

            public static class FakeAuthorizationCheck
            {
                public static bool IsAllowed(FakeUser user) => user.RoleName.Equals("Admin");
            }

            public sealed class FakeUser
            {
                public string RoleName { get; set; } = "";
            }
            """;

        var violations = RoleNameComparisonScanner.Scan(source, "fake.cs");

        violations.ShouldNotBeEmpty();
    }

    // Detector_S2_Catches_ContainsCall — danh sách tên vai trò literal kiểm bằng .Contains(...)
    // trên chính RoleName — cùng tinh thần S2, RoleName xuất hiện ở vị trí đối số thay vì receiver.
    [Fact]
    public void Detector_S2_Catches_ContainsCall()
    {
        const string source = """
            namespace CoreAndSkill.Core.Web.Fake;

            public static class FakeAuthorizationCheck
            {
                public static bool IsAllowed(FakeUser user) =>
                    new[] { "Admin", "Manager" }.Contains(user.RoleName);
            }

            public sealed class FakeUser
            {
                public string RoleName { get; set; } = "";
            }
            """;

        var violations = RoleNameComparisonScanner.Scan(source, "fake.cs");

        violations.ShouldNotBeEmpty();
    }

    // Detector_S2_Ignores_EqualsCall_OnNormalizedName — đúng hình dạng thật của
    // RoleQueryService.NameExistsAsync viết lại bằng .Equals(...) thay vì == — NormalizedName
    // không nằm trong danh sách tên thành viên bị cấm ("Role"/"RoleName" — khớp ĐÚNG, không phải
    // chứa) nên KHÔNG bị bắt.
    [Fact]
    public void Detector_S2_Ignores_EqualsCall_OnNormalizedName()
    {
        const string source = """
            namespace CoreAndSkill.Core.Infrastructure.Fake;

            public static class FakeNameUniquenessCheck
            {
                public static bool IsTaken(FakeRole r, string normalized) => r.NormalizedName.Equals(normalized);
            }

            public sealed class FakeRole
            {
                public string NormalizedName { get; set; } = "";
            }
            """;

        var violations = RoleNameComparisonScanner.Scan(source, "fake.cs");

        violations.ShouldBeEmpty();
    }

    // Detector_S2_Ignores_ContainsCall_OnUnrelatedCollection — đúng hình dạng thật của
    // PermissionMatrixService.cs (`existingRoleIdSet.Contains(roleId)`): receiver và đối số đều
    // không tham chiếu thành viên tên "Role"/"RoleName" — KHÔNG bị bắt.
    [Fact]
    public void Detector_S2_Ignores_ContainsCall_OnUnrelatedCollection()
    {
        const string source = """
            namespace CoreAndSkill.Core.Infrastructure.Fake;

            public static class FakeRoleIdCheck
            {
                public static bool IsExisting(System.Collections.Generic.HashSet<System.Guid> existingRoleIdSet, System.Guid roleId)
                    => existingRoleIdSet.Contains(roleId);
            }
            """;

        var violations = RoleNameComparisonScanner.Scan(source, "fake.cs");

        violations.ShouldBeEmpty();
    }

    private static IReadOnlyList<string> ScanCoreSourceFiles(Func<string, string, IReadOnlyList<string>> scan)
    {
        var offenders = new List<string>();

        foreach (var file in EnumerateCoreSourceFiles())
            offenders.AddRange(scan(File.ReadAllText(file), file));

        return offenders;
    }

    // Liệt kê mọi file .cs sản phẩm dưới src/BE/Core (5 project Core.* — docs/kien-truc-core-module.md),
    // loại bin/obj. Cùng khuôn với CoreAndSkill.ArchTests.Support.SolutionScanner.
    private static IReadOnlyList<string> EnumerateCoreSourceFiles([CallerFilePath] string here = "")
    {
        var root = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", "..", "Core"));
        var separator = Path.DirectorySeparatorChar;

        return Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{separator}bin{separator}", StringComparison.OrdinalIgnoreCase)
                     && !p.Contains($"{separator}obj{separator}", StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToList();
    }
}
