namespace CoreAndSkill.Core.Application.Diagnostics;

// DTO trên dây — không bao giờ là entity (docs/quy-uoc/be-cqrs-handler.md §11).
public sealed record DiagnosticsProbeResponse(string Message, DateTimeOffset ServerTimeUtc);
