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

public class NotificationPublisher
{
    private IGet i;

    public NotificationPublisher(IGet iget)
    {
        i = iget;
    }

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

public class FirstHandler
{
    private readonly ILogger _logger;

    public FirstHandler(ILogger logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(Notification notification)
    {
        _logger.LogInformation("{typeName} started.", GetType().Name);
        return Task.CompletedTask;
    }
}

public class SecondHandler
{
    private readonly ILogger _logger;

    public SecondHandler(ILogger logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(Notification notification)
    {
        _logger.LogInformation("{typeName} started.", GetType().Name);
        return Task.CompletedTask;
    }
}

public class ThirdHandler
{
    private readonly ILogger _logger;

    public ThirdHandler(ILogger logger)
    {
        _logger = logger;
    }

    public void Handle(Notification notification)
    {
        _logger.LogInformation("{typeName} started.", GetType().Name);
    }
}