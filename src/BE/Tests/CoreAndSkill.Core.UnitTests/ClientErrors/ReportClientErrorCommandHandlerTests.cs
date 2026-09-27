using System.Net;
using CoreAndSkill.Core.Application.ClientErrors;
using CoreAndSkill.Core.Application.Common.Interfaces;
using CoreAndSkill.Core.Application.Common.Logging;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.UnitTests.ClientErrors;

// Hai luật khác nhau đi qua cùng một handler, và trước đây chỉ luật đầu có test:
//   • best-effort — luôn trả Success, kể cả khi đầu vào xấu (docs/contracts/client-errors.md §1);
//   • KHÔNG ĐƯỢC LOG CÁI GÌ — docs/wiki-core/be/07-observability.md §5, và card client-errors.md
//     mục Ghi chú: "mọi field đi qua bộ lọc trường nhạy cảm".
//
// Luật thứ hai chỉ kiểm được khi nhìn thấy NỘI DUNG dòng log, nên các test dưới dùng
// RecordingLogger chứ không phải NullLogger.
public class ReportClientErrorCommandHandlerTests
{
    private readonly RecordingLogger<ReportClientErrorCommandHandler> _logger = new();
    private readonly IClientAddressAccessor _clientAddress = Substitute.For<IClientAddressAccessor>();

    [Fact]
    public async Task Handle_AlwaysSucceeds_BestEffort()
    {
        _clientAddress.RemoteIpAddress.Returns(IPAddress.Loopback);
        _clientAddress.UserAgent.Returns("Mozilla/5.0");

        var result = await HandleAsync(
            new ReportClientErrorCommand("TypeError", "lỗi", "at a.js:1:1", "/trang", "trace-1", "2026.09.10-a1"));

        result.IsSuccess.ShouldBeTrue();

        // Mức Error là thứ card chốt ("một dòng log mức Error với nguồn là client"), và nó là lý do
        // phần rò nguy hiểm: mức có lưu trữ ở mọi môi trường (§4).
        _logger.Entries.ShouldHaveSingleItem().Level.ShouldBe(LogLevel.Error);
    }

    [Fact]
    public async Task Handle_StackOver20Lines_TruncatedInLog_ButResultStillSuccess()
    {
        var longStack = string.Join('\n', Enumerable.Range(1, 50).Select(i => $"at frame{i}.js:1:1"));

        var result = await HandleAsync(
            new ReportClientErrorCommand("TypeError", "lỗi", longStack, "/trang", null, "2026.09.10-a1"));

        result.IsSuccess.ShouldBeTrue();

        // Tên test cũ hứa "TruncatedInLog" mà không có gì kiểm — NullLogger không cho nhìn nội dung.
        var logged = _logger.Entries.ShouldHaveSingleItem().Message;

        logged.ShouldContain("at frame20.js");
        logged.ShouldNotContain("at frame21.js");
    }

    // ===== Detector_* — bộ lọc trường nhạy cảm (§5) =====

    // Kịch bản rò: thông điệp do THƯ VIỆN NGOÀI soạn mang theo giá trị người dùng vừa gõ (§5, "Ba
    // đường rò hay gặp", hàng ba). Chuỗi dưới đây dài 47 ký tự nên validator NHẬN — không có nhánh
    // từ chối nào đứng giữa nó và dòng log.
    [Fact]
    public async Task Detector_Handle_DoesNotLogPasswordValue_FromLibraryErrorMessage()
    {
        var result = await HandleAsync(new ReportClientErrorCommand(
            "TypeError",
            "Invalid value for field matKhau: 'Abc@12345'",
            "at LoginForm.submit (main-A7F2.js:1:2481)",
            "/dang-nhap",
            "c1807b11710f43c1964ec2988f532e56",
            "2026.09.10-a1b2c3d"));

        result.IsSuccess.ShouldBeTrue();
        AssertNothingLoggedContains("Abc@12345");
        _logger.Entries.ShouldHaveSingleItem().Message.ShouldContain(SensitiveTextRedactor.Mask);
    }

