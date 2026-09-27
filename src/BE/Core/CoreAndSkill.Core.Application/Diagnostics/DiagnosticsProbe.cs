using CoreAndSkill.Core.Domain.Common;

namespace CoreAndSkill.Core.Application.Diagnostics;

// Không đi qua ICommand/IQuery + MediatR: B0 chưa dựng pipeline CQRS (docs/wiki-core/be/trien-khai/01-b0-nen-mong.md
// §1 không liệt kê be-cqrs-handler.md §1/§5). Đây là điểm tự-kiểm của chính B0, không phải một use case
// nghiệp vụ — controller gọi thẳng, giữ đúng khuôn "Result<T> + catalog lỗi" để chứng minh cầu nối
// Result → HTTP mà không cần chờ hạ tầng CQRS của B1.
public static class DiagnosticsProbe
{
    public static Result<DiagnosticsProbeResponse> Succeed(TimeProvider timeProvider)
        => Result.Success(new DiagnosticsProbeResponse("pong", timeProvider.GetUtcNow()));

    public static Result<DiagnosticsProbeResponse> Fail()
        => Result.Failure<DiagnosticsProbeResponse>(DiagnosticsErrors.ProbeFailureRequested);
}
