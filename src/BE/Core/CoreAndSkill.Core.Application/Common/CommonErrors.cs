using CoreAndSkill.Core.Domain.Common;

namespace CoreAndSkill.Core.Application.Common;

// Catalog tập trung — mã không được dựng từ chuỗi literal ngoài catalog (luật R2, docs/quy-uoc/be-cqrs-handler.md §7.1).
public static class CommonErrors
{
    // Mã GỐC của envelope khi validator trượt — luôn kèm fieldErrors (contracts/auth.md §11). Chưa dùng ở B0
    // (ValidationBehavior thuộc B1+), khai trước vì Unexpected cần đứng cạnh nhóm mã dùng chung.
    public static readonly Error ValidationFailed = new(
        "CORE.VALIDATION.FAILED", "Dữ liệu gửi lên không hợp lệ.", ErrorType.Validation);

    // Năm mã dưới CHỈ xuất hiện ở fieldErrors[<Field>][].code — validator của Core và mọi module
    // dùng lại qua WithErrorCode (be-cqrs-handler.md §7.1).
    public static readonly Error Required = new(
        "CORE.VALIDATION.REQUIRED", "Trường này bắt buộc.", ErrorType.Validation);

    public static readonly Error MaxLength = new(
        "CORE.VALIDATION.MAX_LENGTH", "Tối đa {MaxLength} ký tự.", ErrorType.Validation);

    public static readonly Error MinLength = new(
        "CORE.VALIDATION.MIN_LENGTH", "Tối thiểu {MinLength} ký tự.", ErrorType.Validation);

    public static readonly Error Format = new(
        "CORE.VALIDATION.FORMAT", "Không đúng định dạng.", ErrorType.Validation);

    // Trần số phần tử của một danh sách — rule MaximumItems (ListRuleBuilderExtensions) gắn mã này và tham số MaxItems.
    public static readonly Error MaxItems = new(
        "CORE.VALIDATION.MAX_ITEMS", "Tối đa {MaxItems} phần tử.", ErrorType.Validation);

    // CORE.SYSTEM.UNEXPECTED CHỈ IExceptionHandler phát (be-api-controller.md §2.4) — handler không trả.
    // CORE.CONCURRENCY.CONFLICT phần lớn cũng do IExceptionHandler phát, từ DbUpdateConcurrencyException của lượt ghi qua
    // DbContext/IUnitOfWork. Ngoại lệ — trả thẳng qua Result: (1) ghi qua UserManager/RoleManager — store của Identity
    // tự nuốt DbUpdateConcurrencyException, nên phép so version trước khi ghi và IdentityConcurrency.DetectConflict sau khi
    // ghi trả mã này (be-api-controller.md §2.4; wiki-core/be/06-concurrency-control.md §6.1); (2) phép so version ở
    // handler đổi vai trò, chạy trước mọi luật khác (docs/adr/0082-gan-vai-tro-dung-token-cua-tai-khoan.md).
    public static readonly Error ConcurrencyConflict = new(
        "CORE.CONCURRENCY.CONFLICT", "Bản ghi đã bị thay đổi bởi người khác. Tải lại rồi thử lại.", ErrorType.Conflict);

    public static readonly Error Unexpected = new(
        "CORE.SYSTEM.UNEXPECTED", "Lỗi hệ thống. Vui lòng thử lại; nếu lặp lại, báo mã theo dõi cho quản trị.", ErrorType.Unexpected);

    // Dùng khi HANDLER (không phải RequirePermissionFilter) cần trả 403 do một điều kiện phụ thuộc
    // dữ liệu mà mức khai báo tĩnh của S11 không biểu diễn được — ví dụ: quyền chỉ đòi khi MỘT
    // trường khác của payload khác rỗng (docs/contracts/users.md §5 — core.user.role.assign chỉ cần
    // khi roleIds khác rỗng). Khai MỘT lần ở đây: SecurityErrors.Forbidden ở Core.Web (đường ForbidResult của
    // filter/cookie event) trỏ về trường này — hai đường phát, một khai báo, một mã trên dây
    // (be-api-controller.md §7.4, luật R3). Chiều tham chiếu là Core.Web → Core.Application (luật A2).
    public static readonly Error Forbidden = new(
        "CORE.AUTH.FORBIDDEN", "Không có quyền thực hiện thao tác này.", ErrorType.Forbidden);
}
