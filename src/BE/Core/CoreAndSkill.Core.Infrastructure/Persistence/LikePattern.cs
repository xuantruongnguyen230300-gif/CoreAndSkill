namespace CoreAndSkill.Core.Infrastructure.Persistence;

// Mẫu ILIKE cho `searchText` — "khớp một phần" theo NGHĨA ĐEN của chuỗi người dùng gõ (docs/contracts/users.md,
// roles.md, tenants.md, tham số `searchText`). `%` và `_` là ký tự đại diện của LIKE nên phải thoát, cùng chính ký tự
// thoát. Dùng kèm EscapeCharacter: EF.Functions.ILike(cột, LikePattern.Contains(text), LikePattern.EscapeCharacter).
internal static class LikePattern
{
    public const string EscapeCharacter = @"\";

    public static string Contains(string text) => $"%{Escape(text)}%";

    // Ký tự thoát PHẢI thay trước — thay sau thì nó nhân đôi chính dấu thoát vừa chèn cho % và _.
    private static string Escape(string text)
        => text
            .Replace(@"\", @"\\", StringComparison.Ordinal)
            .Replace("%", @"\%", StringComparison.Ordinal)
            .Replace("_", @"\_", StringComparison.Ordinal);
}
