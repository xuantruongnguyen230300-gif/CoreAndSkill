using Microsoft.EntityFrameworkCore;

namespace CoreAndSkill.Core.Infrastructure.Persistence;

// Thời hạn chờ khoá của MỌI lượt ghi giữ khoá trong Core — docs/wiki-core/be/06-concurrency-control.md §7 quy tắc 3: luôn
// có thời hạn chờ. Nguồn duy nhất của giá trị này; nơi nào khoá (khoá tư vấn của ma trận quyền, khoá dòng core.app_role, câu
// so-và-đổi token giữ khoá dòng core.app_user của lượt gán vai trò…) gọi qua đây, TRƯỚC câu giữ khoá đầu tiên, không tự viết
// lại câu SET.
//
// Lượt ghi giữ khoá là vài câu lệnh ngắn; chờ quá 5 giây nghĩa là lượt đang giữ khoá đã kẹt. Hết hạn chờ thì PostgreSQL huỷ
// câu lệnh (55P03) — request thành 500, không treo, và không bị thử lại (docs/quy-uoc/be-performance.md §7.2 mục "Lỗi KHÔNG
// được thử lại"). SET LOCAL chỉ sống tới hết transaction hiện tại, và phủ mọi câu sau nó trong transaction đó — kể cả câu
// ghi của lượt lưu. Đặt lại trong cùng transaction là vô hại: cùng giá trị ghi đè cùng giá trị.
internal static class LockTimeout
{
    private const string SetLockTimeoutSql = "SET LOCAL lock_timeout = '5s'";

    public static Task SetForCurrentTransactionAsync(CoreDbContext db, CancellationToken ct)
        => db.Database.ExecuteSqlRawAsync(SetLockTimeoutSql, ct);
}
