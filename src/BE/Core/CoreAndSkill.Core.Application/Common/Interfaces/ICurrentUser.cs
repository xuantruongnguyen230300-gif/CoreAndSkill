namespace CoreAndSkill.Core.Application.Common.Interfaces;

// Danh tính của request — định nghĩa gốc, docs/quy-uoc/be-architecture.md §1.1.
// Chỉ có giá trị SAU KHI đã xác thực. Hiện thực HttpContextCurrentUser ở Core.Web — chỗ DUY NHẤT
// đọc HttpContext để lấy danh tính. Đọc theo thứ tự: IExecutionContextScope.Current → HttpContext →
// không có gì.
public interface ICurrentUser
{
    Guid? UserId { get; }
    string? UserName { get; }
}
