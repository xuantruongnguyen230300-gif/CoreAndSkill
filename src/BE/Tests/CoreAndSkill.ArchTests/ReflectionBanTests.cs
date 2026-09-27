using System.Runtime.CompilerServices;
using CoreAndSkill.ArchTests.Support;
using Shouldly;
using Xunit;

namespace CoreAndSkill.ArchTests;

// R5 — docs/RULES.md R5. Lý do: docs/audit/2026-09-05-reflection-envelope.md.
public class ReflectionBanTests
{
    [Fact]
    public void ResultToHttpMapper_MustNotUse_Reflection()
    {
        var source = File.ReadAllText(ResultToHttpMapperPath());

        var violations = ReflectionUsageDetector.Scan(source);

        violations.ShouldBeEmpty();
    }

    [Fact]
    public void Detector_R5_Catches_RealViolation()
    {
        const string source = """
            using System;
            public static class Bad
            {
                public static object Map(Type t)
                {
                    var method = t.GetMethod("FromError");
                    return method!.Invoke(null, null)!;
                }
            }
            """;

        var violations = ReflectionUsageDetector.Scan(source);

        violations.ShouldNotBeEmpty();
    }

    [Fact]
    public void Detector_R5_Ignores_CommentAndIdentifierNamedLikeBannedMember()
    {
        const string source = """
            // KHÔNG dùng GetMethod/Invoke ở đây — xem docs/RULES.md R5
            public static class Good
            {
                // Một class/method trùng TÊN với thành viên cấm, nhưng không phải lời gọi reflection.
                public static void Invoke() { }
                public static void GetMethod() { }
            }
            """;

        var violations = ReflectionUsageDetector.Scan(source);

        violations.ShouldBeEmpty();
    }

    private static string ResultToHttpMapperPath([CallerFilePath] string here = "")
        => Path.GetFullPath(Path.Combine(
            Path.GetDirectoryName(here)!, "..", "..", "Core",
            "CoreAndSkill.Core.Web", "Http", "ResultToHttpMapper.cs"));
}
