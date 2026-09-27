using System.Collections.ObjectModel;

namespace CoreAndSkill.Core.Domain.Common;

public sealed record Error(string Code, string MessageTemplate, ErrorType Type)
{
    public IReadOnlyDictionary<string, string> Params { get; init; }
        = ReadOnlyDictionary<string, string>.Empty;

    public IReadOnlyDictionary<string, IReadOnlyList<FieldError>> FieldErrors { get; init; }
        = ReadOnlyDictionary<string, IReadOnlyList<FieldError>>.Empty;

    public Error WithParams(params ReadOnlySpan<(string Name, object? Value)> args)
    {
        var map = new Dictionary<string, string>(args.Length, StringComparer.Ordinal);
        foreach (var (name, value) in args)
            map[name] = value?.ToString() ?? string.Empty;

        return this with { Params = map.AsReadOnly() };
    }

    public Error WithFieldErrors(IReadOnlyDictionary<string, IReadOnlyList<FieldError>> fieldErrors)
        => this with { FieldErrors = fieldErrors };
}
