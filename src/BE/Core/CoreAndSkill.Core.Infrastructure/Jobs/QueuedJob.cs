using CoreAndSkill.Core.Application.Common.Interfaces;

namespace CoreAndSkill.Core.Infrastructure.Jobs;

// Một lần chạy đã enqueue — docs/quy-uoc/be-cqrs-handler.md §10.2. Run đóng gói TOÀN BỘ lời gọi
// (resolve TJob từ DI scope của lượt chạy + gọi phương thức đã biên dịch từ biểu thức), nên hàng
// đợi không cần biết TJob là kiểu gì — tránh phải bóc tách MethodCallExpression bằng reflection.
internal sealed record QueuedJob(string JobId, Func<IServiceProvider, CancellationToken, Task> Run, ExecutionContextSnapshot? Scope);
