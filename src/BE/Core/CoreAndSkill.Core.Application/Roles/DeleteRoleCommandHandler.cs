using CoreAndSkill.Core.Domain.Common;
using MediatR;

namespace CoreAndSkill.Core.Application.Roles;

internal sealed class DeleteRoleCommandHandler(IRoleAdminService roleAdmin)
    : IRequestHandler<DeleteRoleCommand, Result>
{
    public Task<Result> Handle(DeleteRoleCommand command, CancellationToken ct)
        => roleAdmin.DeleteAsync(command.Id, ct);
}
