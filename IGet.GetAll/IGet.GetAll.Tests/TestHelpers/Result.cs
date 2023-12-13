namespace TestHelpers;
public class Result
{
    public Result() { }
    private Result(string errorMessage)
    {
        ErrorMessages.Add(errorMessage);
    }

    public List<string> ErrorMessages { get; } = new();
    public bool IsFail => !IsSuccess;
    public bool IsSuccess => ErrorMessages.Count == 0;
    public static Result Success() => new();
    public static Result Fail(string message) => new(message);
    public static Result<T> Success<T>(T data) where T : notnull => new(data);
    public static Result<T> Fail<T>(string message) where T : notnull
    {
        var result = new Result<T>(default!);
        result.ErrorMessages.Add(message);
        return result;
    }
    public static Result<T> Fail<T>(IEnumerable<string> messages) where T : notnull
    {
        var result = new Result<T>(default!);
        result.ErrorMessages.AddRange(messages);
        return result;
    }

}

public class Result<T>(T data) where T : notnull
{
    public bool IsFail => !IsSuccess;
    public bool IsSuccess => ErrorMessages.Count == 0;
    public List<string> ErrorMessages { get; } = new();
    public T Data { get; } = data;
}
