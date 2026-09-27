using CoreAndSkill.Core.Application.Identity;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.UnitTests.Identity;

// Tên đăng nhập dành riêng cho danh tính hệ thống — so sau Trim, không phân biệt hoa thường, theo phép chuẩn hoá của Identity
// (SystemActor.cs). Đối chiếu với ILookupNormalizer THẬT của host: IntegrationTests/Identity/SystemActorNormalizationTests.
public class SystemActorTests
{
    // Giá trị ghi vào created_by / updated_by khi không có người — schema-core.md §3.2. Đổi giá trị là đổi dữ liệu đã ghi.
    [Fact]
    public void UserName_IsSystem() => SystemActor.UserName.ShouldBe("system");

    [Theory]
    [InlineData("system")]
    [InlineData("System")]
    [InlineData("SYSTEM")]
    [InlineData("sYsTeM")]
    [InlineData(" system")]
    [InlineData("system ")]
    [InlineData("  SyStEm  ")]
    [InlineData("\tsystem\n")]
    public void IsReservedUserName_TheSystemNameInAnyCaseOrPadding_IsReserved(string userName)
        => SystemActor.IsReservedUserName(userName).ShouldBeTrue();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("system1")]
    [InlineData("system.admin")]
    [InlineData("sys tem")]
    [InlineData("systém")]
    [InlineData("vanhanh")]
    public void IsReservedUserName_AnyOtherName_IsNotReserved(string? userName)
        => SystemActor.IsReservedUserName(userName).ShouldBeFalse();
}
