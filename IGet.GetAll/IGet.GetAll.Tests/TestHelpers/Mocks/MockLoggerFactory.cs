namespace TestHelpers.Mocks;

public class MockLoggerFactory(TestLogger logger) : ILoggerFactory
{
    public void AddProvider(ILoggerProvider provider)
    {
        throw new NotImplementedException();
    }

    public ILogger CreateLogger(string categoryName) => logger;
    public void Dispose() { }
}
