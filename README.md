# IGet

Instantiate classes that are NOT in your service collection via `i.Get<MyClass>()`. Dependencies from your service collection are automatically injected. Get an IEnumerable of class instances (with their dependencies injected) via `i.GetAll<IMyInterface>()` or `i.GetAll<MyBaseClass>()`.

| Package | `i` |
| ------------- | ------------- |
| [IGet](https://www.nuget.org/packages/iget) | `i.Get<Class>()` or `i.Get<IInterface>(reflectedClassType)` |
| [IGet.GetAll](https://www.nuget.org/packages/IGet.GetAll) | `i.GetAll<IInterface>()` or `i.GetAll<BaseClass>()` |

Release notes can be found on NuGet.

### Table of Contents
- **[Setup of IGet](#setup-of-iget)**
- **[Setup with GetAll](#setup-with-getall)**
- **[Declaring a handler](#declaring-a-handler)**
- **[Using a handler](#using-a-handler)**
- **[More complex scenarios](#more-complex-scenarios)**
- **[Shared behaviour](#shared-behaviour)**
- **[Using GetAll](#using-getall)**

## Setup of IGet

1. Install via [Visual Studio's NuGet Package Manager](https://learn.microsoft.com/en-us/nuget/consume-packages/install-use-packages-visual-studio):

<img src="https://user-images.githubusercontent.com/128420391/228517777-5e125fab-08ea-4466-92cc-06f4b016b884.png" width="300" />

2. Add `IGet` to your service collection via `serviceCollection.AddIGet()` - in a .NET Core app, this can be done in Program.cs:
```csharp
builder.Services.AddIGet();
```

## Setup with GetAll

1. If you've also installed IGet.GetAll, then add the following using statement (or add it as a global using):
```csharp
using IGetAll;
```
2. and add to the service collection:
```csharp
serviceCollection.AddIGet();
serviceCollection.AddIGetAll(new [] { typeof(Startup).Assembly, ... });
```

## An impression (C# 12 and up)
```csharp
public class IndexModel(IGet i) : PageModel
{
    public void OnGet()
    {
        var data = i.Get<DataRequestHandler>().Handle();
        ...
    }
...
}
```
or
```csharp
public class IndexModel : PageModel
{   
    public void OnPost([FromServices] IGet i, FormPost request)
    {
        var result = i.Get<FormPostHandler>().Handle(request);
        ...
    }
...
}
```

## An impression (before C# 12)
```csharp
public class IndexModel : PageModel
{
    private readonly IGet i;
    public IndexModel(IGet iget) => i = iget;    
    
    public void OnGet()
    {
        var data = i.Get<DataRequestHandler>().Handle();
        ...
    }
...
}
```


## Declaring a handler

#### Example 1
A method signature that may fit many contexts is `Task<TResult> HandleAsync(TRequest request, CancellationToken cancellationToken)`. This example uses a classic constructor instead of the primary constructor feature of C# 12 and up:
```csharp
public class MyHandler
{
    private IConnectionFactory _connectionFactory;

    public MyHandler(IConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<MyResult> HandleAsync(MyRequest request, CancellationToken cancellationToken)
    {
        ...
    }
}
```

#### Example 2
A method with a value type parameter:
```csharp
public class MyHandler(IConnectionFactory connectionFactory)
{
    public async Task<MyResult> ChooseASignature(int id)
    {
        ...
    }
}
```
#### Example 3
Synchronous code:
```csharp
public class MyHandler(ILogger<MyHandler> logger)
{
    public void Handle()
    {
        // do something
    }
}
```


## Using a handler

#### Example 1
```csharp
var result = i.Get<MyHandler>().YourSignature(1);
```
#### Example 2
```csharp
var handler = i.Get<MyHandler>();
handler.Handle();
```
#### Example 3
```csharp
var result = await i.Get<MyHandler>().HandleAsync(new MyRequest
{
    Id = 2
});
```
#### Example 4
```csharp
var result = i.Get<MyHandler>().Handle(request);
```
Because you get the handler via generics, your code is type-checked by the compiler - therefore you know that each request has a handler immediately. Also, you can place your cursor on the class's method and press `F12` to quickly navigate to the method declaration.


## More complex scenarios

#### Example 1
Handlers may get other handlers to do stuff for them.

Declare:
```csharp
public class SubscribeRequestHandler(
    IConnectionFactory connectionFactory,
    IGet i)
{
    public async Task<Result> HandleAsync(SubscribeRequest request)
    {
        var validationResult = await i.Get<SubscribeRequestValidator>().ValidateAsync(request);
        if (validationResult.IsFail)
        {
            return validationResult;
        }
        using var connection = await connectionFactory.GetConnectionAsync();
        await connection.InsertAsync(new WorkshopParticipant
        {
            Name = request.Name.Trim(),
            WorkshopId = request.WorkshopId,
        });

        return Result.Success();
    }
}
```
Use:
```csharp
    public async Task<IActionResult> OnPost(SubscribeRequest request)
    {
        var result = await i.Get<SubscribeRequestHandler>().HandleAsync(request);
        ...
```

#### Example 2
Use a try-catch structure for multiple noninterdependent handlers of the same event:
```csharp
await i.Get<MyEventPublisher>().PublishAsync(myEvent);
```
```csharp
public class MyEventPublisher(IGet iget)
{
    public async Task PublishAsync(MyEvent myEvent)
    {
        try
        {
            await i.Get<FirstHandler>().HandleAsync(myEvent);
        }
        catch { }
        try
        {
            await i.Get<SecondHandler>().HandleAsync(myEvent);
        }
        catch { }
        try
        {
            i.Get<ThirdHandler>().Handle(myEvent);
        }
        catch { }
    }
}
```
Notes:
- Exceptions should be logged in the `catch` blocks.
- Creating a generic event publisher for each of your event types can be done with IGet.GetAll - see the examples further down this readme.

## Shared behaviour

You may want multiple handlers to have certian behaviour, for example logging their execution time. You can do this via inheritance or via decorators.

#### Example 1
You could create a base class for (a subset of) your handlers if you want to do performance logging:
```csharp
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
```
Inherit:
```csharp
public class ProductOverviewQueryHandler(IBaseHandlerServices baseHandlerServices)
    : BaseHandler<Query, Result>(baseHandlerServices)
{
    protected override async Task<Result> HandleCoreAsync(
        Query query,
        CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.GetOpenConnectionAsync(cancellationToken);

        ...

        return new Result
        {
            // set properties
        };
    }
}
```
Use:
```csharp
var result = await i.Get<ProductOverviewQueryHandler>().HandleAsync(query);
```

#### Example 2
You can also use decorators to add behaviour to handlers. Using a decorator may look like:
```csharp
var decoratedHandler = i.Get<MyHandler>().DecorateWithPerformanceProfiler();
var result = await decoratedHandler.HandleAsync(request);
```
or
```csharp
var result = await i.Get<MyHandler>()
    .DecorateWithPerformanceProfiler()
    .HandleAsync(request);
```
For this to work, you need something like:
```csharp
public interface IRequestHandler<TRequest, TResponse>
{
    public Task<TResponse> HandleAsync(TRequest request);
}

public static class __DecorateWithPerformanceProfiler
{
    public static IRequestHandler<TRequest, TResponse> DecorateWithPerformanceProfiler<TRequest, TResponse>(
        this IRequestHandler<TRequest, TResponse> decorated)
    {
        return new PerformanceProfilerDecoratedHandler<TRequest, TResponse>(decorated);
    }

    public class PerformanceProfilerDecoratedHandler<TRequest, TResponse>(IRequestHandler<TRequest, TResponse> decorated)
        : IRequestHandler<TRequest, TResponse>
    {
        public async Task<TResponse> HandleAsync(TRequest request)
        {
            using (PerformanceProfiler.Current.Step($"[Handler] {request.GetType().Name}"))
            {
                return await decorated.HandleAsync(request);
            }
        }
    }
}
```

#### Example 3
If a decorator depends on services, you could create an extension method with the addition argument `IGet i`. Using the extension method then looks like this:
```csharp
var decoratedHandler = i.Get<MyHandler>().WithPerformanceLogging(i);
var result = await decoratedHandler.HandleAsync(request);
```
To make this work, create something like this:
```csharp
public static class __WithPerformanceLogging
{
    public static IRequestHandler<TRequest, TResponse> WithPerformanceLogging<TRequest, TResponse>(
        this IRequestHandler<TRequest, TResponse> decorated, IGet i)
    {
        var decorator = i.Get<PerformanceLoggingDecoratedHandler<TRequest, TResponse>>();
        decorator.Decorated = decorated;
        return decorator;
    }

    public class PerformanceLoggingDecoratedHandler<TRequest, TResponse>(IPerformanceLogger performanceLogger)
        : IRequestHandler<TRequest, TResponse>
    {
        public IRequestHandler<TRequest, TResponse> Decorated { get; set; } = default!;

        public async Task<TResponse> HandleAsync(TRequest request)
        {
            using (performanceLogger.Measure())
            {
                return await Decorated.HandleAsync(request);
            }
        }
    }
}
```

## Using GetAll

With `i.GetAll<T>()` you can get multiple handlers that implement the same interface or base class. Each time you use `i.GetAll<T>()` for a new type `T`, the collected `Type[]` is stored in a `ConcurrentDictionary`. The next time you call `i.GetAll<T>()` for the same type `T`, no assembly scanning is done.

#### Example 1
This example shows how you can create a generic event publisher that collects the handlers for you.

Declare an interface you like:
```csharp
public interface IEventHandler<TEvent>
{
    Task HandleAsync(TEvent e, CancellationToken cancellationToken);
}
```

Implement the interface:
```csharp
public class EventA { }

public class HandlerA1(ILogger<HandlerA1> logger) : IEventHandler<EventA>
{
    public async Task HandleAsync(EventA e, CancellationToken cancellationToken)
    {
        ...
    }
}

public class HandlerA2(IConnectionFactory connectionFactory) : IEventHandler<EventA>
{
    public async Task HandleAsync(EventA e, CancellationToken cancellationToken)
    {
        ...
    }
}

public class EventB { }

public class HandlerB1(ILogger<HandlerB1> logger) : IEventHandler<EventB>
{
    public async Task HandleAsync(EventB e, CancellationToken cancellationToken)
    {
        ...
    }
}
```

Create a generic event publisher for all your event types:
```csharp
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
                logger.LogError(ex, "Error in {handlerType} for {eventKeyValuePairs}.", handler.GetType().FullName, e.ToKeyValuePairsString());
            }
        }
    }
}
```
Publish events:
```csharp
// invokes HandlerA1 and HandlerA2:
await i.Get<EventPublisher<EventA>>().Publish(eventA);
// invokes HandlerB1:
await i.Get<EventPublisher<EventB>>().Publish(eventB);
```

#### Example 2
Note that because the `EventPublisher<TEvent>` of the previous example is in your own repository, you can easily tweak it. Do you want some handlers to have priority? Add a second interface `IPrio` to some handlers and execute those first. Do you want to fire them all first and then call `Task.WhenAll`? You are in control - without reading any docs:
```csharp
public async Task Publish(TEvent e, CancellationToken cancellationToken = default)
{
    var handlers = i.GetAll<IEventHandler<TEvent>>();
    var prioTasks = handlers.Where(handler => handler is IPrio).Select(handler => GetSafeTask(handler));
    await Task.WhenAll(prioTasks);
    foreach (var handler in handlers.Where(handler => handler is not IPrio))
    {
        await GetSafeTask(handler);
    }

    async Task GetSafeTask(IEventHandler<TEvent> handler)
    {
        try
        {
            await handler.HandleAsync(e, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in {handlerType} for {eventKeyValuePairs}.", handler.GetType().FullName, e.ToKeyValuePairsString());
        }
    }
}
```

#### Example 3
Just to make sure it's clear: `i.GetAll<T>()` can be used for any type of interface or base class. Do you need to get a set of validator classes for a certain request? Get them:
```csharp
i.GetAll<AbstractValidator<UpdateUserCommand>>()
```

## Try it out

The examples above give an idea of how you can be creative with IGet. Share your own examples online to spread the word about IGet.
