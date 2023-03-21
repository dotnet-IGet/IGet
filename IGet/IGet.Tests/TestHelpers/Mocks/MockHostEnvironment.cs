namespace TestHelpers.Mocks;

public interface IHostEnvironment
{
    bool IsProduction();
}

public class MockHostEnvironment : IHostEnvironment
{
    public bool IsProduction()
    {
        return false;
    }
}
