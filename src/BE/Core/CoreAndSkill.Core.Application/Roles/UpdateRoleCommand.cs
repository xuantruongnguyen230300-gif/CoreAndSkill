using CoreAndSkill.Core.Application.Common.Cqrs;

namespace CoreAndSkill.Core.Application.Roles;

// PUT /api/v1/core/roles/{id} — docs/contracts/roles.md §3. Chỉ đổi Name. Version: token nhận từ GET gần nhất —
// docs/wiki-core/be/06-concurrency-control.md §6.3. Trả lại bản ghi sau khi đổi (cùng shape §5), mang version mới.
public sealed record UpdateRoleCommand(Guid Id, string Name, string? Version) : ICommand<RoleSummaryDto>;
