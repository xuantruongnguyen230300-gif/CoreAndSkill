using CoreAndSkill.Core.Domain.Common;

namespace CoreAndSkill.ArchTests.Support;

// Bộ dò của R4: hỏi một hàm ánh xạ ErrorType → HTTP status xem nó có trả đúng status của bảng cho một giá trị
// không. Hàm ánh xạ truyền vào để ArchTest chạy nó trên ResultToHttpMapper.ToStatusCode thật và Detector_R4_*
// chạy nó trên một hàm giả mang vi phạm cắm sẵn — cùng MỘT đường dò, không chép lại logic (luật T1).
internal static class ErrorTypeHttpMappingScanner
{
    // Trả null khi ánh xạ khớp bảng; ngược lại trả mô tả vi phạm. Hàm ánh xạ ném lỗi (giá trị enum chưa
    // được ánh xạ) cũng là vi phạm, không để lỗi lọt ra ngoài như một lỗi hạ tầng.
    public static string? FindViolation(Func<ErrorType, int> map, ErrorType type, int expectedStatus)
    {
        int actual;
        try
        {
            actual = map(type);
        }
        catch (Exception ex)
        {
            return $"{type}: hàm ánh xạ ném {ex.GetType().Name} thay vì trả status {expectedStatus}";
        }

        return actual == expectedStatus
            ? null
            : $"{type}: ánh xạ ra {actual}, bảng yêu cầu {expectedStatus}";
    }
}
