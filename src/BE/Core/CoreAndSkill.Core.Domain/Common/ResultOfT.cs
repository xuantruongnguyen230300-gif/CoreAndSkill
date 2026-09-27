namespace CoreAndSkill.Core.Domain.Common;

public sealed class Result<TValue> : Result, IResultFactory<Result<TValue>>
{
    private readonly TValue? _value;

    internal Result(TValue? value, bool isSuccess, Error? error) : base(isSuccess, error)
        => _value = value;

    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Không đọc được Value của một Result thất bại.");

    public static implicit operator Result<TValue>(TValue value) => Success(value);

    static Result<TValue> IResultFactory<Result<TValue>>.FromError(Error error) => Failure<TValue>(error);
}
