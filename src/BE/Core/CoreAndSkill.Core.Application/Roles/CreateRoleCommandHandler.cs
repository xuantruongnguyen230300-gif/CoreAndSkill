using CoreAndSkill.Core.Domain.Common;
using MediatR;

namespace CoreAndSkill.Core.Application.Roles;

internal sealed class CreateRoleCommandHandler(IRoleAdminService roleAdmin)
    : IRequestHandler<CreateRoleCommand, Result<Guid>>
{
    public Task<Result<Guid>> Handle(CreateRoleCommand command, CancellationToken ct)
        => roleAdmin.CreateAsync(command.Name, ct);
}
