using System.ComponentModel.DataAnnotations;

namespace CoreAndSkill.Core.Application.Configuration;

// POCO thuần, không package hạ tầng — docs/quy-uoc/be-architecture.md §4.1.
// Chuỗi kết nối của ứng dụng nằm ở khoá "ConnectionStrings:Core"; thiếu hoặc rỗng ⇒ không khởi động.
public sealed class CoreConnectionOptions
{
    public const string SectionName = "ConnectionStrings";

    [Required(AllowEmptyStrings = false)]
    public string Core { get; init; } = default!;
}
