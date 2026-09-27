using CoreAndSkill.Core.Domain.Permissions;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.UnitTests.Permissions;

public class RolePermissionTests
{
    [Fact]
    public void Create_SetsRoleAndPermissionIds()
    {
        var roleId = Guid.NewGuid();
        var permissionId = Guid.NewGuid();

        var result = RolePermission.Create(roleId, permissionId);

        result.IsSuccess.ShouldBeTrue();
        result.Value.RoleId.ShouldBe(roleId);
        result.Value.PermissionId.ShouldBe(permissionId);
        result.Value.Id.ShouldNotBe(Guid.Empty);
    }
}
