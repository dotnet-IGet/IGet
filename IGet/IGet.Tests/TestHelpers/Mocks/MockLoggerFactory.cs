namespace TestHelpers.Mocks;

public class MockLoggerFactory : ILoggerFactory
{
    private readonly TestLogger _logger;

    public MockLoggerFactory(TestLogger logger)
    {
        _logger = logger;
    }

    public void AddProvider(ILoggerProvider provider)
    {
        throw new NotImplementedException();
    }

    public ILogger CreateLogger(string categoryName) => _logger;
    public void Dispose() { }
}
