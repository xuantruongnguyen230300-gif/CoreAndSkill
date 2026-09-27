using System.Globalization;
using System.Text.Json;
using CoreAndSkill.Core.Application.Audit;
using CoreAndSkill.Core.Application.Configuration;
using CoreAndSkill.Core.Application.Export;
using CoreAndSkill.Core.Application.Roles;
using CoreAndSkill.Core.Application.Tabular;
using CoreAndSkill.Core.Domain.Common;
using MediatR;
using Microsoft.Extensions.Options;

namespace CoreAndSkill.Core.Application.Users;

// docs/contracts/exports.md §1, docs/wiki-core/be/15-import-export.md §5, luồng N3.
//
// Ba thứ đi cùng nhau ở đây và không tách được:
//   1. ĐẾM TRƯỚC khi dựng tệp — vượt trần thì lỗi NGAY, không tiêu tài nguyên cho việc chắc chắn hỏng.
//   2. CÙNG chỗ dựng truy vấn với màn danh sách (IUserQueryService.SearchAsync / CountAsync / StreamAsync
//      đều đi qua MỘT bộ lọc) — nếu không, tệp xuất sẽ không khớp thứ người dùng đang nhìn.
//   3. GHI NHẬT KÝ KIỂM TOÁN: ai xuất, bộ lọc nào, bao nhiêu dòng (§5.5). Xuất là đường dữ liệu rời khỏi
//      hệ thống nên đây là dòng nhật ký có hậu quả nhất trong nhóm.
//
// Quyền xuất `core.user.export` là quyền RIÊNG (không mặc định đi kèm quyền xem) — khai ở controller.
internal sealed class ExportUsersCommandHandler(
    IUserQueryService users,
    IRoleQueryService roles,
    ITabularWriterFactory writerFactory,
    IAuditTrail auditTrail,
    IOptions<CoreExportOptions> exportOptions,
    TimeProvider timeProvider)
    : IRequestHandler<ExportUsersCommand, Result<ExportFile>>
{
    // Tiêu đề cột là KHOÁ kỹ thuật ổn định, không phải câu hiển thị (BE không ghép câu). Tệp xuất dùng
    // ngôn ngữ nào — của người xuất hay của đơn vị — chưa file tài liệu nào chốt (luồng N3 §6).
    private static readonly string[] Headers = ["UserName", "FullName", "Email", "Roles", "Status", "CreatedAt"];

    public async Task<Result<ExportFile>> Handle(ExportUsersCommand command, CancellationToken ct)
    {
        // roleId không tồn tại là LỖI, không phải "tệp rỗng" — cùng luật với danh sách.
        if (command.RoleId is { } roleId && !await roles.ExistsAsync(roleId, ct))
            return Result.Failure<ExportFile>(UserErrors.RoleNotFound.WithParams(("RoleId", roleId)));

        var criteria = new UserSearchCriteria(
            Page: 1,
            PageSize: 1,
            command.SortBy ?? "userName",
            command.SortDescending,
            command.SearchText,
            command.RoleId,
            command.Status);

        var maxRows = exportOptions.Value.MaxRows;
        var rowCount = await users.CountAsync(criteria, ct);
        if (rowCount > maxRows)
            return Result.Failure<ExportFile>(ExportErrors.TooManyRows.WithParams(("RowCount", rowCount), ("MaxRows", maxRows)));

        // Chỉ ghi thứ ĐÃ CHỌN: bộ lọc và số dòng — không nguyên đối tượng đầu vào, không dữ liệu của
        // người dùng bị xuất (luật S13).
        auditTrail.Record(new AuditEntry(
            AuditActions.UserExport,
            TargetType: "core.user",
            TargetId: "export",
            AfterValueJson: JsonSerializer.Serialize(new
            {
                format = command.Format,
                sortBy = criteria.SortBy,
                sortDescending = criteria.SortDescending,
                searchText = criteria.SearchText,
                roleId = criteria.RoleId,
                status = criteria.Status,
                rowCount,
            }, new JsonSerializerOptions(JsonSerializerDefaults.Web))));

        var format = command.Format == ExportFormats.Xlsx ? TabularFormat.Xlsx : TabularFormat.Csv;
        var fileName = string.Create(
            CultureInfo.InvariantCulture,
            $"users-{timeProvider.GetUtcNow():yyyyMMdd'T'HHmmss'Z'}.{command.Format}");

        return new ExportFile(fileName, ExportFormats.ContentType(command.Format), async (output, token) =>
        {
            await using var writer = await writerFactory.CreateAsync(format, output, Headers, token);

            // Đọc theo lô, ghi từng dòng — không nạp toàn bộ kết quả rồi mới ghi. Chặn cứng ở maxRows
            // kể cả khi dữ liệu tăng giữa lúc đếm và lúc ghi.
            await foreach (var user in users.StreamAsync(criteria, maxRows, token))
            {
                await writer.WriteRowAsync(
                [
                    user.UserName,
                    user.FullName,
                    user.Email,
                    string.Join("; ", user.Roles.Select(r => r.Name)),
                    user.IsLocked ? "locked" : "active",
                    user.CreatedAt?.ToString("O", CultureInfo.InvariantCulture),
                ], token);
            }

            await writer.CompleteAsync(token);
        });
    }
}
