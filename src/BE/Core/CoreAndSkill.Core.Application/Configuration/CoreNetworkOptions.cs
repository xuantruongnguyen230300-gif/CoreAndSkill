using System.ComponentModel.DataAnnotations;
using System.Net;

namespace CoreAndSkill.Core.Application.Configuration;

// Proxy tin cậy đứng trước API — docs/quy-uoc/be-architecture.md §3.1 ràng buộc 4; khoá và cách khai:
// docs/wiki-core/be/18-trien-khai-va-van-hanh.md §3. Khoá `Core:Network:*`.
//
// Cả hai danh sách được phép RỖNG — rỗng nghĩa là KHÔNG tin proxy nào, trạng thái đúng khi app nhận kết nối
// trực tiếp. Không có mục nào "mặc định": danh sách loopback sẵn có của framework bị xoá trước khi nạp các mục
// dưới đây (Core.Web, AddCore). Mục viết sai thì không khởi động — một proxy khai hỏng mà bị bỏ qua im lặng là
// app sau proxy tưởng mọi request là HTTP.
public sealed class CoreNetworkOptions : IValidatableObject
{
    public const string SectionName = "Core:Network";

    // Địa chỉ IP của từng proxy — ví dụ "10.0.0.5".
    public string[] KnownProxies { get; init; } = [];

    // Dải mạng CIDR của proxy — ví dụ "10.0.0.0/24".
    public string[] KnownNetworks { get; init; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var entry in KnownProxies.Where(e => !IPAddress.TryParse(e, out _)))
        {
            yield return new ValidationResult(
                $"{SectionName}:{nameof(KnownProxies)} có mục không phải địa chỉ IP: '{entry}'.",
                [nameof(KnownProxies)]);
        }

        foreach (var entry in KnownNetworks.Where(e => !IPNetwork.TryParse(e, out _)))
        {
            yield return new ValidationResult(
                $"{SectionName}:{nameof(KnownNetworks)} có mục không phải dải CIDR (dạng 10.0.0.0/24): '{entry}'.",
                [nameof(KnownNetworks)]);
        }
    }
}
