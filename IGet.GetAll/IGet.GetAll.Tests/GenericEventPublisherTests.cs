using Microsoft.Extensions.DependencyInjection;
using TestHelpers.Mocks;

namespace IGetAll.GenericEventPublisherTests;

public class GenericEventPublisherTests : IDisposable
{
    private readonly IGet i;
    private readonly TestLogger _logger;

    public GenericEventPublisherTests()
    {
        var services = new TestServices();
        _logger = new TestLogger();
        services.AddSingleton<ILogger>(_logger);
        services.AddIGet();
        services.AddIGetAll(new[] { typeof(GenericEventPublisherTests).Assembly });
        i = services.GetService<IGet>()!;
    }
    public void Dispose() { }

    [Fact]
    public async Task MultipleHandlers()
    {
        var myEvent = new EventA();
        await i.Get<EventPublisher<EventA>>().Publish(myEvent);
        Assert.Equal(2, _logger.Logs.Count);
        Assert.Contains("[Information] HandlerA1 started.", _logger.Logs[0]);
        Assert.Contains("[Information] HandlerA2 started.", _logger.Logs[1]);

    }

    [Fact]
    public async Task SingleHandler()
    {
        var myEvent = new EventB();
        await i.Get<EventPublisher<EventB>>().Publish(myEvent);
        Assert.Equal("[Information] HandlerB1 started.", Assert.Single(_logger.Logs));
    }

    [Fact]
    public async Task NoHandler()
    {
        var myEvent = new EventC();
        await i.Get<EventPublisher<EventC>>().Publish(myEvent);
        Assert.Empty(_logger.Logs);
    }
}

public interface IEventHandler<TEvent>
    where TEvent : notnull
{
    Task HandleAsync(TEvent e, CancellationToken cancellationToken);
}

public class EventPublisher<TEvent>(IGet i, ILogger logger) where TEvent : notnull
{

    public async Task Publish(TEvent e, CancellationToken cancellationToken = default)
    {
        foreach (var handler in i.GetAll<IEventHandler<TEvent>>())
        {
            try
            {
                await handler.HandleAsync(e, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in {handlerType} for {notificationKeyValuePairs}.", handler.GetType().FullName, e.ToKeyValuePairsString());
            }
        }
    }
}

public class EventA { }

public class HandlerA1(ILogger logger) : IEventHandler<EventA>
{
    public async Task HandleAsync(EventA e, CancellationToken cancellationToken)
    {
        logger.LogInformation("{typeName} started.", GetType().Name);
        await Task.Delay(0);
    }
}

public class HandlerA2(ILogger logger) : IEventHandler<EventA>
{
    public async Task HandleAsync(EventA e, CancellationToken cancellationToken)
    {
        logger.LogInformation("{typeName} started.", GetType().Name);
        await Task.Delay(0);
    }
}

public class EventB { }

public class HandlerB1(ILogger logger) : IEventHandler<EventB>
{
    public async Task HandleAsync(EventB e, CancellationToken cancellationToken)
    {
        logger.LogInformation("{typeName} started.", GetType().Name);
        await Task.Delay(0);
    }
}

public class EventC { }

public static class DummyExtensionMethods
{
    public static string ToKeyValuePairsString(this Object obj) => "";
}
