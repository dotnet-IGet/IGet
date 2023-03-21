using Microsoft.Extensions.DependencyInjection;
using System.Collections;

namespace TestHelpers;

public class TestServices : IServiceCollection, IServiceProvider
{
    public TestServices()
    {
        x.Add(new ServiceDescriptor(typeof(IServiceProvider), this));
    }
    public readonly List<ServiceDescriptor> x = new();
    public ServiceDescriptor this[int index] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public int Count => throw new NotImplementedException();

    public bool IsReadOnly => throw new NotImplementedException();

    public void Add(ServiceDescriptor item)
    {
        x.Add(item);
    }

    public void Clear()
    {
        throw new NotImplementedException();
    }

    public bool Contains(ServiceDescriptor item)
    {
        throw new NotImplementedException();
    }

    public void CopyTo(ServiceDescriptor[] array, int arrayIndex)
    {
        throw new NotImplementedException();
    }

    public IEnumerator<ServiceDescriptor> GetEnumerator()
    {
        throw new NotImplementedException();
    }

    public object? GetService(Type serviceType)
    {
        var service = x.FirstOrDefault(x => x.ServiceType == serviceType);
        if (service is null)
        {
            throw new Exception($"Dependency '{serviceType.FullName}' could not be found by the {nameof(IServiceProvider)}.");
        }
        if (service.ImplementationInstance is object obj)
        {
            return obj;
        }
        return ActivatorUtilities.CreateInstance(this, service.ImplementationType!);
    }

    public int IndexOf(ServiceDescriptor item)
    {
        throw new NotImplementedException();
    }

    public void Insert(int index, ServiceDescriptor item)
    {
        throw new NotImplementedException();
    }

    public bool Remove(ServiceDescriptor item)
    {
        throw new NotImplementedException();
    }

    public void RemoveAt(int index)
    {
        throw new NotImplementedException();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        throw new NotImplementedException();
    }
}

