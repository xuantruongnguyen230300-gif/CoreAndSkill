namespace CoreAndSkill.Core.Application.Common.Logging;

// Mô tả một ngoại lệ để LƯU (core.outbox_message.last_error) hoặc LOG — đi qua bộ lọc trường nhạy cảm
// trước, vì thông điệp lỗi của thư viện ngoài có thể mang cả giá trị đầu vào
// (docs/wiki-core/be/07-observability.md §5, đường rò thứ ba; luật S13). Chỉ kiểu ngoại lệ và
// thông điệp đã lọc, cắt ngắn — KHÔNG ngăn xếp, KHÔNG dữ liệu trong (Data).
public static class FailureText
{
    public const int MaxLength = 500;

    public static string Describe(Exception exception)
    {
        var message = SensitiveTextRedactor.Redact(exception.Message) ?? string.Empty;
        var text = $"{exception.GetType().Name}: {message}";

        return text.Length <= MaxLength ? text : text[..MaxLength];
    }
}