    // MỌI field đi qua bộ lọc, không chỉ `message` — card nói "mọi field". Mỗi ca dưới đây cắm bí
    // mật vào một field khác nhau; bỏ lọc ở đúng một field là đủ để rò.
    [Theory]
    [InlineData("kind")]
    [InlineData("message")]
    [InlineData("stack")]
    [InlineData("duongDan")]
    [InlineData("traceId")]
    [InlineData("userAgent")]
    public async Task Detector_Handle_FiltersEveryField_NotOnlyMessage(string field)
    {
        const string secret = "matKhau=Abc@12345";

        _clientAddress.UserAgent.Returns(field == "userAgent" ? secret : "Mozilla/5.0");

        var result = await HandleAsync(new ReportClientErrorCommand(
            Kind: field == "kind" ? secret : "TypeError",
            Message: field == "message" ? secret : "lỗi",
            Stack: field == "stack" ? secret : "at a.js:1:1",
            DuongDan: field == "duongDan" ? secret : "/trang",
            TraceId: field == "traceId" ? secret : "trace-1",
            PhienBanApp: "2026.09.10-a1"));

        result.IsSuccess.ShouldBeTrue();
        AssertNothingLoggedContains("Abc@12345");
    }

    // Bí mật nằm ở dòng stack thứ 3 — trong phần ĐƯỢC GIỮ sau khi cắt. Cắt 20 dòng và lọc nội dung
    // là hai luật khác nhau; ca này đỏ nếu ai đó tưởng cắt stack đã là đủ.
    [Fact]
    public async Task Detector_Handle_FiltersStack_EvenInsideTheKeptTwentyLines()
    {
        var stack = string.Join('\n', Enumerable.Range(1, 50)
            .Select(i => i == 3 ? "at submit (main.js:1:1) token: 'eyJhbGciOi.eyJzdWIi.c2ln'" : $"at frame{i}.js:1:1"));

        var result = await HandleAsync(
            new ReportClientErrorCommand("TypeError", "lỗi", stack, "/trang", null, "2026.09.10-a1"));

        result.IsSuccess.ShouldBeTrue();
        AssertNothingLoggedContains("eyJ");
    }

    // Ca ĐỐI: bộ lọc không được biến dòng log thành vô dụng. Đỏ ở đây nghĩa là mọi báo cáo lỗi về
    // log dưới dạng đã bị xoá, và lối thoát tự nhiên khi đó là gỡ hẳn bộ lọc.
    [Fact]
    public async Task Handle_KeepsDiagnosticFields_WhenNothingIsSensitive()
    {
        _clientAddress.RemoteIpAddress.Returns(IPAddress.Loopback);
        _clientAddress.UserAgent.Returns("Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/140.0.0.0");

        await HandleAsync(new ReportClientErrorCommand(
            "TypeError",
            "Cannot read properties of undefined (reading 'ten')",
            "at PhieuListPage.hienThi (main-A7F2.js:1:2481)",
            "/phieu/danh-sach",
            "c1807b11710f43c1964ec2988f532e56",
            "2026.09.10-a1b2c3d"));

        var logged = _logger.Entries.ShouldHaveSingleItem().Message;

        logged.ShouldContain("TypeError");
        logged.ShouldContain("Cannot read properties of undefined (reading 'ten')");
        logged.ShouldContain("at PhieuListPage.hienThi (main-A7F2.js:1:2481)");
        logged.ShouldContain("/phieu/danh-sach");
        logged.ShouldContain("c1807b11710f43c1964ec2988f532e56");
        logged.ShouldContain("2026.09.10-a1b2c3d");
        logged.ShouldNotContain(SensitiveTextRedactor.Mask);
    }

    private Task<CoreAndSkill.Core.Domain.Common.Result> HandleAsync(ReportClientErrorCommand command)
        => new ReportClientErrorCommandHandler(_logger, _clientAddress).Handle(command, CancellationToken.None);

    // Kiểm CẢ HAI mặt: chuỗi đã dựng và từng giá trị tham số có cấu trúc. Sink dạng có cấu trúc giữ
    // tham số riêng, nên một bí mật chỉ vắng mặt ở chuỗi đã dựng vẫn là một bí mật đã vào log.
    private void AssertNothingLoggedContains(string secret)
    {
        var entry = _logger.Entries.ShouldHaveSingleItem();

        entry.Message.ShouldNotContain(secret);
        entry.Values.ShouldNotContain(value => value.Contains(secret, StringComparison.Ordinal));
    }
}
