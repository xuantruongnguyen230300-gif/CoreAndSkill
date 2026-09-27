using CoreAndSkill.Core.Application.Common.Logging;
using Shouldly;
using Xunit;

namespace CoreAndSkill.Core.UnitTests.Common;

// Test KHẲNG ĐỊNH BỘ LỌC ĐANG HOẠT ĐỘNG — docs/wiki-core/be/07-observability.md §5 đòi đúng thứ
// này bằng tên: "một bộ lọc trường nhạy cảm … cộng một test khẳng định bộ lọc đang hoạt động. Bộ
// lọc không có test kiểm chính nó thuộc đúng loại 'cổng hỏng âm thầm' ở 04-testing-strategy.md §3."
//
// Bộ lọc là loại mã hỏng ÂM THẦM theo cả hai chiều, nên mỗi ca khẳng định đi kèm một ca đối:
//   • hỏng chiều CHE THIẾU → bí mật vào log, request vẫn 200, không ai biết;
//   • hỏng chiều CHE THỪA → thông điệp lỗi bị xoá sạch, và lối thoát tự nhiên của người sửa khi đó
//     là gỡ hẳn bộ lọc — tức mở lại đúng lỗ ở chiều thứ nhất.
public class SensitiveTextRedactorTests
{
    // ===== Detector_* — chiều CHE THIẾU =====

    // Kịch bản rò có thật, nguyên văn từ §5 "Ba đường rò hay gặp" hàng ba: một thư viện ngoài đưa
    // giá trị đầu vào vào chính thông điệp lỗi, FE chuyển nguyên văn lên endpoint báo lỗi.
    [Fact]
    public void Detector_Redact_RemovesPasswordValue_FromLibraryErrorMessage()
    {
        var redacted = SensitiveTextRedactor.Redact("Invalid value for field matKhau: 'Abc@12345'");

        redacted.ShouldNotBeNull();
        redacted.ShouldNotContain("Abc@12345");
        redacted.ShouldContain(SensitiveTextRedactor.Mask);
        redacted.ShouldContain("matKhau");
    }

    [Theory]
    // Mọi cách viết cùng một tên trường: hoa thường, gạch dưới, dấu tiếng Việt, tiền tố/hậu tố.
    [InlineData("matKhau: 'Abc@12345'")]
    [InlineData("mat_khau = Abc@12345")]
    [InlineData("MAT_KHAU=Abc@12345")]
    [InlineData("mật khẩu: Abc@12345")]
    [InlineData("MẬT KHẨU : Abc@12345")]
    [InlineData("matKhauMoi: Abc@12345")]
    [InlineData("xacNhanMatKhau: Abc@12345")]
    [InlineData("{\"password\":\"Abc@12345\"}")]
    [InlineData("{ \"newPassword\" : \"Abc@12345\" }")]
    [InlineData("pwd == Abc@12345")]
    [InlineData("passphrase: Abc@12345")]
    [InlineData("accessToken: Abc@12345")]
    [InlineData("clientSecret: Abc@12345")]
    [InlineData("ConnectionString: Abc@12345")]
    [InlineData("Set-Cookie: Abc@12345")]
    [InlineData("sessionId=Abc@12345")]
    [InlineData("cardNumber: Abc@12345")]
    [InlineData("soCCCD: Abc@12345")]
    [InlineData("cvv: Abc@12345")]
    [InlineData("apiKey => Abc@12345")]
    public void Detector_Redact_RemovesValue_ForEverySpellingOfASensitiveName(string text)
    {
        var redacted = SensitiveTextRedactor.Redact(text);

        redacted.ShouldNotBeNull();
        redacted.ShouldNotContain("Abc@12345");
        redacted.ShouldContain(SensitiveTextRedactor.Mask);
    }

    // Giá trị KHÔNG nháy bị che tới hết mệnh đề, không dừng ở khoảng trắng đầu tiên. Dừng ở khoảng
    // trắng là che đúng chữ "Bearer" rồi để nguyên token — đo được nếu đổi luật quét.
    [Fact]
    public void Detector_Redact_RemovesWholeUnquotedValue_NotJustFirstWord()
    {
        var redacted = SensitiveTextRedactor.Redact("Authorization: Bearer eyJhbGciOi.eyJzdWIi.c2ln");

        redacted.ShouldNotBeNull();
        redacted.ShouldNotContain("eyJ");
        redacted.ShouldBe("Authorization: ***");
    }

    // Token KHÔNG có tên trường đứng cạnh — lớp nhận theo HÌNH DẠNG. §5 xếp token vào bảng "tuyệt
    // đối không log", nên một JWT trôi giữa câu vẫn phải biến mất.
    [Theory]
    [InlineData("Gọi thất bại, header gửi lên là Bearer eyJhbGciOi.eyJzdWIi.c2ln")]
    [InlineData("Phiên hết hạn: eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxIn0.c2lnbmF0dXJl")]
    public void Detector_Redact_RemovesJwtShapedToken_WithoutAnyFieldName(string text)
    {
        var redacted = SensitiveTextRedactor.Redact(text);

        redacted.ShouldNotBeNull();
        redacted.ShouldNotContain("eyJ");
        redacted.ShouldContain(SensitiveTextRedactor.Mask);
    }

