using TestHelpers.Mocks;

namespace Tests;

public class NonGenericNotificationPublisherTest
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
        var notification = new Notification();
        await i.Get<NotificationPublisher>().PublishAsync(notification);

        // Assert
        Assert.Equal(new List<string>
        {
            "[Information] FirstHandler started.",
            "[Information] SecondHandler started.",
            "[Information] ThirdHandler started.",
        }, logger.Logs);
    }
}

public class NotificationPublisher(IGet i)
{
    public async Task PublishAsync(Notification notification)
    {
        try
        {
            await i.Get<FirstHandler>().HandleAsync(notification);
        }
        catch { }
        try
        {
            await i.Get<SecondHandler>().HandleAsync(notification);
        }
        catch { }
        try
        {
            i.Get<ThirdHandler>().Handle(notification);
        }
        catch { }
    }
}

public class Notification { }

public class FirstHandler(ILogger logger)
{
    public Task HandleAsync(Notification notification)
    {
        logger.LogInformation("{typeName} started.", GetType().Name);
        return Task.CompletedTask;
    }
}

public class SecondHandler(ILogger logger)
{
    public Task HandleAsync(Notification notification)
    {
        logger.LogInformation("{typeName} started.", GetType().Name);
        return Task.CompletedTask;
    }
}

public class ThirdHandler(ILogger logger)
{
    public void Handle(Notification notification)
    {
        logger.LogInformation("{typeName} started.", GetType().Name);
    }
}