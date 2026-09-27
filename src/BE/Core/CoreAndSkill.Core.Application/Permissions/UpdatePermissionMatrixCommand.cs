using CoreAndSkill.Core.Application.Common.Cqrs;

namespace CoreAndSkill.Core.Application.Permissions;

// PUT /api/v1/core/permissions/matrix — docs/contracts/permissions.md §6. Ghi đè TOÀN BỘ ma trận.
public sealed record UpdatePermissionMatrixEntry(Guid PermissionId, IReadOnlyList<Guid> RoleIds);

// data trả về CHỈ mang version mới — trả bản ghi thay vì chuỗi trần để hình dạng dây khớp đúng
// { "version": "…" } của card, không cần controller tự dựng kiểu vô danh.
public sealed record UpdatePermissionMatrixResultDto(string Version);

public sealed record UpdatePermissionMatrixCommand(
    string? Version, IReadOnlyList<UpdatePermissionMatrixEntry>? Entries) : ICommand<UpdatePermissionMatrixResultDto>;
