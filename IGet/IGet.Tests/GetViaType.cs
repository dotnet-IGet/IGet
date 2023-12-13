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
        var myEvent = new EventA();
        Type[] GetEventHandlerTypes()
        {
            /* Usually this would be done via reflection,
             * but for this unit test that is not needed. */
            return new[] { typeof(MyEventHandler) };
        }

        Type[] types = GetEventHandlerTypes();
        foreach(var type in types)
        {
            try
            {
                await i.Get<IEventHandler>(type).Handle(myEvent);
            }
            catch { }
        }

        // Assert
        Assert.Equal("[Information] EventA handled.", Assert.Single(logger.Logs));
    }

    public interface IEventHandler
    {
        Task Handle(EventA e);
    }

    public class MyEventHandler(ILogger logger) : IEventHandler
    {
        public Task Handle(EventA e)
        {
            logger.LogInformation("EventA handled.");
            return Task.CompletedTask;
        }
    }
}
