namespace CoachOS.Domain.Models;

public class Result
{
    public bool IsSuccess { get; }
    public IReadOnlyList<Error> Errors { get; }

    protected Result(bool isSuccess, IReadOnlyList<Error> errors)
    {
        IsSuccess = isSuccess;
        Errors = errors;
    }

    public static Result Ok() => new(true, Array.Empty<Error>());
    public static Result Fail(Error error) => new(false, [error]);
    public static Result Fail(IEnumerable<Error> errors) => new(false, errors.ToList());
    public static Result Fail(string message) => new(false, [new Error(ErrorCodes.Unexpected, message)]);
    public static Result Fail(IEnumerable<string> messages) => new(false, messages.Select(m => new Error(ErrorCodes.Validation, m)).ToList());
}

public class Result<T> : Result
{
    public T? Value { get; }

    private Result(bool isSuccess, T? value, IReadOnlyList<Error> errors)
        : base(isSuccess, errors)
    {
        Value = value;
    }

    public static Result<T> Ok(T value) => new(true, value, Array.Empty<Error>());
    public static new Result<T> Fail(Error error) => new(false, default, [error]);
    public static new Result<T> Fail(IEnumerable<Error> errors) => new(false, default, errors.ToList());
    public static new Result<T> Fail(string message) => new(false, default, [new Error(ErrorCodes.Unexpected, message)]);
    public static new Result<T> Fail(IEnumerable<string> messages) => new(false, default, messages.Select(m => new Error(ErrorCodes.Validation, m)).ToList());
}
