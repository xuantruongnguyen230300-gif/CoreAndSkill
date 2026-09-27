using CoreAndSkill.Core.Application.Common.Cqrs;
using CoreAndSkill.Core.Application.Export;

namespace CoreAndSkill.Core.Application.Users;

// GET /api/v1/core/users/export — khuôn ở docs/contracts/exports.md §1. Nhận CÙNG tập tham số lọc và
// sắp xếp với GetUsersListQuery, trừ Page/PageSize (xuất là xuất cả tập), thêm Format.
//
// Là ICommand (không phải IQuery) có chủ đích: xuất GHI một dòng nhật ký kiểm toán, nên phải nằm trong
// giao dịch của TransactionBehavior. Việc ĐỌC và GHI TỆP diễn ra sau, ở hàm ExportFile.WriteToAsync.
public sealed record ExportUsersCommand(
    string Format = ExportFormats.Csv,
    string? SortBy = null,
    bool SortDescending = false,
    string? SearchText = null,
    Guid? RoleId = null,
    string? Status = null) : ICommand<ExportFile>;
