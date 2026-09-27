namespace CoreAndSkill.Core.Domain.Common;

public class Result : IResult, IResultFactory<Result>
{
    protected Result(bool isSuccess, Error? error)
    {
        if (isSuccess == (error is not null))
            throw new InvalidOperationException("Result thành công không được mang Error, và ngược lại.");

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error? Error { get; }

    public static Result Success() => new(true, null);
    public static Result Failure(Error error) => new(false, error);

    public static Result<TValue> Success<TValue>(TValue value) => new(value, true, null);
    public static Result<TValue> Failure<TValue>(Error error) => new(default, false, error);

    static Result IResultFactory<Result>.FromError(Error error) => Failure(error);
}
