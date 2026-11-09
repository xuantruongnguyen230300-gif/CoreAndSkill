using System.Text;

namespace CoreAndSkill.Core.Web.DependencyInjection;

// Console Windows mặc định dùng code page OEM nên chữ tiếng Việt in ra bị hỏng. Chỉ đặt khi chạy trên Windows và đầu ra là
// console thật (không bị chuyển hướng) — máy chủ Linux, container, test và đường ống không bị chạm tới.
internal static class CoreConsoleEncoding
{
    internal static void UseUtf8OnWindowsConsole()
    {
        if (!OperatingSystem.IsWindows() || Console.IsOutputRedirected)
            return;

        Console.OutputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    }
}
