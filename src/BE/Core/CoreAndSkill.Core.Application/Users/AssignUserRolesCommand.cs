using CoreAndSkill.Core.Application.Common.Cqrs;

namespace CoreAndSkill.Core.Application.Users;

// PUT /api/v1/core/users/{id}/roles — docs/contracts/users.md §7. Ngữ nghĩa THAY THẾ toàn bộ.
// Version: token của TÀI KHOẢN đích (docs/adr/0082-gan-vai-tro-dung-token-cua-tai-khoan.md). Không có luật validator cho
// nó: thiếu là 409 (null không bao giờ khớp), không phải 400 — cùng khuôn UpdateUserCommand.
public sealed record AssignUserRolesCommand(Guid UserId, IReadOnlyList<Guid> RoleIds, string? Version) : ICommand;
