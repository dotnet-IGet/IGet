namespace TestHelpers.Mocks;

public class TestLogger : ILogger
{
    public List<string> Logs { get; } = new();
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        throw new NotImplementedException();
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var message = formatter(state, exception);
        var logLine = $"[{logLevel}] {message}";
        if (exception is not null)
        {
            logLine += Environment.NewLine;
            logLine += exception.Message;
            logLine += Environment.NewLine;
            logLine += exception.StackTrace;
        }
        Logs.Add(logLine);
    }
}
