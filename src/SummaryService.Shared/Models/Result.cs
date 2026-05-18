namespace SummaryService.Shared.Models;

public sealed class Result<T>
{
    private Result(T value)
    {
        Value = value;
        Error = Error.None;
        IsSuccess = true;
    }

    private Result(Error error)
    {
        Value = default;
        Error = error;
        IsSuccess = false;
    }

    public T? Value { get; }
    public Error Error { get; }
    public bool IsSuccess { get; }

    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(Error error) => new(error);

    public T Match(Func<T, T> onSuccess, Func<Error, T> onFailure)
        => IsSuccess ? onSuccess(Value!) : onFailure(Error);
}

public sealed class Result
{
    private Result()
    {
        Error = Error.None;
        IsSuccess = true;
    }

    private Result(Error error)
    {
        Error = error;
        IsSuccess = false;
    }

    public Error Error { get; }
    public bool IsSuccess { get; }

    public static Result Success() => new();
    public static Result Failure(Error error) => new(error);
}
