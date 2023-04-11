# IGet

Instantiate classes that are NOT in your service collection via `i.Get<MyClass>()`. Dependencies from your service collection are automatically injected. Get an IEnumerable of class instances (with their dependencies injected) via `i.GetAll<IMyInterface>()` or `i.GetAll<MyBaseClass>()`.

| Package | Latest version | `i` |
| ------------- | ------------- |------------- |
| [IGet](https://www.nuget.org/packages/iget) | [![Nuget](https://img.shields.io/nuget/v/iget)](https://www.nuget.org/packages/iget) | `i.Get<Class>()` or `i.Get<IInterface>(reflectedClassType)` |
| [IGet.GetAll](https://www.nuget.org/packages/IGet.GetAll) | [![Nuget](https://img.shields.io/nuget/v/iget.getall)](https://www.nuget.org/packages/IGet.GetAll) | `i.GetAll<IInterface>()` or `i.GetAll<BaseClass>()` |

### Table of Contents
- **[Quick setup](#quick-setup)**
- **[Why IGet?](#why-iget)**
- **[Why IGet.GetAll?](#why-igetgetall)**

## Quick setup

1. Install via [Visual Studio's NuGet Package Manager](https://learn.microsoft.com/en-us/nuget/consume-packages/install-use-packages-visual-studio):

<img src="https://user-images.githubusercontent.com/128420391/228517777-5e125fab-08ea-4466-92cc-06f4b016b884.png" width="300" />

2. Add `IGet` to your service collection via `serviceCollection.AddIGet()` - in a .NET Core app, this can be done in Program.cs:
```csharp
builder.Services.AddIGet();
```
3. Now you can use it (for example in a .NET Core web app):
```csharp
public class IndexModel : PageModel
{
    private readonly IGet i;

    public IndexModel(IGet iget)
    {
        i = iget;
    }
    
    
    public void OnGet()
    {
        var data = i.Get<DataRequestHandler>().Handle();
        ...
    }
...
}
```
4. If you've also installed IGet.GetAll, then add the following using statement (or add it as a global using):
```csharp
using IGetAll;
```
5. and add to the service collection:
```csharp
serviceCollection.AddIGet();
serviceCollection.AddIGetAll(new [] { typeof(Startup).Assembly, ... });
```
For more examples, see below.


## Why IGet?

- you don't need to implement any interface for your handlers.
- have compile-time checks that all handlers exist.
- use editor shortcuts to jump to a handler's method immediately.
- have a short StackTrace in case of an error.
- IGet is easy to understand - this might save time and money.
- IGet is extremely lightweight - less code often means fewer bugs.


## Declaring a handler

#### Example 1
A method signature that fits many contexts is `Task<TResult> HandleAsync(TRequest request, CancellationToken cancellationToken)`:
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
public class MyHandler
{
    private IConnectionFactory _connectionFactory;

    public MyHandler(IConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<MyResult> ChooseASignature(int id)
    {
        ...
    }
}
```
#### Example 3
Synchronous code:
```csharp
public class MyHandler
{
    private ILogger<MyHandler> _logger;

    public MyHandler(ILogger<MyHandler> logger)
    {
        _logger = logger;
    }

    public void Handle()
    {
        // do something
    }
}
```


## Using a handler

#### Example 1
```csharp
var result = i.Get<MyHandler>().AnyRandomSignature(1);
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
public class SubscribeRequestHandler
{
    private readonly IConnectionFactory _connectionFactory;
    private readonly IGet i;

    public SubscribeRequestHandler(IConnectionFactory connectionFactory, IGet iget)
    {
        _connectionFactory = connectionFactory;
        i = iget;
    }

    public async Task<Result> HandleAsync(SubscribeRequest request)
    {
        var validationResult = await i.Get<SubscribeRequestValidator>().ValidateAsync(request);
        if (validationResult.IsFail)
        {
            return validationResult;
        }
        using var connection = await _connectionFactory.GetConnectionAsync();
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
public class MyEventPublisher
{
    private IGet i;

    public MyEventPublisher(IGet iget)
    {
        i = iget;
    }

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
- If you dislike creating event publishers like this, then have a look at the IGet.GetAll examples further down this readme.


#### Example 3
You may want multiple handlers to have certian behaviour, for example logging their execution time. You could create a base class for (a subset of) your handlers:
```csharp
public abstract class BaseHandler<THandler,TRequest, TResponse>
    where THandler : notnull
    where TRequest : notnull
{
    protected readonly ILogger<THandler> _logger;
    protected readonly IDbConnectionFactory _connectionFactory;
    protected readonly IHostEnvironment _hostEnvironment;

    public BaseHandler(IBaseHandlerServices baseHandlerServices)
    {
        _logger = baseHandlerServices.LoggerFactory.CreateLogger<THandler>();
        _connectionFactory = baseHandlerServices.ConnectionFactory;
        _hostEnvironment = baseHandlerServices.HostEnvironment;
    }

    public async Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken = default)
    {
        if (!_hostEnvironment.IsProduction())
        {
            _logger.LogInformation("Start handling request {RequestMembers}.", request.ToKeyValuePairsString());
        }
        var startTime = DateTime.UtcNow;

        TResponse result;
        try
        {
            result = await HandleCoreAsync(request, cancellationToken);
        }
        finally
        {
            var totalMilliseconds = (DateTime.UtcNow - startTime).TotalMilliseconds;
            if (!_hostEnvironment.IsProduction() || totalMilliseconds > 500)
            {
                _logger.LogInformation("Finished in {TotalMilliseconds}ms.", totalMilliseconds);
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
public class ProductOverviewQueryHandler 
    : BaseHandler<ProductOverviewQueryHandler, Query, Result>
{
    public ProductOverviewQueryHandler(IBaseHandlerServices baseHandlerServices) 
        : base(baseHandlerServices)
    { }

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

#### Example 4
Instead of using a base class, you could use decorators to add behaviour to handlers:
```csharp
var decoratedHandler = i.Get<MyHandler>().DecorateWithPerformanceProfiler();
var result = await decoratedHandler.HandleAsync(request);
```
For this to work, you need something like:
```csharp
public interface IDecoratableHandler<TRequest, TResponse>
{
    public Task<TResponse> HandleAsync(TRequest request);
}

public static class DecoratableHandlerExtensions
{
    public static IDecoratableHandler<TRequest, TResponse> DecorateWithPerformanceProfiler<TRequest, TResponse>(
        this IDecoratableHandler<TRequest, TResponse> decorated)
    {
        return new PerformanceProfilerDecoratedHandler<TRequest, TResponse>(decorated);
    }

    public class PerformanceProfilerDecoratedHandler<TRequest, TResponse> : IDecoratableHandler<TRequest, TResponse>
    {
        private readonly IDecoratableHandler<TRequest, TResponse> _decorated;

        public PerformanceProfilerDecoratedHandler(IDecoratableHandler<TRequest, TResponse> decorated)
        {
            _decorated = decorated;
        }

        public async Task<TResponse> HandleAsync(TRequest request)
        {
            using (PerformanceProfiler.Current.Step($"[Handler] {request.GetType().Name}"))
            {
                return await _decorated.HandleAsync(request);
            }
        }
    }
}
```

#### Example 5
If a decorator depends on services, you could create an extension method with the addition argument `IGet i`. Using the extension method then looks like this:
```csharp
var decoratedHandler = i.Get<MyHandler>().WithPerformanceLogging(i);
var result = await decoratedHandler.HandleAsync(request);
```
To make this work, create something like this:
```csharp
public static IDecoratableHandler<TRequest, TResponse> WithPerformanceLogging<TRequest, TResponse>(
    this IDecoratableHandler<TRequest, TResponse> decorated, IGet i)
    {
        var decorator = i.Get<PerformanceLoggingDecoratedHandler<TRequest, TResponse>>();
        decorator.Decorate(decorated);
        return decorator;
    }

public class PerformanceLoggingDecoratedHandler<TRequest, TResponse> : IDecoratableHandler<TRequest, TResponse>
{
    private readonly IDependency _dependency;

    public PerformanceLoggingDecoratedHandler(IDependency dependency)
    {
        _dependency = dependency;
    }

    public IDecoratableHandler<TRequest, TResponse> Decorated { get; set; } = default!;

    public IDecoratableHandler<TRequest, TResponse> Decorate(IDecoratableHandler<TRequest, TResponse> decorated)
    {
        Decorated = decorated;
        return this;
    }

    public async Task<TResponse> HandleAsync(TRequest request)
    {
        using (_dependency.DoSomething())
        {
            return await Decorated.HandleAsync(request);
        }
    }
}
```

## Why IGet.GetAll?

With `i.GetAll<T>()` you can get multiple handlers that implement the same interface or base class. No matter how complicated your interfaces or generic base classes are - think about `IMyInterface<SomeClass, NestedBaseClass<AnotherClass, AndMore>>` - no additional configuration is needed.

## About IGet.GetAll's performance

Each time you use `i.GetAll<T>()` for a new type `T`, the collected `Type[]` is stored in a `ConcurrentDictionary`. The next time you call `i.GetAll<T>()` for the same type `T`, no assembly scanning is done.


## i.GetAll&lt;T&gt;() examples

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

public class HandlerA1 : IEventHandler<EventA>
{
    private readonly ILogger<HandlerA1> _logger;
    public HandlerA1(ILogger<HandlerA1> logger)
    {
        _logger = logger;
    }
    public async Task HandleAsync(EventA e, CancellationToken cancellationToken)
    {
        ...
    }
}

public class HandlerA2 : IEventHandler<EventA>
{
    private readonly IConnectionFactory _connectionFactory;
    public HandlerA2(IConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }
    public async Task HandleAsync(EventA e, CancellationToken cancellationToken)
    {
        ...
    }
}

public class EventB { }

public class HandlerB1 : IEventHandler<EventB>
{
    private readonly ILogger<Handler1> _logger;
    public HandlerB1(ILogger<Handler1> logger)
    {
        _logger = logger;
    }
    public async Task HandleAsync(EventB e, CancellationToken cancellationToken)
    {
        ...
    }
}
```

Create a generic event publisher for all your event types:
```csharp
public class EventPublisher<TEvent> where TEvent : notnull
{
    private readonly ILogger _logger;
    private readonly IGet i;

    public EventPublisher(IGet iget, ILogger logger)
    {
        _logger = logger;
        i = iget;
    }

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
                _logger.LogError(ex, "Error in {handlerType} for {eventKeyValuePairs}.", handler.GetType().FullName, e.ToKeyValuePairsString());
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
            _logger.LogError(ex, "Error in {handlerType} for {eventKeyValuePairs}.", handler.GetType().FullName, e.ToKeyValuePairsString());
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
