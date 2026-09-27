using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace CoreAndSkill.Core.Web.Controllers;

// docs/contracts/diagnostics.md: endpoint thử của B0 chỉ có NGOÀI Production. Gỡ DiagnosticsController khỏi mô hình
// ứng dụng — không một tuyến nào được đăng ký — thay vì để action tự trả 404: tuyến không tồn tại thì mọi verb, mọi
// query đều rơi vào đúng đường "không có tuyến" (CORE.ROUTE.NOT_FOUND) như một đường dẫn bịa ra, không có 405 hay
// hình dạng riêng nào để lộ rằng endpoint có ở đó. Chỉ gắn khi môi trường là Production (AddCoreWeb).
internal sealed class DiagnosticsOutsideProductionConvention : IApplicationModelConvention
{
    public void Apply(ApplicationModel application)
    {
        foreach (var controller in application.Controllers.Where(c => c.ControllerType == typeof(DiagnosticsController)).ToList())
            application.Controllers.Remove(controller);
    }
}
