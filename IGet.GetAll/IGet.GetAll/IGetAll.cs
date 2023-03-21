using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace IGetAll
{
    public static class __IGetAll
    {
        public static void AddIGetAll(this IServiceCollection serviceCollection, IEnumerable<Assembly> assemblies)
        {
            serviceCollection.AddSingleton(new IGetAllProvider.Memory(assemblies.ToArray()));
        }

        public static IEnumerable<T> GetAll<T>(this IGet i)
        {
            return i.Get<IGetAllProvider>().GetAll<T>();
        }

        public class IGetAllProvider
        {
            private readonly IGet i;
            private readonly Assembly[] Assemblies;
            private readonly ConcurrentDictionary<Type, Type[]> TypesPerInterface;

            public IGetAllProvider(IGet iget, Memory memory)
            {
                i = iget;
                Assemblies = memory.Assemblies;
                TypesPerInterface = memory.TypesPerInterface;
            }

            public IEnumerable<T> GetAll<T>()
            {
                var interfaceType = typeof(T);
                if (!TypesPerInterface.TryGetValue(interfaceType, out Type[] types))
                {
                    types = Assemblies.SelectMany(assembly => assembly.DefinedTypes).Where(type => 
                        !type.IsAbstract 
                        && !type.IsInterface 
                        && interfaceType.IsAssignableFrom(type)).ToArray();
                    _ = TypesPerInterface.TryAdd(interfaceType, types);
                }
                foreach (var handlerType in types)
                {
                    yield return i.Get<T>(handlerType);
                }
            }

            public class Memory
            {
                public Memory(Assembly[] assemblies)
                {
                    Assemblies = assemblies;
                }

                public Assembly[] Assemblies { get; }
                public ConcurrentDictionary<Type, Type[]> TypesPerInterface { get; } = new ConcurrentDictionary<Type, Type[]>();
            }
        }
    }
}
