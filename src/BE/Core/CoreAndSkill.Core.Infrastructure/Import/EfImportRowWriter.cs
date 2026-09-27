using CoreAndSkill.Core.Application.Import;
using CoreAndSkill.Core.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CoreAndSkill.Core.Infrastructure.Import;

// Ghi và HUỶ THEO DÕI thay đổi của MỘT dòng nhập — docs/wiki-core/be/15-import-export.md §4.2, §6.1.
// Mọi DbContext đã đăng ký (Core và mọi module) — cùng tập với IUnitOfWork.
//
// DiscardTrackedChanges gỡ MỌI entity khỏi bộ theo dõi (ChangeTracker.Clear): entity Added của dòng lỗi
// không bao giờ được ghi cùng dòng sau, entity Modified không để lại trạng thái bẩn, và bộ theo dõi
// không phình theo số dòng đã đi qua. An toàn vì bộ chạy nhập KHÔNG giữ entity nào cần sống qua dòng —
// mỗi dòng là một đơn vị độc lập, và trạng thái việc được ghi bằng câu lệnh riêng (EfJobRepository).
internal sealed class EfImportRowWriter(IEnumerable<DbContext> contexts) : IImportRowWriter
{
    private readonly IReadOnlyList<DbContext> _contexts = contexts.ToList();

    public async Task<Result> SaveRowAsync(CancellationToken ct)
    {
        try
        {
            foreach (var context in _contexts)
                await context.SaveChangesAsync(ct);

            return Result.Success();
        }
        catch (DbUpdateException ex)
        {
            // Cơ sở dữ liệu TỪ CHỐI dòng này (trùng khoá đua với một lần ghi khác, vi phạm ràng buộc mà
            // tầng nghiệp vụ không thấy trước). Dòng bị báo lỗi, việc KHÔNG chết — và phần dòng này đã
            // thêm phải được HUỶ, nếu không lần SaveChanges kế sẽ thử ghi lại nó.
            DiscardTrackedChanges();

            return Result.Failure(
                ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation }
                    ? ImportErrors.DuplicateRow
                    : ImportErrors.RowNotSaved);
        }
    }

    public void DiscardTrackedChanges()
    {
        foreach (var context in _contexts)
            context.ChangeTracker.Clear();
    }
}
