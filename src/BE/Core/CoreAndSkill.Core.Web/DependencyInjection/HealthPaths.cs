namespace CoreAndSkill.Core.Web.DependencyInjection;

// Đường health check của Core — MỘT chỗ khai, dùng cho cả MapHealthChecks lẫn RequestLogMiddleware (bỏ qua log).
internal static class HealthPaths
{
    public const string Root = "/health";
    public const string Live = Root + "/live";
    public const string Ready = Root + "/ready";
}
