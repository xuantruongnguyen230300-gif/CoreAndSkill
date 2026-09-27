using System.Text.RegularExpressions;
using CoreAndSkill.Core.Application.Permissions;
using Microsoft.Extensions.Hosting;

namespace CoreAndSkill.Core.Infrastructure.Permissions;

// Kiểm danh mục quyền ĐÃ GỘP lúc khởi động — docs/quy-uoc/be-architecture.md §1.1 "Danh mục khoá
// quyền do module cấp". Bất kỳ vi phạm nào dưới đây làm tiến trình KHÔNG khởi động (ném từ
// StartAsync — host dừng lại, không mở cổng).
internal sealed partial class PermissionCatalogValidationHostedService(IEnumerable<IPermissionCatalogSource> sources)
    : IHostedService
{
    [GeneratedRegex(@"^[a-z][a-z0-9-]*(\.[a-z][a-z0-9-]*)*$")]
    private static partial Regex CodeSegmentPattern();

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var resources = new List<PermissionResourceDefinition>();
        var permissions = new List<PermissionDefinition>();

        foreach (var source in sources)
        {
            var sourceResources = source.GetResources();
            var sourcePermissions = source.GetPermissions();

            resources.AddRange(sourceResources);
            permissions.AddRange(sourcePermissions);

            var resourceKeysInSource = sourceResources.Select(r => r.Key).ToHashSet(StringComparer.Ordinal);
            foreach (var permission in sourcePermissions)
            {
                if (!resourceKeysInSource.Contains(permission.ResourceKey))
                    throw new InvalidOperationException(
                        $"Danh mục quyền không hợp lệ: permission '{permission.Code}' khai ResourceKey " +
                        $"'{permission.ResourceKey}' không có PermissionResourceDefinition nào trong CÙNG nguồn " +
                        $"({source.GetType().FullName}).");
            }
        }

        var duplicateResourceKey = resources
            .GroupBy(r => r.Key, StringComparer.Ordinal)
            .FirstOrDefault(g => g.Count() > 1);
        if (duplicateResourceKey is not null)
            throw new InvalidOperationException(
                $"Danh mục quyền không hợp lệ: Key tài nguyên '{duplicateResourceKey.Key}' bị khai trùng ở nhiều nguồn.");

        var duplicateCode = permissions
            .GroupBy(p => p.Code, StringComparer.Ordinal)
            .FirstOrDefault(g => g.Count() > 1);
        if (duplicateCode is not null)
            throw new InvalidOperationException(
                $"Danh mục quyền không hợp lệ: Code quyền '{duplicateCode.Key}' bị khai trùng ở nhiều nguồn.");

        foreach (var resource in resources)
        {
            if (string.IsNullOrWhiteSpace(resource.NameKey))
                throw new InvalidOperationException(
                    $"Danh mục quyền không hợp lệ: tài nguyên '{resource.Key}' có NameKey rỗng.");
        }

        foreach (var permission in permissions)
        {
            if (string.IsNullOrWhiteSpace(permission.NameKey))
                throw new InvalidOperationException(
                    $"Danh mục quyền không hợp lệ: quyền '{permission.Code}' có NameKey rỗng.");

            if (!CodeSegmentPattern().IsMatch(permission.Code))
                throw new InvalidOperationException(
                    $"Danh mục quyền không hợp lệ: Code '{permission.Code}' sai khuôn <tài nguyên>.<hành động>.");

            var expectedCode = $"{permission.ResourceKey}.{permission.Action}";
            if (!string.Equals(permission.Code, expectedCode, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    $"Danh mục quyền không hợp lệ: Code '{permission.Code}' không bằng ResourceKey + \".\" + " +
                    $"Action (mong đợi '{expectedCode}').");
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
