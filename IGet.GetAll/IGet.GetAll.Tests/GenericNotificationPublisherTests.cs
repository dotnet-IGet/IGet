﻿using Microsoft.Extensions.DependencyInjection;
using TestHelpers.Mocks;

namespace IGetAll.GenericNotificationPublisherTests;

public class GenericNotificationPublisherTests : IDisposable
{
    private readonly IGet i;
    private readonly TestLogger _logger;

    public GenericNotificationPublisherTests()
    {
        var services = new TestServices();
        _logger = new TestLogger();
        services.AddSingleton<ILogger>(_logger);
        services.AddIGet();
        services.AddIGetAll(new[] { typeof(GenericNotificationPublisherTests).Assembly });
        i = services.GetService<IGet>()!;
    }
    public void Dispose() { }

    [Fact]
    public async Task MultipleHandlers()
    {
        var notification = new NotificationA();
        await i.Get<NotificationPublisher<NotificationA>>().Publish(notification);
        Assert.Equal(2, _logger.Logs.Count);
        Assert.Contains("[Information] HandlerA1 started.", _logger.Logs[0]);
        Assert.Contains("[Information] HandlerA2 started.", _logger.Logs[1]);

    }

    [Fact]
    public async Task SingleHandler()
    {
        var notification = new NotificationB();
        await i.Get<NotificationPublisher<NotificationB>>().Publish(notification);
        Assert.Equal("[Information] HandlerB1 started.", Assert.Single(_logger.Logs));
    }

    [Fact]
    public async Task NoHandler()
    {
        var notification = new NotificationC();
        await i.Get<NotificationPublisher<NotificationC>>().Publish(notification);
        Assert.Empty(_logger.Logs);
    }
}

public interface INotificationHandler<TNotification>
    where TNotification : notnull
{
    Task HandleAsync(TNotification notification, CancellationToken cancellationToken);
}

public class NotificationPublisher<TNotification> where TNotification : notnull
{
    private readonly ILogger _logger;
    private readonly IGet i;

    public NotificationPublisher(IGet iget, ILogger logger)
    {
        _logger = logger;
        i = iget;
    }

    public async Task Publish(TNotification notification, CancellationToken cancellationToken = default)
    {
        foreach (var handler in i.GetAll<INotificationHandler<TNotification>>())
        {
            try
            {
                await handler.HandleAsync(notification, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in {handlerType} for {notificationKeyValuePairs}.", handler.GetType().FullName, notification.ToKeyValuePairsString());
            }
        }
    }
}

public class NotificationA { }

public class HandlerA1 : INotificationHandler<NotificationA>
{
    private readonly ILogger _logger;

    public HandlerA1(ILogger logger)
    {
        _logger = logger;
    }

    public async Task HandleAsync(NotificationA notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("{typeName} started.", GetType().Name);
        await Task.Delay(0);
    }
}

public class HandlerA2 : INotificationHandler<NotificationA>
{
    private readonly ILogger _logger;

    public HandlerA2(ILogger logger)
    {
        _logger = logger;
    }

    public async Task HandleAsync(NotificationA notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("{typeName} started.", GetType().Name);
        await Task.Delay(0);
    }
}

public class NotificationB { }

public class HandlerB1 : INotificationHandler<NotificationB>
{
    private readonly ILogger _logger;

    public HandlerB1(ILogger logger)
    {
        _logger = logger;
    }

    public async Task HandleAsync(NotificationB notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("{typeName} started.", GetType().Name);
        await Task.Delay(0);
    }
}

public class NotificationC { }

public static class DummyExtensionMethods
{
    public static string ToKeyValuePairsString(this Object obj) => "";
}
