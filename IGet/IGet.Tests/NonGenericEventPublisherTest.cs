using TestHelpers.Mocks;

namespace Tests;

public class NonGenericEventPublisherTest
{
    [Fact]
    public async Task Test()
    {
        // Arrange
        var services = new TestServices();
        var logger = new TestLogger();
        services.AddSingleton<ILogger>(logger);
        services.AddIGet();
        var i = services.GetService<IGet>()!;

        // Act
        var myEvent = new EventA();
        await i.Get<EventPublisher>().PublishAsync(myEvent);

        // Assert
        Assert.Equal(
        [
            "[Information] HandlerA1 started.",
            "[Information] HandlerA2 started.",
            "[Information] HandlerA3 started.",
        ], logger.Logs);
    }
}

public class EventPublisher(IGet i)
{
    public async Task PublishAsync(EventA e)
    {
        try
        {
            await i.Get<HandlerA1>().HandleAsync(e);
        }
        catch { }
        try
        {
            await i.Get<HandlerA2>().HandleAsync(e);
        }
        catch { }
        try
        {
            i.Get<HandlerA3>().Handle(e);
        }
        catch { }
    }
}

public class EventA { }

public class HandlerA1(ILogger logger)
{
    public Task HandleAsync(EventA e)
    {
        logger.LogInformation("{typeName} started.", GetType().Name);
        return Task.CompletedTask;
    }
}

public class HandlerA2(ILogger logger)
{
    public Task HandleAsync(EventA e)
    {
        logger.LogInformation("{typeName} started.", GetType().Name);
        return Task.CompletedTask;
    }
}

public class HandlerA3(ILogger logger)
{
    public void Handle(EventA e)
    {
        logger.LogInformation("{typeName} started.", GetType().Name);
    }
}