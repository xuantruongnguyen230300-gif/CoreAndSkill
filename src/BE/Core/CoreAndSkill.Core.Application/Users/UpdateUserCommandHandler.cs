using CoreAndSkill.Core.Application.Identity;
using CoreAndSkill.Core.Domain.Common;
using MediatR;

namespace CoreAndSkill.Core.Application.Users;

internal sealed class UpdateUserCommandHandler(IUserAdminService userAdmin)
    : IRequestHandler<UpdateUserCommand, Result>
{
    public Task<Result> Handle(UpdateUserCommand command, CancellationToken ct)
        => userAdmin.UpdateAsync(
            command.Id, new UpdateUserInput(command.Email, command.FullName, command.Version), ct);
}
