using CoreAndSkill.Core.Application.Files;
using CoreAndSkill.Core.Domain.Files;
using CoreAndSkill.Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAndSkill.Core.Infrastructure.Files;

internal sealed class EfFileRepository(CoreDbContext db) : IFileRepository
{
    public async Task AddAsync(StoredFile file, CancellationToken ct)
        => await db.Files.AddAsync(file, ct);

    // Bộ lọc đơn vị và xoá mềm áp tự động: tệp của đơn vị khác, hay đã gỡ, đều là "không có".
    public Task<StoredFile?> FindByIdAsync(Guid id, CancellationToken ct)
        => db.Files.FirstOrDefaultAsync(f => f.Id == id, ct);
}
