using CoreAndSkill.ArchTests.Support;
using CoreAndSkill.Core.Web.Permissions;
using CoreAndSkill.Core.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shouldly;
using Xunit;

namespace CoreAndSkill.ArchTests;

// S11 — docs/RULES.md S11, docs/quy-uoc/be-api-controller.md §4.2,
// docs/adr/0024-ba-muc-khai-bao-phan-quyen-endpoint.md. Mục 3 của nghiệm thu B2: "Endpoint mới
// quên khai quyền → bị từ chối, không mở mặc định" — cổng này CHÍNH LÀ cơ chế từ chối đó: một
// action không khai đúng một mức không được phép tồn tại trong solution đã build.
public class PermissionDeclarationTests
{
    [Fact]
    public void EveryAction_DeclaresExactlyOneAuthorizationLevel()
    {
        var offenders = AuthorizationLevelScanner.FindActionsWithoutExactlyOneLevel(
            ArchitectureFixture.WebAssembly, AnonymousEndpointAllowlist.Endpoints);

        offenders.ShouldBeEmpty();
    }

    // T6 (phía BE) — một bộ dò xét tập rỗng luôn PASS; test này chứng minh scanner thật sự quét
    // được ít nhất một action có thật trong CoreAndSkill.Core.Web.
    [Fact]
    public void EveryAction_DeclaresExactlyOneAuthorizationLevel_ScansAtLeastOneRealController()
    {
        var anyController = ArchitectureFixture.WebAssembly.GetTypes()
            .Any(t => !t.IsAbstract && typeof(ControllerBase).IsAssignableFrom(t));

        anyController.ShouldBeTrue();
    }

    // Detector_S11_Catches_MissingDeclaration — action không mang bất kỳ mức nào (kể cả không
    // [AllowAnonymous]) là ca "quên khai quyền" mà mục 3 của nghiệm thu B2 đòi phải bắt được.
    [Fact]
    public void Detector_S11_Catches_MissingDeclaration()
    {
        var offenders = AuthorizationLevelScanner.FindActionsWithoutExactlyOneLevel(
            typeof(NoLevelController).Assembly, new HashSet<string>(StringComparer.Ordinal));

        offenders.ShouldContain($"{typeof(NoLevelController).FullName}.{nameof(NoLevelController.Get)}");
    }

    // Detector_S11_Catches_TwoLevelsOnSameAction — RequirePermission cấp controller CỘNG
    // AuthenticatedOnly cấp action là hai mức khác nhau, không phải một.
    [Fact]
    public void Detector_S11_Catches_TwoDifferentLevelsMixed()
    {
        var offenders = AuthorizationLevelScanner.FindActionsWithoutExactlyOneLevel(
            typeof(MixedLevelController).Assembly, new HashSet<string>(StringComparer.Ordinal));

        offenders.ShouldContain($"{typeof(MixedLevelController).FullName}.{nameof(MixedLevelController.Get)}");
    }

    // Detector_S11_Ignores_MultipleKeysOnSameLevel — nhiều [RequirePermission] vẫn là MỘT mức.
    [Fact]
    public void Detector_S11_Ignores_MultipleKeysOnSameLevel()
    {
        var offenders = AuthorizationLevelScanner.FindActionsWithoutExactlyOneLevel(
            typeof(MultiplePermissionKeysController).Assembly, new HashSet<string>(StringComparer.Ordinal));

        offenders.ShouldNotContain($"{typeof(MultiplePermissionKeysController).FullName}.{nameof(MultiplePermissionKeysController.Get)}");
    }

    // Detector_S11_Ignores_ControllerLevelDeclaration — mức khai ở CẤP CONTROLLER áp cho action
    // không tự khai gì.
    [Fact]
    public void Detector_S11_Ignores_ControllerLevelDeclaration()
    {
        var offenders = AuthorizationLevelScanner.FindActionsWithoutExactlyOneLevel(
            typeof(ControllerLevelController).Assembly, new HashSet<string>(StringComparer.Ordinal));

        offenders.ShouldNotContain($"{typeof(ControllerLevelController).FullName}.{nameof(ControllerLevelController.Get)}");
    }

    // Detector_S11_Ignores_AllowAnonymousOnAllowlist — [AllowAnonymous] thắng KHI có tên trong allowlist.
    [Fact]
    public void Detector_S11_Ignores_AllowAnonymousOnAllowlist()
    {
        var allowlist = new HashSet<string>(StringComparer.Ordinal) { "GET /api/v1/core/fake/anon-on-allowlist" };

        var offenders = AuthorizationLevelScanner.FindActionsWithoutExactlyOneLevel(
            typeof(AnonymousOnAllowlistController).Assembly, allowlist);

        offenders.ShouldNotContain($"{typeof(AnonymousOnAllowlistController).FullName}.{nameof(AnonymousOnAllowlistController.Get)}");
    }

    // Detector_S11_Catches_AllowAnonymousNotOnAllowlist — [AllowAnonymous] KHÔNG có tên trong
    // allowlist không được miễn — vẫn phải qua kiểm "đúng một mức" (ở đây là 0 mức ⇒ vi phạm).
    [Fact]
    public void Detector_S11_Catches_AllowAnonymousNotOnAllowlist()
    {
        var offenders = AuthorizationLevelScanner.FindActionsWithoutExactlyOneLevel(
            typeof(AnonymousOnAllowlistController).Assembly, new HashSet<string>(StringComparer.Ordinal));

        offenders.ShouldContain($"{typeof(AnonymousOnAllowlistController).FullName}.{nameof(AnonymousOnAllowlistController.Get)}");
    }

    [ApiController]
    [Route("api/v1/core/fake")]
    private sealed class NoLevelController : ControllerBase
    {
        [HttpGet("no-level")]
        public IActionResult Get() => Ok();
    }

    [ApiController]
    [Route("api/v1/core/fake")]
    [RequirePermission("core.fake.read")]
    private sealed class MixedLevelController : ControllerBase
    {
        [HttpGet("mixed-level")]
        [AuthenticatedOnly("chỉ để test")]
        public IActionResult Get() => Ok();
    }

    [ApiController]
    [Route("api/v1/core/fake")]
    private sealed class MultiplePermissionKeysController : ControllerBase
    {
        [HttpGet("multi-key")]
        [RequirePermission("core.fake.read")]
        [RequirePermission("core.fake.write")]
        public IActionResult Get() => Ok();
    }

    [ApiController]
    [Route("api/v1/core/fake")]
    [RequirePermission("core.fake.read")]
    private sealed class ControllerLevelController : ControllerBase
    {
        [HttpGet("controller-level")]
        public IActionResult Get() => Ok();
    }

    [ApiController]
    [Route("api/v1/core/fake")]
    private sealed class AnonymousOnAllowlistController : ControllerBase
    {
        [HttpGet("anon-on-allowlist")]
        [AllowAnonymous]
        public IActionResult Get() => Ok();
    }
}
