using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace CoreAndSkill.Core.Application.Common.Logging;

// Bộ lọc trường nhạy cảm cho VĂN BẢN TỰ DO sắp đi vào log — docs/wiki-core/be/07-observability.md
// §5 ("Không được log cái gì") và docs/contracts/client-errors.md mục Ghi chú.
//
// VÌ SAO KHÔNG PHẢI BỘ LỌC THEO TÊN THUỘC TÍNH CỦA THƯ VIỆN LOG. §5 mô tả một bộ lọc "che theo
// danh sách TÊN TRƯỜNG" ở tầng cấu hình log: nó nhìn thấy thuộc tính có cấu trúc (`{MatKhau}`) và
// che giá trị của thuộc tính mang tên trong danh sách. Bộ lọc đó KHÔNG cứu được ca ở §5 "Ba đường
// rò hay gặp" hàng thứ ba — *thông điệp lỗi của thư viện ngoài* — vì ở đó tên trường và giá trị
// nằm LẪN trong MỘT chuỗi do bên ngoài soạn:
//
//     "Invalid value for field matKhau: 'Abc@12345'"
//
// Với bộ lọc theo tên thuộc tính, cả chuỗi trên là giá trị của MỘT thuộc tính tên `Message` —
// không có tên nào trong danh sách, nên nó đi thẳng vào log ở mức Error. Đó là lý do
// client-errors.md nói rõ "nội dung do trình duyệt soạn nên danh sách đó không tự áp".
//
// PHÉP ĐÁNH ĐỔI ĐÃ CHỌN, nói ra để không ai tưởng bộ lọc này che được mọi thứ:
//   • che ĐƯỢC: giá trị đứng sau một tên trường nhạy cảm và một dấu phân cách (`:` hoặc `=`), ở cả
//     dạng có nháy lẫn không nháy; token dạng JWT và `Bearer …`; phần tên của địa chỉ thư.
//   • KHÔNG che được: một giá trị bí mật đứng MỘT MÌNH, không tên trường nào bên cạnh và không
//     mang hình dạng nhận ra được. Không bộ lọc văn bản nào làm được việc đó — hàng rào cho ca ấy
//     là ràng buộc phía FE ở docs/wiki-core/fe/10-observability.md §4.1 (không gửi nội dung form).
//     Bộ lọc này là LỚP THỨ HAI, không phải lớp duy nhất.
//   • che THỪA thì chấp nhận, che THIẾU thì không: một thông điệp mất vài từ vẫn điều tra được;
//     một mật khẩu đã vào log thì không rút lại được. Vì vậy giá trị không nháy bị che tới hết dòng
//     hoặc tới dấu kết thúc mệnh đề, chứ không dừng ở khoảng trắng đầu tiên.
internal static partial class SensitiveTextRedactor
{
    public const string Mask = "***";

