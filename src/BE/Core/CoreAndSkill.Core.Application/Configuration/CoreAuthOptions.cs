using System.ComponentModel.DataAnnotations;

namespace CoreAndSkill.Core.Application.Configuration;

// POCO thuần, không package hạ tầng — định nghĩa gốc, docs/quy-uoc/be-architecture.md §4.1.
// Khoá cấu hình: Core:Auth. CookieName/SessionMinutes/SessionAbsoluteHours dùng ở B1 (cookie phiên);
// AntiforgeryCookieName và AllowedOrigins thuộc cơ chế CSRF/CORS của B2
// (docs/wiki-core/be/trien-khai/03-b2-phan-quyen-va-bien.md) — khai đủ ở đây để giữ ĐÚNG MỘT
// định nghĩa của khối Options này (không tách thành hai bản), dù middleware CORS/antiforgery
// chưa nối vào pipeline ở B1.
public sealed class CoreAuthOptions
{
    public const string SectionName = "Core:Auth";

    [Required(AllowEmptyStrings = false)]
    public string CookieName { get; init; } = default!; // cookie phiên — be-api-controller.md §7.4

    [Required(AllowEmptyStrings = false)]
    public string AntiforgeryCookieName { get; init; } = default!; // cookie nội bộ antiforgery — §7.5; hai tên không được trùng

    [Range(1, 60 * 24)]
    public int SessionMinutes { get; init; }

    [Range(1, 24 * 7)]
    public int SessionAbsoluteHours { get; init; } = 12; // khoá Core:Auth:SessionAbsoluteHours — be-api-controller.md §7.4

    [Required, MinLength(1)]
    public string[] AllowedOrigins { get; init; } = [];
}
