namespace TestHelpers.Mocks;

public interface IDbConnectionFactory
{
    Task<IConnection> GetOpenConnectionAsync(CancellationToken cancellationToken);
}

public class MockDbConnectionFactory : IDbConnectionFactory
{
    public async Task<IConnection> GetOpenConnectionAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(0);
        return new MockConnection();
    }

    public class MockConnection : IConnection
    {
        public ValueTask DisposeAsync() => new ValueTask();
    }
}

public interface IConnection : IAsyncDisposable
{

}