    // §5 bảng "log có kiểm soát": địa chỉ thư che MỘT PHẦN — giữ tên miền để còn điều tra được,
    // bỏ phần tên. Che trọn thì mất luôn khả năng biết sự cố xảy ra với ai; không che thì đó là dữ
    // liệu cá nhân đi vào nơi nhiều người đọc được hơn DB.
    [Fact]
    public void Detector_Redact_PartiallyMasksEmailAddress()
    {
        var redacted = SensitiveTextRedactor.Redact("Không tìm thấy tài khoản nguyen.van.a@coreandskill.vn");

        redacted.ShouldNotBeNull();
        redacted.ShouldNotContain("nguyen.van.a@");
        redacted.ShouldContain("@coreandskill.vn");
        redacted.ShouldBe("Không tìm thấy tài khoản n***@coreandskill.vn");
    }

    // Nhiều bí mật trong MỘT thông điệp: che cái đầu rồi bỏ cái sau là lỗi kinh điển của vòng lặp
    // một-lần-thoát.
    [Fact]
    public void Detector_Redact_RemovesEveryOccurrence_NotOnlyTheFirst()
    {
        var redacted = SensitiveTextRedactor.Redact("matKhau='Abc@12345', apiKey='K-999', pwd='Zzz@1'");

        redacted.ShouldNotBeNull();
        redacted.ShouldNotContain("Abc@12345");
        redacted.ShouldNotContain("K-999");
        redacted.ShouldNotContain("Zzz@1");
    }

    // ===== Ca ĐỐI — chiều CHE THỪA =====
    //
    // Nếu ba khối dưới đây đỏ thì bộ lọc đang xoá đúng phần làm báo cáo lỗi có ích, và người sửa sẽ
    // bị đẩy đi gỡ bộ lọc thay vì thu hẹp nó.

    // Thông điệp PHỔ BIẾN NHẤT của trình duyệt có nhắc tên trường mà KHÔNG mang giá trị nào.
    [Theory]
    [InlineData("Cannot read properties of undefined (reading 'matKhau')")]
    [InlineData("matKhau is not defined")]
    [InlineData("Thiếu trường password")]
    public void Redact_KeepsMessage_WhenSensitiveNameHasNoValueBesideIt(string text)
        => SensitiveTextRedactor.Redact(text).ShouldBe(text);

    // Ranh giới TỪ: tên nhạy cảm nằm LỌT trong một từ dài hơn thì không phải tên đó.
    [Theory]
    [InlineData("tokenizer: dong 12")]
    [InlineData("pinned: true")]
    [InlineData("healthCheck: ok")]
    [InlineData("passenger: 3")]
    [InlineData("phienBanApp: 2026.09.10-a1b2c3d")]
    public void Redact_KeepsValue_WhenSensitiveNameIsOnlyPartOfALongerWord(string text)
        => SensitiveTextRedactor.Redact(text).ShouldBe(text);

    // Hình dạng THẬT của hai field mà endpoint báo lỗi nhận nhiều nhất. Đỏ ở đây nghĩa là mọi báo
    // cáo lỗi đều về log dưới dạng đã bị xoá.
    [Theory]
    [InlineData("TypeError")]
    [InlineData("/phieu/danh-sach")]
    [InlineData("2026.09.10-a1b2c3d")]
    [InlineData("at PhieuListPage.hienThi (main-A7F2.js:1:2481)")]
    [InlineData("at Object.dispatch (vendor-9C31.js:24:1170)\nat run (main-A7F2.js:1:99)")]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/140.0.0.0")]
    public void Redact_LeavesOrdinaryDiagnosticTextUntouched(string text)
        => SensitiveTextRedactor.Redact(text).ShouldBe(text);

    // ===== Biên =====

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Redact_PassesThroughNullAndEmpty(string? text)
        => SensitiveTextRedactor.Redact(text).ShouldBe(text);

    [Fact]
    public void Redact_HandlesTextWithNoLetterOrDigit()
        => SensitiveTextRedactor.Redact("--- ??? ---").ShouldBe("--- ??? ---");

    // Tên dài phải thắng tên ngắn: `passphrase` chứa `pass`, và nếu `pass` khớp trước thì vị trí kết
    // thúc rơi vào giữa từ, phép tìm dấu phân cách trượt, và giá trị đi thẳng vào log.
    [Fact]
    public void Redact_PrefersLongerName_WhenOneNameContainsAnother()
        => SensitiveTextRedactor.Redact("passphrase: bi-mat").ShouldBe("passphrase: ***");

    // Chuỗi mở mà không đóng: che tới hết văn bản, KHÔNG bỏ qua. Bỏ qua là để lọt trọn giá trị.
    [Fact]
    public void Redact_MasksToEndOfText_WhenQuotedValueIsNeverClosed()
        => SensitiveTextRedactor.Redact("matKhau: 'Abc@12345").ShouldBe("matKhau: '***");

    // Dấu phân cách đứng cuối văn bản, không còn giá trị nào phía sau.
    [Theory]
    [InlineData("matKhau:")]
    [InlineData("matKhau: ")]
    [InlineData("matKhau")]
    [InlineData("matKhau: ''")]
    public void Redact_DoesNothing_WhenThereIsNoValueAfterSeparator(string text)
        => SensitiveTextRedactor.Redact(text).ShouldBe(text);

    // Tên nhạy cảm NẰM TRONG một giá trị vừa bị che thì không được kéo theo một lần che thứ hai
    // chồng lên vùng đã che — vòng lặp phải bước qua, không được lùi.
    [Fact]
    public void Redact_DoesNotDoubleMask_WhenOneSecretContainsAnotherName()
    {
        var redacted = SensitiveTextRedactor.Redact("token: 'password=Abc@12345'");

        redacted.ShouldBe("token: '***'");
    }
}
