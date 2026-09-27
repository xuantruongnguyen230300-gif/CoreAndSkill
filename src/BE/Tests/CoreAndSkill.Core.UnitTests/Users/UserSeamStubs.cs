using CoreAndSkill.Core.Application.Permissions;
using CoreAndSkill.Core.Application.Roles;
using NSubstitute;

namespace CoreAndSkill.Core.UnitTests.Users;

// Giả lập hai seam tra theo TẬP như bản thật của Core.Infrastructure: mọi id được hỏi đều có mặt trong kết quả, vai trò
// không khai quyền nhận tập rỗng.
internal static class UserSeamStubs
{
    public static void StubRolePermissions(this IPermissionChecker checker, IReadOnlyDictionary<Guid, string[]> grants)
        => checker.GetPermissionsForRolesAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(call => (IReadOnlyDictionary<Guid, IReadOnlySet<string>>)call.ArgAt<IReadOnlyCollection<Guid>>(0)
                .Distinct()
                .ToDictionary(
                    id => id,
                    IReadOnlySet<string> (id) => grants.TryGetValue(id, out var codes) ? codes.ToHashSet() : new HashSet<string>()));

    public static void StubRolesExist(this IRoleQueryService roles, Func<Guid, bool> exists)
        => roles.FindExistingIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(call => (IReadOnlySet<Guid>)call.ArgAt<IReadOnlyCollection<Guid>>(0).Where(exists).ToHashSet());
}

// Đếm lời gọi seam theo TÊN phương thức — không trói test vào một chữ ký cụ thể của seam.
internal static class SeamCallCounter
{
    public static int CallerPermissionLookups(IPermissionChecker checker)
        => checker.ReceivedCalls().Count(call => call.GetMethodInfo().Name is
            nameof(IPermissionChecker.HasPermissionAsync) or nameof(IPermissionChecker.GetEffectivePermissionsAsync));
}
