namespace CoreAndSkill.Core.Infrastructure.Persistence;

// Tên schema `core` — docs/database/schema-core.md §1.
public static class CoreSchema
{
    public const string Name = "core";

    // Collation ICU tiếng Việt do migration Core tạo, và extension bỏ dấu cho `searchText` — ADR-0119,
    // docs/database/schema-core.md §3.9. Cột chữ người dùng nhập bằng tiếng Việt mà API cho sắp xếp theo
    // phải mang ViVnCollation (luật E25, docs/RULES-BE.md). Extension nằm ở schema `public` (có sẵn trong search_path).
    public const string ViVnCollation = "vi_vn";
    public const string QualifiedViVnCollation = Name + "." + ViVnCollation;
    public const string ViVnLocale = "vi-VN";
    public const string UnaccentExtension = "unaccent";
}
