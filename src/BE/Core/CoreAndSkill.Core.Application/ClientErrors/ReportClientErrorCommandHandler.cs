using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Application.Common.Logging;
using CoreAndSkill.Core.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CoreAndSkill.Core.Application.ClientErrors;

// docs/contracts/client-errors.md §1. Endpoint BEST-EFFORT — không dựng bảng riêng ở v1, chỉ ghi
// log mức Error theo khuôn có cấu trúc (docs/wiki-core/be/07-observability.md §2.2): truyền THAM SỐ
// có tên, KHÔNG log nguyên object request (§5 "Ba đường rò hay gặp").
//
// MỌI FIELD ĐI QUA BỘ LỌC TRƯỜNG NHẠY CẢM trước khi ghi — card client-errors.md mục Ghi chú, và
// 07-observability.md §5. Không phải phòng xa: nội dung ở đây do TRÌNH DUYỆT soạn, và §5 "Ba đường
// rò hay gặp" hàng thứ ba (*thông điệp lỗi của thư viện ngoài*) là đúng đường đi tới endpoint này —
// một `TypeError` do thư viện ngoài soạn có thể kèm giá trị người dùng vừa gõ
// (`"Invalid value for field matKhau: 'Abc@12345'"`, 47 ký tự nên validator nhận). Ghi nguyên văn ở
// mức Error là đưa mật khẩu vào nơi sống lâu hơn dữ liệu, được sao sang hệ phân tích, và nhiều
// người đọc được hơn DB — trong khi request trả 200 và không ai biết.
//
// Cắt stack KHÔNG thay được bộ lọc và bộ lọc KHÔNG thay được cắt stack: cái đầu giới hạn KÍCH CỠ,
// cái sau giới hạn NỘI DUNG. Nửa đầu từng có một mình.
internal sealed class ReportClientErrorCommandHandler(
    ILogger<ReportClientErrorCommandHandler> logger, IClientAddressAccessor clientAddress)
    : IRequestHandler<ReportClientErrorCommand, Result>
{
    private const int MaxStackLines = 20;

    public Task<Result> Handle(ReportClientErrorCommand command, CancellationToken ct)
    {
        // Cắt TRƯỚC rồi lọc, không phải ngược lại: lọc xong mới cắt là tốn công lọc 20 dòng đầu
        // cộng phần sắp bị vứt, mà kết quả ghi ra không khác.
        var stack = SensitiveTextRedactor.Redact(TruncateStack(command.Stack));

        // User-Agent do CLIENT gửi nên client soạn được — nó đi qua bộ lọc như mọi field khác. IP do
        // hạ tầng đọc ra, không phải văn bản tự do, nên không cần lọc.
        logger.LogError(
            "Lỗi runtime từ trình duyệt: {Kind} tại {DuongDan} (build {PhienBanApp}, trace {ClientTraceId}, " +
            "ip {ClientIp}, user-agent {UserAgent}). {Message}\n{Stack}",
            SensitiveTextRedactor.Redact(command.Kind),
            SensitiveTextRedactor.Redact(command.DuongDan),
            SensitiveTextRedactor.Redact(command.PhienBanApp),
            SensitiveTextRedactor.Redact(command.TraceId) ?? "(none)",
            clientAddress.RemoteIpAddress?.ToString() ?? "(unknown)",
            SensitiveTextRedactor.Redact(clientAddress.UserAgent) ?? "(unknown)",
            SensitiveTextRedactor.Redact(command.Message),
            stack ?? "(không có stack)");

        return Task.FromResult(Result.Success());
    }

    private static string? TruncateStack(string? stack)
    {
        if (string.IsNullOrEmpty(stack))
            return stack;

        var lines = stack.Split('\n');
        return lines.Length <= MaxStackLines ? stack : string.Join('\n', lines.Take(MaxStackLines));
    }
}
