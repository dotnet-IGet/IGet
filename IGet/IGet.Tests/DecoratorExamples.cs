using TestHelpers.Mocks;

namespace Tests;

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
}

public class DecoratorExample
{
    public async Task Example()
    {
        IGet i = default!;
        var request = new Request();
        var decoratedHandler = i.Get<MyHandler>().DecorateWithPerformanceProfiler();
        var result = await decoratedHandler.HandleAsync(request);
    }

    public class Request { }

    public class MyHandler : IDecoratableHandler<Request, Result>
    {
        public Task<Result> HandleAsync(Request request)
        {
            throw new NotImplementedException();
        }
    }
}
