namespace CoreAndSkill.Core.Domain.Common;

public static class ResultExtensions
{
    public static Result<TOut> Map<TIn, TOut>(this Result<TIn> result, Func<TIn, TOut> selector)
        => result.IsSuccess ? Result.Success(selector(result.Value)) : Result.Failure<TOut>(result.Error!);

    public static async Task<Result<TOut>> MapAsync<TIn, TOut>(
        this Task<Result<TIn>> resultTask, Func<TIn, TOut> selector)
        => (await resultTask).Map(selector);

    public static Result<TOut> Bind<TIn, TOut>(this Result<TIn> result, Func<TIn, Result<TOut>> binder)
        => result.IsSuccess ? binder(result.Value) : Result.Failure<TOut>(result.Error!);

    public static async Task<Result<TOut>> BindAsync<TIn, TOut>(
        this Result<TIn> result, Func<TIn, Task<Result<TOut>>> binder)
        => result.IsSuccess ? await binder(result.Value) : Result.Failure<TOut>(result.Error!);

    public static async Task<Result<TOut>> BindAsync<TIn, TOut>(
        this Task<Result<TIn>> resultTask, Func<TIn, Task<Result<TOut>>> binder)
        => await (await resultTask).BindAsync(binder);

    public static Result<T> Ensure<T>(this Result<T> result, Func<T, bool> predicate, Error error)
        => result.IsFailure || predicate(result.Value) ? result : Result.Failure<T>(error);

    public static Result<T> Tap<T>(this Result<T> result, Action<T> action)
    {
        if (result.IsSuccess) action(result.Value);
        return result;
    }
}
