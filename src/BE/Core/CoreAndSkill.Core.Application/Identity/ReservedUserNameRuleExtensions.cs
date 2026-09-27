using CoreAndSkill.Core.Application.Users;
using FluentValidation;

namespace CoreAndSkill.Core.Application.Identity;

// Rule "tên đăng nhập không được là danh tính hệ thống" — dùng chung cho MỌI validator của đường tạo tài khoản
// (CreateUserCommand, CreateTenantCommand, CreateTenantAdminCommand). Điều kiện tính được từ payload nên thuộc validator
// (docs/quy-uoc/be-cqrs-handler.md §6.1): 400 CORE.VALIDATION.FAILED, lý do ở fieldErrors[<ô tên đăng nhập>][] mã
// CORE.USER.USERNAME_RESERVED, không messageParams.
//
// Không làm lộ gì về dữ liệu — kể cả ở tenants.md §6, nơi mọi lỗi tạo tài khoản gộp một mã để chống dò tên: "system" bị
// từ chối ở mọi đơn vị, bất kể database có gì.
//
// Rỗng / null không phải việc của rule này (NotEmpty + Required gánh); chúng đi qua để hai rule không dẫm lên nhau.
public static class ReservedUserNameRuleExtensions
{
    public static IRuleBuilderOptions<T, string> NotReservedUserName<T>(this IRuleBuilder<T, string> rule)
        => rule
            .Must(userName => !SystemActor.IsReservedUserName(userName))
            .WithErrorCode(UserErrors.UsernameReserved.Code);
}
