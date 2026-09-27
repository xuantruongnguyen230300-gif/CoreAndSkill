using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using Npgsql.EntityFrameworkCore.PostgreSQL;

namespace CoreAndSkill.Core.Infrastructure.Persistence;

// Chiến lược thử lại của Core — docs/quy-uoc/be-performance.md §7.2 mục "Lỗi KHÔNG được thử lại" (định nghĩa gốc),
// docs/adr/0053-thu-lai-khong-ap-cho-het-han-cho-va-commit-khong-ro-ket-qua.md. Giữ số lần thử, trễ tối đa và phân loại
// tạm thời của Npgsql, TRỪ hai lỗi hết thời hạn chờ:
//   • PostgresException 55P03 (hết lock_timeout) — thời hạn chờ là trần cho CẢ request, không phải cho một lượt thử;
//   • NpgsqlException bọc TimeoutException (hết CommandTimeout) — câu vừa quá hạn sẽ lại quá hạn trên database đang chậm.
// errorCodesToAdd của NpgsqlRetryingExecutionStrategy chỉ THÊM mã, không bớt được — nên phải override.
//
// Phân loại này bám vào danh sách lỗi tạm thời của Npgsql: nâng Npgsql thì CoreExecutionStrategyTests (luật E11) là
// thứ báo khi danh sách đó đổi.
//
// internal: chưa module nào tồn tại (docs/kien-truc-core-module.md §4). DbContext của module đọc ngoài transaction
// chưa có đường dùng strategy này — cái giá đã ghi trong ADR-0053.
internal sealed class CoreExecutionStrategy(ExecutionStrategyDependencies dependencies, int maxRetryCount, TimeSpan maxRetryDelay)
    : NpgsqlRetryingExecutionStrategy(dependencies, maxRetryCount, maxRetryDelay, errorCodesToAdd: null)
{
    public const int CoreMaxRetryCount = 3;
    public static readonly TimeSpan CoreMaxRetryDelay = TimeSpan.FromSeconds(5);

    protected override bool ShouldRetryOn(Exception? exception)
        => !IsWaitLimitExceeded(exception) && base.ShouldRetryOn(exception);

    // Lớp gốc nhận ngoại lệ đã bóc DbUpdateException; bóc thêm ở đây để phép loại không phụ thuộc chỗ gọi. EF bọc lỗi
    // provider trong InvalidOperationException ở vài đường đọc — cũng bóc.
    private static bool IsWaitLimitExceeded(Exception? exception)
    {
        var current = exception;
        while (current is not null)
        {
            if (current is PostgresException { SqlState: PostgresErrorCodes.LockNotAvailable })
                return true;

            if (current is NpgsqlException { InnerException: TimeoutException })
                return true;

            current = current is DbUpdateException or InvalidOperationException ? current.InnerException : null;
        }

        return false;
    }
}
