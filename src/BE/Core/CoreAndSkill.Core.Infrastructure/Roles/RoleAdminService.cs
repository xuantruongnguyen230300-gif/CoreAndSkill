using CoreAndSkill.Core.Application.Common;
using CoreAndSkill.Core.Application.Roles;
using CoreAndSkill.Core.Domain.Common;
using CoreAndSkill.Core.Infrastructure.Identity;
using CoreAndSkill.Core.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CoreAndSkill.Core.Infrastructure.Roles;

// Hiện thực IRoleAdminService — docs/contracts/roles.md §2, §3, §4.
internal sealed class RoleAdminService(RoleManager<AppRole> roleManager, CoreDbContext db) : IRoleAdminService
{
    public async Task<Result<Guid>> CreateAsync(string name, CancellationToken ct)
    {
        if (await roleManager.RoleExistsAsync(name))
            return Result.Failure<Guid>(RoleErrors.NameDuplicate.WithParams(("Name", name)));

        var role = AppRole.NewRole(name);
        var identityResult = await roleManager.CreateAsync(role);
        if (!identityResult.Succeeded)
            throw new InvalidOperationException(
                "CreateAsync thất bại ngoài dự kiến — dữ liệu đã qua validator trước khi tới đây: " +
                string.Join(", ", identityResult.Errors.Select(e => e.Code)));

        return Result.Success(role.Id);
    }

    public async Task<Result> RenameAsync(Guid roleId, string name, string? version, CancellationToken ct)
    {
        var role = await roleManager.FindByIdAsync(roleId.ToString());
        if (role is null)
            return Result.Failure(RoleErrors.NotFound);

        if (role.IsSystem)
            return Result.Failure(RoleErrors.SystemImmutable);

        var existing = await roleManager.FindByNameAsync(name);
        if (existing is not null && existing.Id != roleId)
            return Result.Failure(RoleErrors.NameDuplicate.WithParams(("Name", name)));

        // Token đồng thời — 06-concurrency-control.md §6.3. Hai phép so nối nhau thành "version == stamp trong DB lúc ghi":
        //  1. version client giữ phải bằng stamp vừa đọc trong transaction này (null không bao giờ khớp).
        //  2. câu UPDATE của RoleStore mang WHERE concurrency_stamp = <stamp vừa đọc> — ai ghi chen giữa ⇒ 0 dòng ⇒
        //     ConcurrencyFailure.
        // KHÔNG đặt OriginalValue = version thay cho bước 1: RoleStore.UpdateAsync gọi Context.Attach(role) trước khi lưu,
        // và Attach đưa bản ghi về Unchanged — OriginalValue bị ghi đè bằng giá trị hiện tại, phép so biến mất
        // (RoleRenameConcurrencyTests chứng minh cả hai nhánh).
        if (version is null || !string.Equals(version, role.ConcurrencyStamp, StringComparison.Ordinal))
            return Result.Failure(CommonErrors.ConcurrencyConflict);

        role.Name = name;
        var identityResult = await roleManager.UpdateAsync(role);
        if (!identityResult.Succeeded)
        {
            var conflict = IdentityConcurrency.DetectConflict(identityResult);
            if (conflict is not null)
                return Result.Failure(conflict);

            throw new InvalidOperationException(
                "RenameAsync thất bại ngoài dự kiến: " + string.Join(", ", identityResult.Errors.Select(e => e.Code)));
        }

        return Result.Success();
    }

    // Khoá dòng vai trò TRƯỚC khi đếm người mang, đếm TRƯỚC khi xoá — RoleRowLocks giải thích cặp khoá với đường gán vai
    // trò (nợ E17). Đếm chạy sau khoá nên thấy mọi lượt gán đã commit; lượt gán còn đang chạy thì câu khoá chờ nó xong.
    public async Task<Result> DeleteAsync(Guid roleId, CancellationToken ct)
    {
        var locked = await RoleRowLocks.LockForDeleteAsync(db, roleId, ct);
        if (locked is null)
            return Result.Failure(RoleErrors.NotFound);

        if (locked.IsSystem)
            return Result.Failure(RoleErrors.SystemImmutable);

        var userCount = await db.UserRoles.CountAsync(ur => ur.RoleId == roleId, ct);
        if (userCount > 0)
            return Result.Failure(RoleErrors.InUse.WithParams(("Count", userCount)));

        // Đọc bản ghi đầy đủ SAU khoá: concurrency_stamp mà câu DELETE của RoleStore đặt vào WHERE là stamp mới nhất — không
        // lượt đổi tên nào chen được vào giữa đây và câu DELETE. Đang giữ khoá dòng mà không đọc lại được là lỗi lập trình.
        var role = await roleManager.FindByIdAsync(roleId.ToString())
            ?? throw new InvalidOperationException($"Vai trò {roleId} đang bị khoá để xoá nhưng không đọc lại được.");

        var identityResult = await roleManager.DeleteAsync(role);
        if (!identityResult.Succeeded)
            throw new InvalidOperationException(
                "DeleteAsync thất bại ngoài dự kiến: " + string.Join(", ", identityResult.Errors.Select(e => e.Code)));

        return Result.Success();
    }
}
