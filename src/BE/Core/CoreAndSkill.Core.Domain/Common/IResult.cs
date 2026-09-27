namespace CoreAndSkill.Core.Domain.Common;

public interface IResult
{
    bool IsSuccess { get; }
    bool IsFailure { get; }
    Error? Error { get; }
}

// static abstract (C# 11) — cho pipeline behavior dựng Result thất bại đúng kiểu trả về, không reflection.
// Xem docs/quy-uoc/be-cqrs-handler.md §5.4.
public interface IResultFactory<out TSelf> where TSelf : IResultFactory<TSelf>
{
    static abstract TSelf FromError(Error error);
}
