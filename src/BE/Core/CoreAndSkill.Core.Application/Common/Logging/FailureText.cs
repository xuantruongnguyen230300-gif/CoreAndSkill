using System.Text;

namespace CoreAndSkill.Core.Application.Common.Logging;

// Mô tả một ngoại lệ — thông điệp đi qua bộ lọc trường nhạy cảm trước, vì thông điệp lỗi của thư viện ngoài
// có thể mang cả giá trị đầu vào (docs/wiki-core/be/07-observability.md §5, đường rò thứ ba; luật S13).
//   - Describe: cho cột DB (core.outbox_message.last_error) và mô tả health check. Một tầng, cắt ngắn, KHÔNG ngăn xếp.
//   - DescribeWithStack: cho dòng log mô tả ngoại lệ, mức Warning trở lên. Mọi tầng, kèm ngăn xếp.
// Cả hai KHÔNG bao giờ mang dữ liệu trong (Data) hay thông điệp chưa lọc.
public static class FailureText
{
    public const int MaxLength = 500;

    public static string Describe(Exception exception)
    {
        var message = SensitiveTextRedactor.Redact(exception.Message) ?? string.Empty;
        var text = $"{exception.GetType().Name}: {message}";

        return text.Length <= MaxLength ? text : text[..MaxLength];
    }

    // Bản GIỮ NGĂN XẾP — B28 (docs/wiki-core/be/07-observability.md §5 mục "Ghi một ngoại lệ vào
    // log"). Dùng cho dòng log mức Warning trở lên mô tả một ngoại lệ; KHÔNG dùng cho cột lưu DB
    // (core.outbox_message.last_error dùng Describe ở trên).
    //
    // Mỗi tầng: "<tên kiểu đầy đủ>: <thông điệp đã lọc, cắt ở MaxLength>" rồi tới StackTrace nguyên
    // văn của CHÍNH tầng đó — khung gọi là siêu dữ liệu biên dịch, không mang giá trị lúc chạy nên
    // không qua bộ lọc. Không cắt TỔNG độ dài: log không phải cột DB.
    //
    // Thứ tự từ ngoài vào trong theo InnerException, mỗi tầng trong mở đầu bằng " ---> " như
    // Exception.ToString(). AggregateException duyệt MỌI phần tử của InnerExceptions — không đi
    // InnerException của chính nó, vì đó luôn là phần tử đầu, đã duyệt qua nhánh InnerExceptions.
    public static string DescribeWithStack(Exception exception)
    {
        var builder = new StringBuilder();
        AppendChain(builder, exception, prefix: null);

        return builder.ToString();
    }

    private static void AppendChain(StringBuilder builder, Exception exception, string? prefix)
    {
        if (builder.Length > 0)
            builder.Append(Environment.NewLine);

        if (prefix is not null)
            builder.Append(prefix);

        AppendLayer(builder, exception);

        if (exception is AggregateException aggregate)
        {
            foreach (var branch in aggregate.InnerExceptions)
                AppendChain(builder, branch, " ---> ");
        }
        else if (exception.InnerException is { } inner)
        {
            AppendChain(builder, inner, " ---> ");
        }
    }

    private static void AppendLayer(StringBuilder builder, Exception exception)
    {
        var typeName = exception.GetType().FullName ?? exception.GetType().Name;
        var message = SensitiveTextRedactor.Redact(exception.Message) ?? string.Empty;

        if (message.Length > MaxLength)
            message = message[..MaxLength];

        builder.Append(typeName).Append(": ").Append(message);

        if (!string.IsNullOrEmpty(exception.StackTrace))
            builder.Append(Environment.NewLine).Append(exception.StackTrace);
    }
}
