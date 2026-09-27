using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Application.Tenants;
using CoreAndSkill.Core.Domain.Common;
using Microsoft.Extensions.DependencyInjection;

namespace CoreAndSkill.Core.IntegrationTests.Support;

// ITenantProvisioningService chạy trong transaction của NGƯỜI GỌI và ném khi bị gọi ngoài transaction
// (TenantProvisioningService.cs, chú thích đầu lớp). Test gọi thẳng service thì bọc giống hệt CoreCommandRunner:
// một IUnitOfWork.ExecuteInTransactionAsync mỗi lời gọi, commit chỉ khi Result thành công.
internal static class ProvisioningInTransaction
{
    public static Task<TResult> RunAsync<TResult>(
        IServiceProvider scopedServices, Func<ITenantProvisioningService, CancellationToken, Task<TResult>> operation)
        where TResult : IResult
    {
        var provisioning = scopedServices.GetRequiredService<ITenantProvisioningService>();
        var uow = scopedServices.GetRequiredService<IUnitOfWork>();

        return uow.ExecuteInTransactionAsync(async ct =>
        {
            var result = await operation(provisioning, ct);
            return new TransactionOutcome<TResult>(result, result.IsSuccess);
        }, CancellationToken.None);
    }
}
