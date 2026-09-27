using System.Globalization;
using CoreAndSkill.Core.Application.Common;
using CoreAndSkill.Core.Application.Configuration;
using CoreAndSkill.Core.Application.Files;
using CoreAndSkill.Core.Application.Identity;
using CoreAndSkill.Core.Web.Http;
using CoreAndSkill.Core.Web.Security;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoreAndSkill.Core.Web.ExceptionHandling;

// IExceptionHandler của Core — đăng ký bằng AddExceptionHandler<T>() trong AddCoreWeb, nối bằng
// UseExceptionHandler() trong UseCore. docs/quy-uoc/be-architecture.md §2.1, §3.1.
internal sealed class CoreExceptionHandler(ILogger<CoreExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        // Luật R10 (be-api-controller.md §2.4) — client đã huỷ request và ngoại lệ đi lên chỉ là hệ quả của lần huỷ đó:
        // 499, KHÔNG envelope (client đã đi, không ai đọc), log Debug (đóng tab giữa chừng không phải sự cố vận hành).
        // Ca ngoại lệ đứng ĐẦU là OperationCanceledException/IOException thì ExceptionHandlerMiddleware của framework đã
        // trả 499 trước khi gọi tới đây; Core gánh ca một tầng giữa BỌC lần huỷ vào ngoại lệ khác. Đứng TRƯỚC mọi nhánh
        // khác: mọi nhánh dưới đều ghi thân bằng cancellationToken (= RequestAborted, đã huỷ) và sẽ tự ném.
        if (httpContext.RequestAborted.IsCancellationRequested && ChainContainsClientAbort(exception))
        {
            logger.LogDebug(exception, "Client huỷ request ở {Path} — không phải lỗi.", httpContext.Request.Path);
            httpContext.Response.StatusCode = StatusCodes.Status499ClientClosedRequest;
            return true;
        }

        // DbUpdateConcurrencyException — dịch lỗi đồng thời cho ghi qua DbContext/IUnitOfWork trực
        // tiếp (be-api-controller.md §2.4). Ghi qua UserManager đi đường khác: UserStore tự bắt
        // ngoại lệ này bên trong, không để lộ ra đây — IdentityConcurrency.DetectConflict dịch mã
        // "ConcurrencyFailure" qua Result bình thường (docs/wiki-core/be/06-concurrency-control.md §6.1).
        if (exception is DbUpdateConcurrencyException)
        {
            httpContext.Response.StatusCode = ResultToHttpMapper.ToStatusCode(CommonErrors.ConcurrencyConflict.Type);
            await httpContext.Response.WriteAsJsonAsync(
                Envelope.Failure(CommonErrors.ConcurrencyConflict, httpContext.TraceIdentifier),
                cancellationToken);
            return true;
        }

        // Hàng rào 3 của rate limit — status đặt TRỰC TIẾP, không qua ResultToHttpMapper
        // (be-api-controller.md §2.4, §6.5). MỘT chuỗi số giây cho cả header lẫn messageParams.RetryAfterSeconds
        // (be-api-controller.md §6.3 ràng buộc 6, docs/contracts/auth.md §10).
        if (exception is LoginAttemptLimitExceededException limitExceeded)
        {
            var retryAfterSeconds = ((int)Math.Ceiling(limitExceeded.RetryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
            httpContext.Response.Headers.RetryAfter = retryAfterSeconds;
            httpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            await httpContext.Response.WriteAsJsonAsync(
                Envelope.Failure(
                    SecurityErrors.RateLimitExceeded.WithParams(("RetryAfterSeconds", retryAfterSeconds)),
                    httpContext.TraceIdentifier),
                cancellationToken);
            return true;
        }

        // Thân request vượt trần dung lượng ĐANG được đọc dở — Kestrel (413) hoặc multipart parser. Đó là
        // lỗi của NGƯỜI GỌI (contracts/files.md §1: CORE.FILE.TOO_LARGE, 400), không phải lỗi hệ thống, nên
        // không được rơi xuống 500 kèm dòng log Error mỗi lần có người gửi tệp quá lớn. Chỗ khai giới hạn:
        // UploadSizeLimitAttribute. `InvalidDataException` của multipart chỉ tính khi request thật sự là
        // form — nếu không, nó là lỗi khác và vẫn đi nhánh 500 bên dưới.
        if (exception is BadHttpRequestException { StatusCode: StatusCodes.Status413PayloadTooLarge }
            || (exception is InvalidDataException && httpContext.Request.HasFormContentType))
        {
            var options = httpContext.RequestServices.GetRequiredService<IOptions<CoreFileOptions>>().Value;
            var error = FileErrors.TooLarge.WithParams(("MaxBytes", (long)options.MaxUploadMb * 1024 * 1024));

            httpContext.Response.StatusCode = ResultToHttpMapper.ToStatusCode(error.Type);
            await httpContext.Response.WriteAsJsonAsync(Envelope.Failure(error, httpContext.TraceIdentifier), cancellationToken);
            return true;
        }

        logger.LogError(exception, "Ngoại lệ ngoài dự kiến ở {Path}", httpContext.Request.Path);

        httpContext.Response.StatusCode = ResultToHttpMapper.ToStatusCode(CommonErrors.Unexpected.Type);
        await httpContext.Response.WriteAsJsonAsync(
            Envelope.Failure(CommonErrors.Unexpected, httpContext.TraceIdentifier),
            cancellationToken);

        return true;
    }

    // Duyệt cả chuỗi InnerException (kể cả nhánh của AggregateException) tìm dấu vết của lần huỷ: OperationCanceledException
    // (token RequestAborted lan vào truy vấn/handler) hoặc IOException (ghi thân phản hồi vào kết nối đã đóng). Chỉ có nghĩa
    // khi RequestAborted đã huỷ — cùng ngoại lệ mà client vẫn còn thì là lỗi thật, đi nhánh 500 bên trên.
    private static bool ChainContainsClientAbort(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is OperationCanceledException or IOException)
                return true;

            if (current is AggregateException aggregate && aggregate.InnerExceptions.Any(ChainContainsClientAbort))
                return true;
        }

        return false;
    }
}
