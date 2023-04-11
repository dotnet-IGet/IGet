namespace TestHelpers.Mocks;
public class PerformanceProfiler
{
    public static _Current Current = new _Current();
    public class _Current
    {
        public IDisposable Step(string step) => default!;
    }
}
