using CoreAndSkill.Core.Application.Jobs;
using CoreAndSkill.Core.Web.Http;
using CoreAndSkill.Core.Web.Permissions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CoreAndSkill.Core.Web.Controllers;

// docs/contracts/jobs.md. FE hỏi theo chu kỳ, không giữ kết nối; dừng hỏi khi việc `succeeded` /
// `failed` / `cancelled`. Không có endpoint huỷ hay chạy lại ở v1.
[ApiController]
[Route(CoreRoutes.Jobs)]
public sealed class JobsController(ISender mediator) : ApiControllerBase
{
    [HttpGet("{id:guid}")]
    [AuthenticatedOnly("Việc nền của chính người đã khởi tạo — handler kiểm, không có khoá quyền riêng")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => HandleResult(await mediator.Send(new GetJobQuery(id), ct));
}
