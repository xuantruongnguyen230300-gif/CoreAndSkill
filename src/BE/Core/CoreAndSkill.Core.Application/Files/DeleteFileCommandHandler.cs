using CoreAndSkill.Core.Domain.Common;
using MediatR;

namespace CoreAndSkill.Core.Application.Files;

internal sealed class DeleteFileCommandHandler(IFileRepository files, FileAccessPolicy access)
    : IRequestHandler<DeleteFileCommand, Result>
{
    public async Task<Result> Handle(DeleteFileCommand command, CancellationToken ct)
    {
        var file = await files.FindByIdAsync(command.Id, ct);

        // Quyền GHI bản ghi chủ (không phải quyền đọc); không có ⇒ gộp với "không có" (luật M7).
        if (file is null || !await access.CanWriteAsync(file, ct))
            return Result.Failure(FileErrors.NotFound);

        file.Remove();
        return Result.Success();
    }
}
