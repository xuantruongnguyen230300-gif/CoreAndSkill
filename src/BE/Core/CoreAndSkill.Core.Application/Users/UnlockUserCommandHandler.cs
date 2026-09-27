using CoreAndSkill.Core.Application.Identity;
using CoreAndSkill.Core.Domain.Common;
using MediatR;

namespace CoreAndSkill.Core.Application.Users;

internal sealed class UnlockUserCommandHandler(IUserAdminService userAdmin)
    : IRequestHandler<UnlockUserCommand, Result>
{
    public Task<Result> Handle(UnlockUserCommand command, CancellationToken ct)
        => userAdmin.UnlockAsync(command.UserId, command.Version, ct);
}
