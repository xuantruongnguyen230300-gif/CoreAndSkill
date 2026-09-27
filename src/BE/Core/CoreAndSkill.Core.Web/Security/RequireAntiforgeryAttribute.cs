namespace CoreAndSkill.Core.Web.Security;

// Dấu tường minh cho một action dùng method AN TOÀN (GET/HEAD) nhưng có tác dụng phụ — hôm nay là endpoint xuất theo khuôn
// docs/contracts/exports.md §1, vì mỗi lần xuất ghi một dòng nhật ký kiểm toán mang tên người gọi. Cookie SameSite=Lax
// vẫn đi theo điều hướng cấp cao nhất xuyên site và trình duyệt không gắn Origin cho điều hướng GET, nên một trang lạ dẫn
// người dùng tới URL đó là tạo được một dòng audit giả. Action mang dấu này bị AntiforgeryValidationMiddleware kiểm cả hai
// lớp của docs/quy-uoc/be-api-controller.md §7.2 (Origin + X-XSRF-TOKEN) y như lệnh ghi. Quyết định:
// docs/adr/0062-endpoint-xuat-kiem-token-chong-gia-mao-nhu-lenh-ghi.md
//
// Middleware đọc dấu qua metadata của endpoint (MVC đưa attribute của action vào EndpointMetadata), KHÔNG nhận diện bằng
// đường dẫn hay tên action. Chỉ THÊM kiểm, không bớt: gắn lên một action ghi (POST/PUT/PATCH/DELETE) là thừa, vô hại.
// Module gắn dấu này lên action xuất của mình; ArchTest EveryGetActionWithSideEffects_RequiresAntiforgery canh mọi action
// xuất và mọi GET gửi command / ghi nhật ký kiểm toán trong Core.
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class RequireAntiforgeryAttribute : Attribute;
