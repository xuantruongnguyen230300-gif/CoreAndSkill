using System.ComponentModel.DataAnnotations;
using CoreAndSkill.Core.Application.Identity;

namespace CoreAndSkill.Core.Application.Configuration;

// Khoá cấu hình mang tên đăng nhập của một tài khoản SẼ ĐƯỢC TẠO không được là danh tính hệ thống (SystemActor) — cùng luật,
// cùng phép so với validator của các đường tạo tài khoản qua HTTP (ReservedUserNameRuleExtensions). Dùng cho lệnh bootstrap
// (CoreBootstrapOptions): runner kiểm ở đầu lệnh, trước dòng ghi đầu tiên, nên tên sai thì không dòng nào được ghi.
//
// Rỗng / null không phải việc của thuộc tính này ([Required] gánh).
[AttributeUsage(AttributeTargets.Property)]
public sealed class NotReservedUserNameAttribute : ValidationAttribute
{
    public override bool IsValid(object? value) => value is not string userName || !SystemActor.IsReservedUserName(userName);

    public override string FormatErrorMessage(string name)
        => $"{name}: '{SystemActor.UserName}' là danh tính hệ thống (ghi vào created_by / updated_by), không dùng làm tên đăng nhập.";
}
