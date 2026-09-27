using CoreAndSkill.Core.Application.Common.Cqrs;

namespace CoreAndSkill.Core.Application.Profile;

// POST /api/v1/core/profile/renounce-permission-bypass — docs/contracts/profile.md §3. Không body,
// chỉ tác động lên CHÍNH tài khoản gọi.
public sealed record RenouncePermissionBypassCommand : ICommand;
