namespace TestHelpers.Mocks;

public interface IPerformanceLogger
{
    public IDisposable Measure();
}
