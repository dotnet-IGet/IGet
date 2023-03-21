using TestHelpers.Mocks;

namespace Tests;

public class GetViaType
{
    [Fact]
    public async void Test()
    {
        // Arrange
        var logger = new TestLogger();
        var services = new TestServices();
        services.AddSingleton<ILogger>(logger);
        services.AddIGet();
        var i = services.GetService<IGet>()!;

        // Act
        var notification = new Notification();
        Type[] GetNotificationHandlerTypes()
        {
            /* Usually this would be done via reflection, 
             * but for this unit test that is not needed. */
            return new[] { typeof(NotificationHandler) };
        }

        Type[] types = GetNotificationHandlerTypes();
        foreach(var type in types)
        {
            try
            {
                await i.Get<INotificationHandler>(type).Handle(notification);
            }
            catch { }
        }

        // Assert
        Assert.Equal("[Information] Notification handled.", Assert.Single(logger.Logs));
    }

    public interface INotificationHandler
    {
        Task Handle(Notification notification);
    }

    public class NotificationHandler : INotificationHandler
    {
        private ILogger _logger;

        public NotificationHandler(ILogger logger)
        {
            _logger = logger;
        }

        public Task Handle(Notification notification)
        {
            _logger.LogInformation("Notification handled.");
            return Task.CompletedTask;
        }
    }
}
