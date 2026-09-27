namespace CoreAndSkill.Core.Domain.Common;

// Bảng ánh xạ sang HTTP status: docs/quy-uoc/be-api-controller.md §1.1 — đúng MỘT chỗ, ở Core.Web/Http/ResultToHttpMapper.cs.
public enum ErrorType
{
    Validation,     // input sai — tính được từ payload, không cần DB
    NotFound,       // resource chính của route không tồn tại
    Conflict,       // xung đột trạng thái: trùng unique, concurrency
    Forbidden,      // đã đăng nhập nhưng thiếu quyền
    BusinessRule,   // vi phạm quy tắc nghiệp vụ, cần đọc dữ liệu mới biết
    Unauthorized,   // chưa đăng nhập / phiên hết hạn — CHỈ hạ tầng phát (luật R9)
    Unexpected,     // lỗi hệ thống ngoài dự kiến — CHỈ IExceptionHandler phát; handler và Domain không bao giờ trả
}
