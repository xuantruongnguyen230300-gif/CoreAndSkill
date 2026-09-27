using CoreAndSkill.Core.Application.Common;
using CoreAndSkill.Core.Domain.Common;
using Microsoft.AspNetCore.Identity;

namespace CoreAndSkill.Core.Infrastructure.Identity;

// Điểm DUY NHẤT phát hiện lỗi ConcurrencyFailure từ IdentityResult — docs/wiki-core/be/06-concurrency-control.md
// §6.1 "Bắt ngoại lệ đồng thời | Ở đúng một chỗ, không rải trong từng handler". Trước khi có file này,
// khối kiểm tra dưới đây bị LẶP Y HỆT ở ba điểm gọi (IdentityService.ChangePasswordAsync,
// UserProfileService.UpdateAsync, UserProfileService.RenouncePermissionBypassAsync) — chính lỗi đã bị
// sửa sai hai lần trước khi đúng (rơi vào exception chung / map nhầm password policy). Mọi thao tác
// ghi AppUser mới qua UserManager PHẢI gọi DetectConflict thay vì tự lặp lại khối kiểm tra.
internal static class IdentityConcurrency
{
    // Mã lỗi Identity thô khi UserStore<TUser>.UpdateAsync bắt DbUpdateConcurrencyException và trả
    // IdentityResult.Failed — KHÔNG BAO GIỜ ném ra ngoài UserManager (đã đối chiếu bằng decompile
    // assembly thật). Đây là ĐƯỜNG DUY NHẤT nhận biết xung đột đồng thời cho thao tác qua UserManager
    // — 06-concurrency-control.md §6.1, §6.3.
    private const string ConcurrencyFailureCode = "ConcurrencyFailure";

    // Gọi TRƯỚC khi ánh xạ identityResult.Errors sang bất kỳ nhánh lỗi nào khác (password policy, lỗi
    // hệ thống, ...) — thứ tự bắt buộc, không đổi. Trả về null nghĩa là identityResult không thất bại
    // vì ConcurrencyFailure; người gọi tự xử lý nhánh còn lại.
    public static Error? DetectConflict(IdentityResult identityResult) =>
        identityResult.Errors.Any(e => e.Code == ConcurrencyFailureCode)
            ? CommonErrors.ConcurrencyConflict
            : null;

    // Token đồng thời của AppUser — 06-concurrency-control.md §6.3. Hai phép so nối nhau thành
    // "version == stamp trong DB lúc ghi":
    //  1. version client giữ phải bằng stamp vừa đọc trong transaction này (null không bao giờ khớp) — hàm này,
    //     gọi TRƯỚC khi đổi bất kỳ trường nào của bản ghi.
    //  2. câu UPDATE của UserStore mang WHERE concurrency_stamp = <stamp vừa đọc> — ai ghi chen giữa ⇒ 0 dòng ⇒
    //     ConcurrencyFailure, bắt qua DetectConflict.
    // KHÔNG đặt OriginalValue = version thay cho bước 1: UserStore.UpdateAsync gọi Context.Attach(user) trước khi lưu,
    // và Attach đưa bản ghi về Unchanged — OriginalValue bị ghi đè bằng stamp vừa đọc, phép so biến mất
    // (UserConcurrencyTokenTests chứng minh cả hai nhánh). Cùng khuôn RoleAdminService.RenameAsync.
    public static bool VersionMatches(AppUser user, string? version) =>
        version is not null && string.Equals(version, user.ConcurrencyStamp, StringComparison.Ordinal);
}
