using CoreAndSkill.Core.Application.Notifications;
using CoreAndSkill.Core.Web.Http;
using CoreAndSkill.Core.Web.Permissions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CoreAndSkill.Core.Web.Controllers;

// docs/contracts/notifications.md. MỌI action chỉ chạm thông báo CỦA CHÍNH NGƯỜI GỌI — định danh lấy từ
// phiên (ICurrentUser trong handler), không từ tham số. KHÔNG có endpoint tạo hay xoá thông báo: thông
// báo sinh ra từ sự kiện nghiệp vụ đi qua Outbox, dọn dẹp đi theo chính sách vòng đời dữ liệu.
[ApiController]
[Route(CoreRoutes.Notifications)]
public sealed class NotificationsController(ISender mediator) : ApiControllerBase
{
    [HttpGet]
    [AuthenticatedOnly("Thông báo của chính người gọi — định danh lấy từ phiên")]
    public async Task<IActionResult> GetList([FromQuery] GetNotificationsQuery query, CancellationToken ct)
        => HandleResult(await mediator.Send(query, ct));

    [HttpGet("unread-count")]
    [AuthenticatedOnly("Số thông báo chưa đọc của chính người gọi")]
    public async Task<IActionResult> GetUnreadCount(CancellationToken ct)
        => HandleResult(await mediator.Send(new GetUnreadNotificationCountQuery(), ct));

    [HttpPut("{id:guid}/read")]
    [AuthenticatedOnly("Chỉ đánh dấu thông báo của chính người gọi — handler kiểm chủ bản ghi")]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken ct)
        => HandleResult(await mediator.Send(new MarkNotificationReadCommand(id), ct));

    [HttpPut("read-all")]
    [AuthenticatedOnly("Đánh dấu toàn bộ thông báo của chính người gọi")]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct)
        => HandleResult(await mediator.Send(new MarkAllNotificationsReadCommand(), ct));
}