    // DANH SÁCH TÊN TRƯỜNG — đây là bản duy nhất, và nó ở trong mã chứ không trong tài liệu:
    // 07-observability.md §5 khai các NHÓM (mật khẩu, token, cookie phiên, số giấy tờ, dữ liệu sức
    // khoẻ, nội dung file tải lên), còn tên cụ thể thì chỉ mã mới kiểm được. Mỗi nhóm dưới đây neo
    // vào một hàng của bảng §5; thêm hàng ở §5 thì thêm tên ở đây.
    //
    // So khớp KHÔNG phân biệt hoa thường, dấu tiếng Việt, và dấu ngăn từ: `matKhau`, `mat_khau`,
    // `MAT_KHAU`, `mật khẩu` là một. Xem Normalize.
    //
    // KHÔNG có "email" và "số điện thoại" trong danh sách này — §5 xếp chúng vào bảng "log có kiểm
    // soát … che một phần", không phải bảng "tuyệt đối không log". Che trọn địa chỉ thư làm mất
    // luôn khả năng biết sự cố xảy ra với ai. Phần tên của địa chỉ thư được che riêng, xem
    // EmailPattern. Số điện thoại KHÔNG che: mọi phép dò theo hình dạng số sẽ nuốt số dòng, số cột
    // và dấu thời gian trong stack trace — che thừa ở đó là xoá đúng thứ duy nhất làm stack trace
    // có ích (ranh giới đã biết, không phải chỗ sót).
    private static readonly string[] SensitiveFieldNames =
    [
        // §5 — "Mật khẩu, ở mọi dạng". Khớp cả `newPassword`, `matKhauMoi`, `xacNhanMatKhau`.
        "password", "passwd", "pwd", "pass", "passphrase", "matkhau",

        // §5 — "Token, khoá API, chuỗi kết nối, bí mật ký". `token` khớp cả `accessToken`,
        // `refreshToken`, `csrfToken`; `secret` khớp cả `clientSecret`, `signingSecret`.
        "token", "secret", "bimat", "apikey", "khoaapi", "privatekey", "khoariengtu",
        "signingkey", "connectionstring", "chuoiketnoi", "credential", "credentials",
        "authorization", "otp", "maxacthuc", "verificationcode",

        // §5 — "Giá trị cookie phiên".
        "cookie", "setcookie", "session", "sessionid", "phienlamviec",

        // §5 — "Số giấy tờ tuỳ thân, số thẻ".
        "cmnd", "cccd", "socmnd", "socccd", "sogiayto", "nationalid", "ssn",
        "cardnumber", "sothe", "cvv", "pin",

        // §5 — "Dữ liệu sức khoẻ". KHÔNG dùng "health": nó khớp `healthCheck`, một khoá vô hại và
        // xuất hiện thường xuyên trong chính hệ này.
        "suckhoe", "benhan", "medicalrecord", "diagnosis",

        // §5 — "Nội dung file người dùng tải lên".
        "filecontent", "noidungfile", "filebase64",
    ];

    private static readonly HashSet<string> NormalizedNames =
        [.. SensitiveFieldNames.Select(NormalizeName)];

    private static readonly int ShortestName = NormalizedNames.Min(n => n.Length);
    private static readonly int LongestName = NormalizedNames.Max(n => n.Length);

    /// <summary>Che phần nhạy cảm trong một đoạn văn bản tự do trước khi ghi log.</summary>
    public static string? Redact(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var afterNames = RedactNamedValues(text);

        // Hình dạng tự nhận ra được, KHÔNG cần tên trường đứng cạnh. `Bearer` phải chạy trước JWT,
        // nếu không thì token bị che xong còn trơ lại chữ "Bearer" đứng một mình.
        var afterBearer = BearerPattern().Replace(afterNames, "Bearer " + Mask);
        var afterJwt = JwtPattern().Replace(afterBearer, Mask);

        return EmailPattern().Replace(afterJwt, "$1" + Mask + "$2");
    }

    private static string RedactNamedValues(string text)
    {
        var (normalized, origin, segmentStart) = Normalize(text);

        if (normalized.Length == 0)
            return text;

        var builder = new StringBuilder(text.Length);
        var copied = 0;

        for (var start = 0; start + ShortestName <= normalized.Length; start++)
        {
            if (!segmentStart[start] || origin[start] < copied)
                continue;

            var matchedEnd = MatchNameAt(normalized, segmentStart, start);

            if (matchedEnd < 0)
                continue;

            var nameEndInText = origin[matchedEnd - 1] + 1;

            // Tên nhạy cảm có thể là TIỀN TỐ của tên trường thật: `matKhauMoi`, `passwordConfirm`,
            // `tokenValue`. Dấu phân cách khi đó đứng sau TRỌN định danh chứ không sau phần khớp,
            // nên thử lần hai ở cuối định danh. Thiếu lần thử này thì `matKhauMoi: 'Abc@12345'` đi
            // thẳng vào log — đo được. Biên phải ở MatchNameAt vẫn giữ nguyên, nên `tokenizer` vẫn
            // không khớp: `token` ở đó không kết thúc ở một ranh giới từ.
            if (!TryFindValueSpan(text, nameEndInText, out var valueStart, out var valueEnd)
             && !TryFindValueSpan(text, IdentifierEnd(text, nameEndInText), out valueStart, out valueEnd))
                continue;

            builder.Append(text, copied, valueStart - copied).Append(Mask);
            copied = valueEnd;
        }

        return copied == 0
            ? text
            : builder.Append(text, copied, text.Length - copied).ToString();
    }

