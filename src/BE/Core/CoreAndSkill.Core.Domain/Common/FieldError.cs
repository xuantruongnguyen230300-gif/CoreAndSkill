namespace CoreAndSkill.Core.Domain.Common;

public sealed record FieldError(string Code, IReadOnlyDictionary<string, string> Params);
