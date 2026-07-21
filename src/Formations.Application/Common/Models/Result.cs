namespace Formations.Application.Common.Models;

public class Result<T>
{
    public bool IsSuccess { get; private set; }
    public T? Data { get; private set; }
    public string? Error { get; private set; }
    public IEnumerable<string> Errors { get; private set; } = [];

    private Result() { }

    public static Result<T> Success(T data) => new() { IsSuccess = true, Data = data };
    public static Result<T> Failure(string error) => new() { IsSuccess = false, Error = error, Errors = [error] };
    public static Result<T> Failure(IEnumerable<string> errors) => new() { IsSuccess = false, Errors = errors, Error = string.Join("; ", errors) };
}

public class Result
{
    public bool IsSuccess { get; private set; }
    public string? Error { get; private set; }
    public IEnumerable<string> Errors { get; private set; } = [];

    private Result() { }

    public static Result Success() => new() { IsSuccess = true };
    public static Result Failure(string error) => new() { IsSuccess = false, Error = error, Errors = [error] };
    public static Result Failure(IEnumerable<string> errors) => new() { IsSuccess = false, Errors = errors, Error = string.Join("; ", errors) };
}