    // Tên dài khớp trước tên ngắn: `passphrase` phải thắng `pass`, nếu không thì vị trí kết thúc
    // rơi vào giữa một từ và phép tìm dấu phân cách ngay sau đó luôn trượt.
    private static int MatchNameAt(string normalized, bool[] segmentStart, int start)
    {
        var maxLength = Math.Min(LongestName, normalized.Length - start);

        for (var length = maxLength; length >= ShortestName; length--)
        {
            var end = start + length;

            // Biên phải: tên phải kết thúc đúng ở một ranh giới từ, nếu không `pin` sẽ khớp trong
            // `pinned` và `token` sẽ khớp trong `tokenizer`.
            if (end != normalized.Length && !segmentStart[end])
                continue;

            if (NormalizedNames.Contains(normalized[start..end]))
                return end;
        }

        return -1;
    }

    // Giá trị của một tên trường là phần đứng sau dấu phân cách. KHÔNG có dấu phân cách thì KHÔNG
    // che gì cả — và đó là chủ đích, không phải chỗ sót: thông điệp phổ biến nhất của trình duyệt là
    // `Cannot read properties of undefined (reading 'matKhau')`, ở đó tên trường xuất hiện mà không
    // có giá trị nào bên cạnh. Che nó đi là xoá đúng dòng báo lỗi hữu ích nhất mà không giấu được gì.
    private static bool TryFindValueSpan(string text, int from, out int valueStart, out int valueEnd)
    {
        valueStart = 0;
        valueEnd = 0;

        var position = from;

        SkipWhitespace(text, ref position);

        // Nháy đóng của một khoá JSON: `{"matKhau": "…"}` — tên nằm TRONG nháy nên vị trí kết thúc
        // của tên dừng ngay trước nháy đóng.
        if (position < text.Length && text[position] is '"' or '\'' or '`')
            position++;

        SkipWhitespace(text, ref position);

        if (position >= text.Length || text[position] is not (':' or '='))
            return false;

        position++;

        // `==`, `=>`, `: =` — mọi biến thể của dấu gán/so sánh vẫn dẫn tới một giá trị.
        while (position < text.Length && text[position] is '=' or '>' or ':')
            position++;

        SkipWhitespace(text, ref position);

        if (position >= text.Length)
            return false;

        if (text[position] is '"' or '\'' or '`')
        {
            var quote = text[position];
            var close = text.IndexOf(quote, position + 1);

            valueStart = position + 1;
            valueEnd = close < 0 ? text.Length : close;

            return valueEnd > valueStart;
        }

        var scan = position;

        // Che tới hết dòng hoặc tới dấu kết thúc mệnh đề — KHÔNG dừng ở khoảng trắng đầu tiên.
        // Xem phép đánh đổi ở đầu lớp: `Authorization: Bearer eyJ…` mà dừng ở khoảng trắng là che
        // đúng chữ "Bearer" rồi để nguyên token.
        while (scan < text.Length && text[scan] is not (',' or ';' or ')' or '}' or ']' or '\n' or '\r'))
            scan++;

        while (scan > position && char.IsWhiteSpace(text[scan - 1]))
            scan--;

        valueStart = position;
        valueEnd = scan;

        return valueEnd > valueStart;
    }

    // Cuối định danh chứa vị trí này: chữ, số, `_` và `-` đều là ký tự nối tên trường trong mã FE
    // (`mat_khau_moi`, `Set-Cookie`).
    private static int IdentifierEnd(string text, int from)
    {
        var position = from;

        while (position < text.Length
            && (char.IsLetterOrDigit(ToBaseChar(text[position])) || text[position] is '_' or '-'))
            position++;

        return position;
    }

