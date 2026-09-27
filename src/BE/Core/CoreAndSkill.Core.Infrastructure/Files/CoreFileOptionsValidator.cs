using CoreAndSkill.Core.Application.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace CoreAndSkill.Core.Infrastructure.Files;

// Kiểm SÂU cấu hình lưu trữ lúc khởi động — docs/wiki-core/be/14-file-storage.md §2, luật A8. Thư mục
// không tồn tại hoặc không ghi được phải làm app KHÔNG khởi động, chứ không phải hỏng ở lần tải tệp đầu
// tiên; và thư mục đặt sai chỗ (trong thư mục ứng dụng: triển khai lại là mất sạch tệp; trong thư mục
// phục vụ tĩnh: ai đoán được đường dẫn là tải được) phải bị từ chối ngay.
//
// [Required] của CoreFileOptions chỉ bắt được "thiếu khoá"; phần còn lại cần chạm hệ thống tệp nên
// nằm ở Infrastructure, không ở Application.
internal sealed class CoreFileOptionsValidator(IHostEnvironment environment) : IValidateOptions<CoreFileOptions>
{
    public ValidateOptionsResult Validate(string? name, CoreFileOptions options)
    {
        var failures = new List<string>();
        var raw = options.RootPath;

        // Khoá thiếu đã có thông báo từ ValidateDataAnnotations; tránh nói hai lần.
        if (string.IsNullOrWhiteSpace(raw))
            return ValidateOptionsResult.Success;

        if (!Path.IsPathFullyQualified(raw))
        {
            failures.Add("Core:File:RootPath phải là đường dẫn tuyệt đối — đường dẫn tương đối phụ thuộc thư mục chạy lệnh.");
            return ValidateOptionsResult.Fail(failures);
        }

        var root = Path.GetFullPath(raw);

        // Tệp lâu dài không được nằm trong thư mục ứng dụng (triển khai lại là mất) hay thư mục phục vụ
        // tĩnh. wwwroot mặc định nằm trong ContentRootPath nên phép kiểm này phủ cả nó.
        foreach (var forbidden in new[] { AppContext.BaseDirectory, environment.ContentRootPath })
        {
            if (!string.IsNullOrEmpty(forbidden) && IsSameOrInside(root, Path.GetFullPath(forbidden)))
            {
                failures.Add(
                    "Core:File:RootPath không được nằm trong thư mục ứng dụng hoặc thư mục phục vụ tĩnh — " +
                    "triển khai lại sẽ mất sạch tệp, và tệp trong thư mục phục vụ tĩnh tải được không qua kiểm quyền.");
                break;
            }
        }

        if (!Directory.Exists(root))
        {
            failures.Add("Core:File:RootPath trỏ tới thư mục không tồn tại — tạo nó trước khi khởi động (Core không tự tạo).");
        }
        else if (!IsWritable(root))
        {
            failures.Add("Core:File:RootPath là thư mục tiến trình không ghi được.");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    private static bool IsSameOrInside(string candidate, string container)
    {
        var containerWithSeparator = container.EndsWith(Path.DirectorySeparatorChar)
            ? container
            : container + Path.DirectorySeparatorChar;
        var candidateWithSeparator = candidate.EndsWith(Path.DirectorySeparatorChar)
            ? candidate
            : candidate + Path.DirectorySeparatorChar;

        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        return candidateWithSeparator.StartsWith(containerWithSeparator, comparison);
    }

    // Thử ghi thật một tệp rồi xoá: quyền ghi trên thư mục không suy ra được từ thuộc tính.
    private static bool IsWritable(string directory)
    {
        var probe = Path.Combine(directory, $".write-probe-{Guid.NewGuid():N}");
        try
        {
            File.WriteAllBytes(probe, [0]);
            File.Delete(probe);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}
