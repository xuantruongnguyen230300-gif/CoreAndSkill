using CoreAndSkill.Core.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CoreAndSkill.Core.Infrastructure.Identity;

// Kho người dùng của Core — thay kho mặc định của Identity (đăng ký ở AddCoreIdentity).
// docs/adr/0089-trung-unique-khi-ghi-dong-thoi-dich-o-kho-identity.md
//
// Vì sao ở đây: hai lượt tạo (hoặc sửa) cùng tên đăng nhập / cùng email chạy đồng thời thì cả hai lọt phép kiểm trước và bộ
// kiểm của Identity (READ COMMITTED — không lượt nào thấy dòng chưa commit của lượt kia); lượt sau vỡ index duy nhất ở lệnh
// lưu. Kho mặc định chỉ bắt DbUpdateConcurrencyException, nên 23505 đi xuyên UserManager thành 500. Mọi đường ghi tài khoản
// đều qua UserManager → kho này (UserAdminService, TenantProvisioningService, UserProfileService, IdentityService), nên
// dịch ở đây phủ mọi chỗ gọi, kể cả chỗ viết sau này — cùng khuôn IdentityConcurrency.
//
// Dịch thành ĐÚNG lỗi bộ kiểm Identity dựng khi tự thấy bản trùng (ErrorDescriber.DuplicateUserName / DuplicateEmail): từ đó
// IdentityErrorMapper và bộ ánh xạ riêng của từng endpoint chạy như cũ. Ràng buộc ngoài tập AppUserUniqueIndexes, hoặc lỗi
// không phải 23505, KHÔNG bị nuốt — vẫn ném, vẫn 500.
//
// Sau khi dịch: PostgreSQL đã đánh dấu transaction là hỏng, nên chỗ gọi phải trả thất bại NGAY, không chạy thêm lệnh SQL nào
// (TransactionBehavior / runner rollback). Kho gỡ bản ghi vừa hỏng khỏi bộ theo dõi, để không lần lưu nào sau đó thử ghi lại
// nó (ADR-0089 quyết định 4).
//
// Nâng phiên bản Identity: lớp cha đổi cách lưu (gom lệnh lưu ra ngoài CreateAsync/UpdateAsync, tự bọc ngoại lệ) thì lớp này
// mất tác dụng im lặng — chỉ test trên PostgreSQL thật bắt được (AppUserUniqueViolationDatabaseTests, RequiresDocker).
internal sealed class AppUserStore(CoreDbContext context, IdentityErrorDescriber? describer = null)
    : UserStore<AppUser, AppRole, CoreDbContext, Guid, AppUserClaim, AppUserRole, AppUserLogin, AppUserToken, AppRoleClaim>(
        context, describer)
{
    public override async Task<IdentityResult> CreateAsync(AppUser user, CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.CreateAsync(user, cancellationToken);
        }
        catch (DbUpdateException ex) when (AppUserUniqueIndexes.Translate(ex, user, ErrorDescriber) is { } duplicate)
        {
            Forget(user);
            return IdentityResult.Failed(duplicate);
        }
    }

    public override async Task<IdentityResult> UpdateAsync(AppUser user, CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.UpdateAsync(user, cancellationToken);
        }
        catch (DbUpdateException ex) when (AppUserUniqueIndexes.Translate(ex, user, ErrorDescriber) is { } duplicate)
        {
            Forget(user);
            return IdentityResult.Failed(duplicate);
        }
    }

    private void Forget(AppUser user) => Context.Entry(user).State = EntityState.Detached;
}
