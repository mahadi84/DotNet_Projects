namespace OnlineBanking.Common;

public sealed record Result(bool Success, string Message)
{
    public static Result Ok(string msg = "OK") => new(true, msg);
    public static Result Fail(string msg) => new(false, msg);
}

public sealed record Result<T>(bool Success, string Message, T? Data)
{
    public static Result<T> Ok(T data, string msg = "OK") => new(true, msg, data);
    public static Result<T> Fail(string msg) => new(false, msg, default);
}
