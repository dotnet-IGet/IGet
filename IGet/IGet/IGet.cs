using Microsoft.Extensions.DependencyInjection;

namespace System
{
    public interface IGet
    {
        T Get<T>();
        T Get<T>(Type type);
    }

    public static class __IGet
    {
        public static void AddIGet(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<System.IGet, IGet>();
        }

        public class IGet : System.IGet
        {
            private readonly IServiceProvider _serviceProvider;

            public IGet(IServiceProvider serviceProvider)
            {
                _serviceProvider = serviceProvider;
            }

            public T Get<T>()
            {
                return ActivatorUtilities.CreateInstance<T>(_serviceProvider);
            }

            public T Get<T>(Type type)
            {
                return (T)ActivatorUtilities.CreateInstance(_serviceProvider, type);
            }
        }
    }
}
