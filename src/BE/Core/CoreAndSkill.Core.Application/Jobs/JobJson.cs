using System.Text.Json;
using CoreAndSkill.Core.Domain.Common;

namespace CoreAndSkill.Core.Application.Jobs;

// Hình dạng jsonb của core.job.error — { code, message, messageParams } theo hợp đồng thông điệp lỗi
// (be-cqrs-handler.md §8.1, contracts/jobs.md §1). BE giữ mã + tham số; `message` là câu dự phòng
// dev-facing, client không bị buộc hiển thị.
public static class JobJson
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static string ErrorOf(Error error)
        => JsonSerializer.Serialize(new
        {
            code = error.Code,
            message = MessageTemplateRenderer.Render(error.MessageTemplate, error.Params),
            messageParams = error.Params,
        }, Options);
}
