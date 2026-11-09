using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CoreAndSkill.Core.Web.DependencyInjection;

// docs/wiki-core/be/07-observability.md §2.3: ngoài Development, console ghi JSON một dòng mỗi bản ghi bằng formatter có sẵn của
// .NET; Development giữ console văn bản mặc định. Ba thiết lập dưới đây là chỗ formatter mặc định làm hỏng log mà không báo.
internal static class CoreLoggingExtensions
{
    internal static IServiceCollection AddCoreLogging(this IServiceCollection services, IHostEnvironment environment)
    {
        if (environment.IsDevelopment())
            return services;

        return services.AddLogging(logging => logging.AddJsonConsole(options =>
        {
            options.IncludeScopes = true; // mã lần gọi nằm ở phạm vi log — thiếu thì mất khỏi mọi dòng
            options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ"; // để trống thì dòng JSON không có thời điểm
            options.UseUtcTimestamp = true;
            options.JsonWriterOptions = new JsonWriterOptions
            {
                Indented = false, // đúng một dòng mỗi bản ghi
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping // tiếng Việt đọc được, vẫn thoát nháy kép và ký tự điều khiển
            };
        }));
    }
}
