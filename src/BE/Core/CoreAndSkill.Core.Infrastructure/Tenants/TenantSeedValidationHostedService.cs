using CoreAndSkill.Core.Application.Permissions;
using CoreAndSkill.Core.Application.Tenants;
using Microsoft.Extensions.Hosting;

namespace CoreAndSkill.Core.Infrastructure.Tenants;

// Kiểm nguồn seed ĐÃ GỘP lúc khởi động — docs/quy-uoc/be-architecture.md §1.1 "Nguồn seed cho đơn vị mới": nguồn seed
// sai ⇒ tiến trình KHÔNG khởi động (ném từ StartAsync — host dừng, không mở cổng). Cùng khuôn
// PermissionCatalogValidationHostedService. Thiếu lớp này, lỗi của một nguồn seed chỉ lộ ra ở lần tạo đơn vị đầu tiên — ở
// người vận hành, không ở lúc triển khai.
//
// Phép kiểm nằm ở TenantSeedValidator, không ở đây. Lớp này KHÔNG phải cổng duy nhất đứng trước đường ghi: lệnh
// `core bootstrap` không khởi động hosted service nào, nên TenantProvisioningService tự gọi cùng phép kiểm trước dòng ghi
// đầu tiên (xem chú thích đầu TenantSeedValidator).
//
// Được canh bằng test seam động TenantSeedValidationHostedService_IsRegistered_InTheContainer.
internal sealed class TenantSeedValidationHostedService(
    IEnumerable<ITenantSeedSource> seedSources, IEnumerable<IPermissionCatalogSource> catalogSources)
    : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            TenantSeedValidator.Validate(seedSources, catalogSources);
        }
        catch (TenantSeedException ex)
        {
            // Ở lúc khởi động, nguồn seed sai là lỗi TRIỂN KHAI — ném InvalidOperationException để host dừng, cùng loại
            // PermissionCatalogValidationHostedService ném. TenantSeedException cố ý không kế thừa loại đó.
            throw new InvalidOperationException(ex.Message, ex);
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
