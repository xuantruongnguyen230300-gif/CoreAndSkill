using CoreAndSkill.Core.Application.Common;
using CoreAndSkill.Core.Domain.Common;
using MediatR;

namespace CoreAndSkill.Core.Application.Roles;

internal sealed class CreateRoleCommandHandler(IRoleAdminService roleAdmin)
    : IRequestHandler<CreateRoleCommand, Result<CreatedIdDto>>
{
    public async Task<Result<CreatedIdDto>> Handle(CreateRoleCommand command, CancellationToken ct)
    {
        var result = await roleAdmin.CreateAsync(command.Name, ct);
        return result.IsSuccess ? Result.Success(new CreatedIdDto(result.Value)) : Result.Failure<CreatedIdDto>(result.Error!);
    }
}