    private static void SkipWhitespace(string text, ref int position)
    {
        while (position < text.Length && char.IsWhiteSpace(text[position]))
            position++;
    }

    // Đưa văn bản về dạng so khớp được: bỏ dấu tiếng Việt, bỏ mọi ký tự không phải chữ/số, hạ chữ
    // thường. Giữ kèm HAI mảng song song, vì thiếu mảng nào cũng hỏng:
    //   • origin[k]       — vị trí trong văn bản GỐC của ký tự chuẩn hoá thứ k, để che đúng chỗ;
    //   • segmentStart[k] — ký tự thứ k có mở đầu một "từ" không. Ranh giới từ ở đây gồm cả kiểu
    //     viết hoa lạc đà (`matKhau` là hai từ), vì tên trường trong mã FE viết theo kiểu đó.
    private static (string Normalized, int[] Origin, bool[] SegmentStart) Normalize(string text)
    {
        var builder = new StringBuilder(text.Length);
        var origin = new List<int>(text.Length);
        var segmentStart = new List<bool>(text.Length);
        var previousWasAlphanumeric = false;
        var previousWasLowerOrDigit = false;

        for (var i = 0; i < text.Length; i++)
        {
            var baseChar = ToBaseChar(text[i]);

            if (!char.IsLetterOrDigit(baseChar))
            {
                previousWasAlphanumeric = false;
                previousWasLowerOrDigit = false;
                continue;
            }

            var isSegmentStart = !previousWasAlphanumeric
                              || (char.IsUpper(baseChar) && previousWasLowerOrDigit);

            builder.Append(char.ToLowerInvariant(baseChar));
            origin.Add(i);
            segmentStart.Add(isSegmentStart);

            previousWasAlphanumeric = true;
            previousWasLowerOrDigit = char.IsLower(baseChar) || char.IsDigit(baseChar);
        }

        return (builder.ToString(), [.. origin], [.. segmentStart]);
    }

    // `đ`/`Đ` KHÔNG phân rã được bằng FormD — chúng là ký tự riêng, không phải `d` cộng dấu. Bỏ sót
    // hai ký tự này thì mọi tên trường tiếng Việt có `đ` trượt khỏi danh sách một cách im lặng.
    private static char ToBaseChar(char c)
    {
        if (c is 'đ' or 'Đ')
            return c == 'đ' ? 'd' : 'D';

        foreach (var decomposed in c.ToString().Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(decomposed) != UnicodeCategory.NonSpacingMark)
                return decomposed;
        }

        return c;
    }

    private static string NormalizeName(string name)
    {
        var builder = new StringBuilder(name.Length);

        foreach (var c in name)
        {
            var baseChar = ToBaseChar(c);

            if (char.IsLetterOrDigit(baseChar))
                builder.Append(char.ToLowerInvariant(baseChar));
        }

        return builder.ToString();
    }

    [GeneratedRegex(@"\bBearer\s+[^\s'""`,;)\]}]+", RegexOptions.IgnoreCase)]
    private static partial Regex BearerPattern();

    // JWT: ba phần ngăn bằng dấu chấm, phần đầu luôn bắt đầu bằng `eyJ` (base64url của `{"`).
    [GeneratedRegex(@"\beyJ[A-Za-z0-9_-]{4,}\.[A-Za-z0-9_-]{4,}(?:\.[A-Za-z0-9_-]*)?")]
    private static partial Regex JwtPattern();

    // Che PHẦN TÊN, giữ ký tự đầu và trọn tên miền — §5 bảng "log có kiểm soát": che một phần, để
    // vẫn đối chiếu được với người dùng khi điều tra.
    [GeneratedRegex(@"([A-Za-z0-9._%+-])[A-Za-z0-9._%+-]*(@[A-Za-z0-9-]+(?:\.[A-Za-z0-9-]+)+)")]
    private static partial Regex EmailPattern();
}
