using TestHelpers.Mocks;

namespace Tests;

public class SharedBehaviourTest
{
    [Fact]
    public async Task Test()
    {
        // Arrange
        var logger = new TestLogger();
        var services = new TestServices();
        services.AddScoped<IDbConnectionFactory, MockDbConnectionFactory>();
        services.AddSingleton<IHostEnvironment, MockHostEnvironment>();
        services.AddSingleton<ILoggerFactory>(new MockLoggerFactory(logger));
        services.AddScoped<IBaseHandlerServices, BaseHandlerServices>();
        services.AddIGet();
        var i = services.GetService<IGet>()!;

        // Act
        var query = new Query();
        var result = await i.Get<ProductOverviewQueryHandler>().HandleAsync(query);

        // Assert
        Assert.NotNull(result);
        Assert.StartsWith("[Information] Start handling request ", logger.Logs.First());
        Assert.StartsWith("[Information] Finished in ", logger.Logs.Last());
    }
}

public abstract class BaseHandler<TRequest, TResponse>
    where TRequest : notnull
{
    protected readonly ILogger _logger;
    protected readonly IDbConnectionFactory _connectionFactory;
    protected readonly IHostEnvironment _hostEnvironment;

    public BaseHandler(IBaseHandlerServices baseHandlerServices)
    {
        _logger = baseHandlerServices.LoggerFactory.CreateLogger(GetType().FullName!);
        _connectionFactory = baseHandlerServices.ConnectionFactory;
        _hostEnvironment = baseHandlerServices.HostEnvironment;
    }

    public async Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken = default)
    {
        if (!_hostEnvironment.IsProduction())
        {
            _logger.LogInformation("Start handling request {RequestMembers}.", request.ToKeyValuePairsString());
        }
        var stopWatch = new Stopwatch();
        TResponse result;
        try
        {
            stopWatch.Start();
            result = await HandleCoreAsync(request, cancellationToken);
        }
        finally
        {
            stopWatch.Stop();
            if (!_hostEnvironment.IsProduction() || stopWatch.ElapsedMilliseconds > 500)
            {
                _logger.LogInformation("Finished in {TotalMilliseconds}ms.", stopWatch.ElapsedMilliseconds);
            }
        }

        return result;
    }

    protected abstract Task<TResponse> HandleCoreAsync(
        TRequest request,
        CancellationToken cancellationToken);
}

public class ProductOverviewQueryHandler : BaseHandler<Query, Result>
{
    public ProductOverviewQueryHandler(IBaseHandlerServices baseHandlerServices) 
        : base(baseHandlerServices)
    { }

    protected override async Task<Result> HandleCoreAsync(
        Query query,
        CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.GetOpenConnectionAsync(cancellationToken);

        // execute

        return new Result
        {
            // set properties
        };
    }
}

public class Query { }

public static class __ToKeyValuePairString
{
    public static string ToKeyValuePairsString(this object obj) => "Dummy";
}

public interface IBaseHandlerServices
{
    IDbConnectionFactory ConnectionFactory { get; }
    ILoggerFactory LoggerFactory { get; }
    IHostEnvironment HostEnvironment { get; }
}

public class BaseHandlerServices : IBaseHandlerServices
{
    public BaseHandlerServices(
        IDbConnectionFactory connectionFactory,
        IHostEnvironment hostEnvironment,
        ILoggerFactory loggerFactory)
    {
        ConnectionFactory = connectionFactory;
        HostEnvironment = hostEnvironment;
        LoggerFactory = loggerFactory;
    }

    public IDbConnectionFactory ConnectionFactory { get; }
    public IHostEnvironment HostEnvironment { get; }
    public ILoggerFactory LoggerFactory { get; }
}
