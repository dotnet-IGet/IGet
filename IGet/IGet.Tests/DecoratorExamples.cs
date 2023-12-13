using TestHelpers.Mocks;

namespace Tests;

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

public class DecoratorExample
{
    public async Task Example()
    {
        IGet i = default!;
        var request = new Request();
        {
            var decoratedHandler = i.Get<MyHandler>().DecorateWithPerformanceProfiler();
            var result = await decoratedHandler.HandleAsync(request);
        }

        {
            var result = await i.Get<MyHandler>()
                .DecorateWithPerformanceProfiler()
                .HandleAsync(request);
        }

        {
            var decoratedHandler = i.Get<MyHandler>().WithPerformanceLogging(i);
            var result = await decoratedHandler.HandleAsync(request);
        }
    }

    public class Request { }

    public class MyHandler : IRequestHandler<Request, Result>
    {
        public Task<Result> HandleAsync(Request request)
        {
            throw new NotImplementedException();
        }
    }
}
