using System.Reflection;
using CoreAndSkill.ArchTests.Support;
using Shouldly;
using Xunit;

namespace CoreAndSkill.ArchTests;

// Luật B4 — docs/RULES.md §10, docs/adr/0026-ranh-gioi-identity-va-cookie.md: `Core.Infrastructure` KHÔNG phụ thuộc
// `HttpContext`. Luật A2 canh chiều Application → Infrastructure; chiều này không luật nào canh, và đã lệch thật một lần
// (một tài liệu đặt `HttpContextCurrentUser` vào Infrastructure). Ranh giới có hai nửa: Identity lõi, store, UserManager
// ở Infrastructure — cookie scheme, SignIn/SignOut, mọi thứ đọc `HttpContext` ở Core.Web.
//
// Đo trên ASSEMBLY ĐÃ BUILD, không trên .csproj: `Microsoft.AspNetCore.Http.Abstractions` đi vào bằng phụ thuộc bắc cầu
// của gói khác (không có dòng PackageReference nào để đọc), và trình biên dịch chỉ ghi tham chiếu khi IL thật sự dùng tới.
// Vì vậy "sạch" ở đây nghĩa là: không kiểu nào của Infrastructure chạm tới kiểu của các assembly đó.
//
// ĐO NGÀY 2026-09-23: hôm nay một tệp trong Core.Infrastructure dùng `IHttpContextAccessor` còn KHÔNG BIÊN DỊCH ĐƯỢC —
// các gói đang tham chiếu chưa kéo `Microsoft.AspNetCore.Http.Abstractions` vào tầm biên dịch, nên lớp chặn thứ nhất là
// chính trình biên dịch. Cổng này là lớp thứ hai, và nó là lớp DUY NHẤT còn lại ngay khi có người thêm một gói ASP.NET
// Core (hay `<FrameworkReference Include="Microsoft.AspNetCore.App" />`) vào project — lúc đó lỗi biên dịch biến mất mà
// ranh giới thì không. Đừng đọc "hôm nay xanh" thành "cổng này thừa".
public class HttpContextBoundaryTests
{
    // Ba assembly mang HttpContext và phần lõi của nó. Bắt theo TIỀN TỐ để một assembly mới cùng họ không lọt.
    private const string HttpAssemblyPrefix = "Microsoft.AspNetCore.Http";

    [Fact]
    public void Core_Infrastructure_MustNotReference_HttpAbstractions()
    {
        var offenders = HttpReferences(ArchitectureFixture.InfrastructureAssembly);

        offenders.ShouldBeEmpty(
            "Core.Infrastructure chạm HttpContext — ranh giới Identity lõi ↔ cookie ở ADR-0026 đã vỡ; seam của request "
            + "(ICurrentUser, ITenantContext, IClientAddressAccessor) là đường DUY NHẤT dữ liệu của HttpContext đi vào");
    }

    // T6 — đối chứng dương cho phép đo: chính Core.Web PHẢI tham chiếu nhóm assembly này. Nếu nó cũng "sạch" thì phép đo
    // đang nhìn nhầm chỗ (sai tên, sai assembly, GetReferencedAssemblies trả rỗng) và cổng trên xanh vì mù.
    [Fact]
    public void Core_Infrastructure_MustNotReference_HttpAbstractions_MeasuresSomethingReal()
    {
        HttpReferences(ArchitectureFixture.WebAssembly).ShouldNotBeEmpty();
    }

    // Detector: một assembly tổng hợp CÓ dùng HttpContext phải bị bắt — mẫu vi phạm đúng hình dạng thật (một seam của
    // request cài đặt sai chỗ, đọc HttpContext ngay trong Infrastructure).
    [Fact]
    public void Detector_B4_Catches_AnAssemblyThatTouchesHttpContext()
    {
        var httpAbstractions = typeof(Microsoft.AspNetCore.Http.HttpContext).Assembly;
        var offender = SyntheticAssembly.Compile(
            "FakeInfrastructureTouchingHttp",
            """
            using Microsoft.AspNetCore.Http;

            namespace Fake;

            public sealed class FakeCurrentUser(IHttpContextAccessor accessor)
            {
                public string? UserName => accessor.HttpContext?.User.Identity?.Name;
            }
            """,
            httpAbstractions,
            typeof(System.Security.Claims.ClaimsPrincipal).Assembly,
            typeof(System.Security.Principal.IIdentity).Assembly);

        HttpReferences(offender).ShouldNotBeEmpty();
    }

    [Fact]
    public void Detector_B4_Ignores_AnAssemblyWithoutHttp()
    {
        var clean = SyntheticAssembly.Compile(
            "FakeInfrastructureWithoutHttp",
            """
            namespace Fake;

            public sealed class FakeClock
            {
                public System.DateTimeOffset Now => System.DateTimeOffset.UtcNow;
            }
            """);

        HttpReferences(clean).ShouldBeEmpty();
    }

    private static IReadOnlyList<string> HttpReferences(Assembly assembly)
        => [.. assembly.GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .Where(name => name.StartsWith(HttpAssemblyPrefix, StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)];
}
