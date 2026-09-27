using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CoreAndSkill.Core.Infrastructure.Identity;

// Hai index duy nhất của core.app_user và lỗi Identity tương ứng khi một lượt ghi vỡ chúng —
// docs/adr/0089-trung-unique-khi-ghi-dong-thoi-dich-o-kho-identity.md quyết định 2, 3, 5.
// Chỗ DUY NHẤT khai tên hai index: AppUserConfiguration đặt tên index trong mô hình EF bằng chính hai hằng dưới đây, và
// AppUserStore dịch vi phạm 23505 theo chính tập này.
//
// Nhận ra bằng TÊN RÀNG BUỘC (PostgresException.ConstraintName), không bằng câu thông báo. Tên lấy từ
// database/scripts/core/0001__core__initial.sql — `CREATE UNIQUE INDEX "UserNameIndex"`, `CREATE UNIQUE INDEX "EmailIndex"`,
// có nháy kép nên PostgreSQL giữ nguyên hoa thường; so Ordinal. Test đối chiếu từng tên với mô hình EF và với script
// (AppUserUniqueIndexesTests).
//
// Tập này KHÔNG mang logic theo endpoint (ADR-0089 "Dấu hiệu quyết định này bắt đầu sai"): nó chỉ trả đúng lỗi mà bộ kiểm
// Identity dựng khi tự thấy bản trùng. Khoá fieldErrors, luật gộp mã chống dò (tenants.md §6) vẫn là việc của bộ ánh xạ
// riêng của từng chỗ gọi, chạy như cũ.
internal static class AppUserUniqueIndexes
{
    // Giữ tên Identity tự đặt — docs/database/schema-core.md §4.0. Đổi tên là đổi cả hành vi lỗi (ADR-0089 "Tiêu cực").
    public const string UserName = "UserNameIndex";
    public const string Email = "EmailIndex";

    private static readonly Dictionary<string, Func<IdentityErrorDescriber, AppUser, IdentityError>> Translations =
        new(StringComparer.Ordinal)
        {
            [UserName] = (describer, user) => describer.DuplicateUserName(user.UserName ?? string.Empty),
            [Email] = (describer, user) => describer.DuplicateEmail(user.Email ?? string.Empty),
        };

    public static IReadOnlyCollection<string> ConstraintNames => Translations.Keys;

    // null ⇒ không phải vi phạm unique trên một index của tập — người gọi để ngoại lệ đi tiếp (lỗi lập trình ⇒ 500).
    // EF bọc lỗi của provider làm InnerException trực tiếp của DbUpdateException (cùng cách EfImportRowWriter đọc).
    public static IdentityError? Translate(DbUpdateException exception, AppUser user, IdentityErrorDescriber describer)
        => exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: { } name }
           && Translations.TryGetValue(name, out var describe)
            ? describe(describer, user)
            : null;
}
