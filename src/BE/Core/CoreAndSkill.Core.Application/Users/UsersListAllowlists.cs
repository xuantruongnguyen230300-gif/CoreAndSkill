namespace CoreAndSkill.Core.Application.Users;

// Allowlist của bộ lọc danh sách người dùng — docs/contracts/users.md §3. MỘT chỗ cho cả endpoint danh
// sách lẫn endpoint xuất: xuất nhận CÙNG tập tham số lọc và sắp xếp với danh sách (docs/contracts/exports.md
// §1). Hai bên khai allowlist riêng thì sẽ lệch, và người dùng nhận một tệp không khớp thứ họ đang nhìn.
internal static class UsersListAllowlists
{
    public static readonly string[] SortBy = ["userName", "fullName", "email", "createdAt"];

    public static readonly string[] Status = ["active", "locked"];

    public const int SearchTextMaxLength = 200;
}
