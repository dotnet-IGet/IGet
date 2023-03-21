# IGet

Instantiate classes that are NOT in your service collection via `i.Get<MyClass>()`. Dependencies from your service collection are automatically injected. Get an IEnumerable of class instances (with their dependencies injected) via `i.GetAll<IMyInterface>()` or `i.GetAll<MyBaseClass>()`.

| Package | Latest version | `i` |
| ------------- | ------------- |------------- |
| [IGet](https://www.nuget.org/packages/iget) | [![Nuget](https://img.shields.io/nuget/v/iget)](https://www.nuget.org/packages/iget) | `i.Get<Class>()` or `i.Get<IInterface>(reflectedClassType)` |
| [IGet.GetAll](https://www.nuget.org/packages/IGet.GetAll) | [![Nuget](https://img.shields.io/nuget/v/iget.getall)](https://www.nuget.org/packages/IGet.GetAll) | `i.GetAll<IInterface>()` or `i.GetAll<BaseClass>()` |

### Table of Contents
**[Quick setup](#quick-setup)**<br>
**[Why IGet?](#why-iget)**<br>
**[Why IGet.GetAll?](#why-igetgetall)**

## Quick setup

Install via [Visual Studio's NuGet Package Manager](https://learn.microsoft.com/en-us/nuget/consume-packages/install-use-packages-visual-studio).

#### IGet-only
```csharp
serviceCollection.AddIGet();
```
 *Idea*: use `i.Get<Handler>().Handle(request)` instead of `mediatR.Send(request)`. See the examples below.

#### IGet with IGet.GetAll
```csharp
using IGetAll;
```
```csharp
serviceCollection.AddIGet();
serviceCollection.AddIGetAll(new [] { typeof(Startup).Assembly, ... });
```
*Idea*: also replace `mediatR.Publish(notification)` by `i.Get<NotificationPublisher<NotificationA>>().Publish(notification)`. See the examples below.


## Why IGet?

MediatR has positively shaped code bases of developers for many years. You might, however, like IGet better:

- you don't need to implement any interface for your handlers.
- creating a request class is optional.
- have compile-time checks that all handlers exist.
- use editor shortcuts to jump to a handler's method immediately.
- have a shorter StackTrace in case of an error.
- have more control to design complex processes.
- IGet is easier to understand than MediatR and it therefore might save time and money.
- IGet is extremely lightweight - less code often means fewer bugs.

IGet has only one responsibility - instantiating classes with their dependencies injected - but with this you can create MediatR-like structures easily. A basic IRequest-IRequestHandler structure needs less code if you use IGet and complex structures such as INotification-INotificationHandler are completely under your control.


## Declaring a handler

#### Example 1
There is no need to implement `IRequest` and `IRequestHandler<Request>` or `IRequest<Result>` and `IRequestHandler<Request, Result>`. Choose another method name and use value type parameters if you want.
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
        // do something
    }
}
```
#### Example 2
Do you prefer synchronous code if possible? That's allowed!
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

## Sending a request
In the examples below `i` is the `IGet` instance where you call `Get` on. (Like `mediator` might have been the name of the variable for a `IMediator` instance where you called `Send` on.)

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
Handlers may get other handlers to do stuff for them, like validation, pre-processing and post-processing.
```csharp
var result = await i.Get<MoreComplexHandler>().Handle(request);
```
```csharp
public class MoreComplexHandler
{
    private ILogger<MoreComplexHandler> _logger;
    private IGet i;

    public MoreComplexHandler(IGet iget, ILogger<MoreComplexHandler> logger)
    {
        _logger = logger;
        i = iget;
    }

    public Result<WhatWasAskedFor> Handle(RequestX request)
    {
        try
        {
            var validationResult = i.Get<RequestXValidator>().Validate(request);
            if (validationResult.IsFail)
            {
                return Result.Fail<WhatWasAskedFor>(validationResult.ErrorMessages);
            }

            i.Get<RequestXPreProcessor>().Prepare(request);
            var whatWasAskedFor = i.Get<RequestXMainProcessor>().Handle(request);

            try
            {
                i.Get<RequestXPostProcessor>().DoLessImportantStuffWith(request, whatWasAskedFor);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Post processor failed for request {requestId}.", request.Id);
            }

            return Result.Success(whatWasAskedFor);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error for request {requestId}.", request.Id);
            return Result.Fail<WhatWasAskedFor>("Something went wrong. Try again later.");
        }
    }
}
```

#### Example 2
The functionality of MediatR's INotification-INotificationHandler combination can be created via something like this:
```csharp
await i.Get<NotificationPublisher>().PublishAsync(notification);
```
```csharp
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
```
Notes:
- Exceptions should be logged in the `catch` blocks.
- If you find the example above too risky - because you might forget to register a newly created handler in the publisher, then have a look at [IGet.GetAll](#why-igetgetall). You can ask IGet.GetAll to return an instance of each class that implements a certain interface, for example `INotification<MyRequest>`. It uses `i.Get<T>(type)` from [IGet](#why-iget) for instantiating the `type` and casting it to `T` and it has a "memory" to increase performance on next calls.

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
        HandleBefore(request);
        var result = await HandleCoreAsync(request, cancellationToken);
        HandleAfter();
        return result;
    }

    private void HandleBefore(TRequest request)
    {
        if (!_hostEnvironment.IsProduction())
        {
            _logger.LogInformation("Start handling request {RequestMembers}.", request.ToKeyValuePairsString());
        }
        StartTime = DateTime.UtcNow;
    }
    DateTime StartTime;
    protected abstract Task<TResponse> HandleCoreAsync(
        TRequest request, 
        CancellationToken cancellationToken);
    private void HandleAfter()
    {
        var totalMilliseconds = (DateTime.UtcNow - StartTime).TotalMilliseconds;
        if (!_hostEnvironment.IsProduction() || totalMilliseconds > 500)
        {
            _logger.LogInformation("Finished in {TotalMilliseconds}ms.", totalMilliseconds);
        }
    }
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

        // execute

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

## Why IGet.GetAll?

[IGet](https://www.nuget.org/packages/iget) gets single classes, but cannot provide multiple in one call. If you like MediatR's INotification-INotificationHandler combination then you will probably feel the need for something that automatically collects multiple INotificationHandlers - that is one of the things that `i.GetAll<T>()` can do - it has the following benefits:

- you declare whatever interfaces and base classes you like; this package does not force you to use pre-defined interfaces.
- after `i.GetAll<T>()` you explicitly show how you handle exceptions, making your code easier to understand.
- after `i.GetAll<T>()` you explicitly show that you use `Task.WhenAll` or `foreach` with `await` (or synchronous code).

Also note: no matter how complicated your interfaces or generic base classes are - think about `IMyInterface<SomeClass, NestedBaseClass<AnotherClass, AndMore>>` - no additional configuration is needed.


## About IGet.GetAll's performance

Each time you use `i.GetAll<T>()` for a new type `T`, the collected `Type[]` is stored in a `ConcurrentDictionary`. The next time you call `i.GetAll<T>()` for the same type `T`, no assembly scanning is done.


## i.GetAll&lt;T&gt;() examples

#### Example 1
This example shows how you can create a generic notification publisher.

Declare an interface you like:
```csharp
public interface INotificationHandler<TNotification>
{
    Task HandleAsync(TNotification notification, CancellationToken cancellationToken);
}
```

Implement the interface:
```csharp
public class NotificationA { }

public class HandlerA1 : INotificationHandler<NotificationA>
{
    private readonly ILogger<HandlerA1> _logger;
    public HandlerA1(ILogger<HandlerA1> logger)
    {
        _logger = logger;
    }
    public async Task HandleAsync(NotificationA notification, CancellationToken cancellationToken)
    {
        // do stuff
    }
}

public class HandlerA2 : INotificationHandler<NotificationA>
{
    private readonly IConnectionFactory _connectionFactory;
    public HandlerA2(IConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }
    public async Task HandleAsync(NotificationA notification, CancellationToken cancellationToken)
    {
        // do stuff
    }
}

public class NotificationB { }

public class HandlerB1 : INotificationHandler<NotificationB>
{
    private readonly ILogger<Handler1> _logger;
    public HandlerB1(ILogger<Handler1> logger)
    {
        _logger = logger;
    }
    public async Task HandleAsync(NotificationB notification, CancellationToken cancellationToken)
    {
        // do stuff
    }
}
```

Create a generic notification publisher for all your notification types:
```csharp
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
```
Publish notifications:
```csharp
// invokes HandlerA1 and HandlerA2:
await i.Get<NotificationPublisher<NotificationA>>().Publish(notificationA);
// invokes HandlerB1:
await i.Get<NotificationPublisher<NotificationB>>().Publish(notificationB);
```

#### Example 2
Note that because the `NotificationPublisher<TNotification>` of the previous example is in your own repository, you can easily tweak it. Do you want some handlers to have priority? Add a second interface `IPrio` to some handlers and execute those first. Do you want to fire them all first and then call `Task.WhenAll`? You are in control - without reading any docs:
```csharp
public async Task Publish(TNotification notification, CancellationToken cancellationToken = default)
{
    var handlers = i.GetAll<INotificationHandler<TNotification>>();
    var prioTasks = handlers.Where(handler => handler is IPrio).Select(handler => GetSafeTask(handler));
    await Task.WhenAll(prioTasks);
    foreach (var handler in handlers.Where(handler => handler is not IPrio))
    {
        await GetSafeTask(handler);
    }

    async Task GetSafeTask(INotificationHandler<TNotification> handler)
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
```

#### Example 3
Just to make sure it's clear: `i.GetAll<T>()` can be used for any type of interface or base class. Do you need to get a set of validator classes for a certain request? Get them:
```csharp
i.GetAll<AbstractValidator<UpdateUserCommand>>()
```

## Try it out

The examples above give an idea of how you can be creative with IGet. Share your own examples online to spread the word about IGet.
