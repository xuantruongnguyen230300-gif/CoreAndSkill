using System.ComponentModel.DataAnnotations;

namespace CoreAndSkill.Core.Application.Configuration;

// Chính sách mật khẩu — docs/wiki-core/be/02-identity-auth.md §4.1. Luật S9: khoá này CHỈ được
// khai ở appsettings.json (không theo môi trường) — chính sách giống nhau ở mọi môi trường.
public sealed class CoreIdentityPasswordOptions
{
    public const string SectionName = "Core:Identity:Password";

    [Range(1, 128)]
    public int RequiredLength { get; init; } = 8;

    public bool RequireDigit { get; init; } = true;

    public bool RequireLowercase { get; init; } = true;

    public bool RequireUppercase { get; init; }

    public bool RequireNonAlphanumeric { get; init; }

    [Range(1, 128)]
    public int RequiredUniqueChars { get; init; } = 1;
}
