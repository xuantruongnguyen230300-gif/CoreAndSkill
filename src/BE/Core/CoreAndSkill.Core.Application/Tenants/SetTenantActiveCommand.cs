using CoreAndSkill.Core.Application.Common.Cqrs;

namespace CoreAndSkill.Core.Application.Tenants;

// PUT /api/v1/core/system/tenants/{id}/active — docs/contracts/tenants.md §3. IsActive kiểu bool?
// (không phải bool) để validator phân biệt được "thiếu trường" (null) với "false" tường minh — một
// bool thường bind về mặc định false khi JSON thiếu trường, không có gì báo.
public sealed record SetTenantActiveCommand(Guid TenantId, bool? IsActive) : ICommand;
