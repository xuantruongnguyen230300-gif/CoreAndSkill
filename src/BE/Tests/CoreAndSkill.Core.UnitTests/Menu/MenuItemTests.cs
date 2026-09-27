using CoreAndSkill.Core.Domain.Menu;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.UnitTests.Menu;

public class MenuItemTests
{
    [Fact]
    public void Create_TrimsCodeAndLabelKey_SetsFields()
    {
        var parentId = Guid.NewGuid();
        var permissionId = Guid.NewGuid();

        var result = MenuItem.Create(
            "  users  ", "  menu.users  ", "icon-user", "/users", parentId, 3, permissionId, "core");

        result.IsSuccess.ShouldBeTrue();
        result.Value.Code.ShouldBe("users");
        result.Value.LabelKey.ShouldBe("menu.users");
        result.Value.Icon.ShouldBe("icon-user");
        result.Value.Route.ShouldBe("/users");
        result.Value.ParentId.ShouldBe(parentId);
        result.Value.DisplayOrder.ShouldBe(3);
        result.Value.RequiredPermissionId.ShouldBe(permissionId);
        result.Value.ModuleKey.ShouldBe("core");
    }

    [Fact]
    public void Create_TopLevelItem_HasNullParentAndPermission()
    {
        var result = MenuItem.Create("root", "menu.root", null, null, null, 0, null, null);

        result.Value.ParentId.ShouldBeNull();
        result.Value.RequiredPermissionId.ShouldBeNull();
        result.Value.Icon.ShouldBeNull();
        result.Value.Route.ShouldBeNull();
        result.Value.ModuleKey.ShouldBeNull();
    }
}
