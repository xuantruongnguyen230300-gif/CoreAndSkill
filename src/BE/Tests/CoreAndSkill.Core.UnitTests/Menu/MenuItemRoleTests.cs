using CoreAndSkill.Core.Domain.Menu;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.UnitTests.Menu;

public class MenuItemRoleTests
{
    [Fact]
    public void Create_SetsMenuItemAndRoleIds()
    {
        var menuItemId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        var result = MenuItemRole.Create(menuItemId, roleId);

        result.IsSuccess.ShouldBeTrue();
        result.Value.MenuItemId.ShouldBe(menuItemId);
        result.Value.RoleId.ShouldBe(roleId);
    }
}
