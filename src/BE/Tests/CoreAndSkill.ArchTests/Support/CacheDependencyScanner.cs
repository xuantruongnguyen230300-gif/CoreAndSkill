using ArchUnitNET.Domain;

namespace CoreAndSkill.ArchTests.Support;

// A22 — docs/RULES-BE.md hàng A22, docs/quy-uoc/be-performance.md §8.2: Core.Application không chạm thẳng
// HybridCache / IMemoryCache / IDistributedCache; cache chỉ đi qua ICacheStore.
//
// Quét đồ thị dependency của ArchUnitNET theo TÊN ĐẦY ĐỦ của đích (không theo tên assembly: IMemoryCache và
// IDistributedCache cùng nằm trong một assembly Abstractions với nhiều kiểu khác, HybridCache ở gói riêng) — cùng
// khuôn BoundaryRules, MỘT hàm cho rule thật lẫn mọi Detector_A22_*. Duyệt architecture.Types, KHÔNG chỉ Classes:
// một interface hay struct của Application có tham số IMemoryCache cũng là chạm thẳng.
//
// ĐIỂM MÙ: chỉ thấy phụ thuộc mà trình biên dịch ghi vào metadata (trường, tham số, kiểu trả về, thân hàm, kế thừa,
// đối số generic). Không thấy: kiểu cache khác ngoài ba kiểu trong luật (ví dụ MemoryCache cụ thể, IDistributedCache
// bọc qua một kiểu trung gian của Infrastructure — kiểu đó bị A2 canh), phụ thuộc qua phản chiếu (bị cấm riêng).
// Vế "đăng ký no-op khi chưa có bản cài" do review canh, không phải cổng này.
internal static class CacheDependencyScanner
{
    public static readonly IReadOnlyList<string> ForbiddenTypeFullNames =
    [
        "Microsoft.Extensions.Caching.Hybrid.HybridCache",
        "Microsoft.Extensions.Caching.Memory.IMemoryCache",
        "Microsoft.Extensions.Caching.Distributed.IDistributedCache",
    ];

    public static IReadOnlyList<string> FindTypesInAssemblyDependingOnCacheLibraries(
        Architecture architecture,
        string sourceAssemblyName)
    {
        var offenders = new List<string>();

        foreach (var type in architecture.Types)
        {
            if (type.Assembly.Name != sourceAssemblyName)
                continue;

            var dependsOnForbidden = type.Dependencies.Any(dependency =>
                ForbiddenTypeFullNames.Contains(dependency.Target.FullName, StringComparer.Ordinal));

            if (dependsOnForbidden)
                offenders.Add(type.FullName);
        }

        return offenders;
    }
}
