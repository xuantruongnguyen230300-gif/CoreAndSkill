namespace CoreAndSkill.Core.Web.Permissions;

// Một trong BA mức khai báo phân quyền endpoint (luật S11) — docs/quy-uoc/be-api-controller.md §4.2,
// docs/adr/0024-ba-muc-khai-bao-phan-quyen-endpoint.md. Dùng cho endpoint CỦA BẢN THÂN: hồ sơ, đổi
// mật khẩu, `me`, đăng xuất. Không cần filter riêng — [Authorize] của ApiControllerBase đã đòi đăng
// nhập; chuỗi lý do bắt buộc để người review đọc được ngay tại chỗ vì sao endpoint không đòi quyền.
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class AuthenticatedOnlyAttribute(string reason) : Attribute
{
    public string Reason { get; } = reason;
}
