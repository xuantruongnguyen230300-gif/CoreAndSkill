namespace CoreAndSkill.Core.Infrastructure.Persistence;

// Lỗi đường truyền ném từ CommitAsync — không biết máy chủ đã commit hay chưa. docs/quy-uoc/be-cqrs-handler.md §4 luật 2
// (luật E12), docs/adr/0053-thu-lai-khong-ap-cho-het-han-cho-va-commit-khong-ro-ket-qua.md quyết định 2.
//
// CỐ Ý không kế thừa NpgsqlException hay TimeoutException: phân loại tạm thời của Npgsql (và của CoreExecutionStrategy)
// không nhận ra nó, nên execution strategy KHÔNG chạy lại đơn vị công việc — chạy lại có thể ghi hai lần. Đi ra thành 500.
public sealed class CommitOutcomeUnknownException(string? traceId, Exception innerException)
    : Exception(
        $"Commit không rõ kết quả (traceId: {traceId ?? "không có"}) — dữ liệu có thể ĐÃ được ghi. Không chạy lại đơn vị công việc.",
        innerException)
{
    public string? TraceId { get; } = traceId;
}
