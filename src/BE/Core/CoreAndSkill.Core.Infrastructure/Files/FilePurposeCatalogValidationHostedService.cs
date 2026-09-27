using CoreAndSkill.Core.Application.Files;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CoreAndSkill.Core.Infrastructure.Files;

// Dựng catalog purpose NGAY lúc khởi động: FilePurposeCatalog ném khi khoá trùng, sai khuôn hoặc danh
// sách kiểu rỗng (cùng khuôn PermissionCatalogValidationHostedService). Không có lớp này, catalog chỉ
// được dựng ở lần tải tệp đầu tiên — tức lỗi cấu hình của module lộ ra ở người dùng, không ở lúc triển
// khai. Được canh bằng test seam động `FilePurposeCatalogValidationHostedService_IsRegistered_InTheContainer`.
internal sealed class FilePurposeCatalogValidationHostedService(
    FilePurposeCatalog catalog, ILogger<FilePurposeCatalogValidationHostedService> logger) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Catalog purpose tệp hợp lệ: {Count} khoá.", catalog.All.Count);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
