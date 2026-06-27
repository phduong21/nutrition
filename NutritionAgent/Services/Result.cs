namespace NutritionAgent.Services;

public enum ErrorKind
{
    NotFound,
    UpstreamFailure,
    InvalidInput
}

public sealed record ServiceError(ErrorKind Kind, string Message);

public sealed class Result<T>
{
    private Result(T? value, ServiceError? error)
    {
        Value = value;
        Error = error;
    }

    public T? Value { get; }
    public ServiceError? Error { get; }
    public bool IsSuccess => Error is null;

    public static Result<T> Success(T value) => new(value, null);

    public static Result<T> Failure(ServiceError error) => new(default, error);
}
