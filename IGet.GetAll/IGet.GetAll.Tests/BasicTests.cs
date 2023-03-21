namespace IGetAll.BasicTests;

public class BasicTests : IDisposable
{
    private readonly IGet i;
    private readonly TestServices services;

    public BasicTests()
    {
        services = new TestServices();
        services.AddIGet();
        services.AddIGetAll(new[] { typeof(BasicTests).Assembly });
        i = services.GetService<IGet>()!;
    }

    [Fact]
    public void GetsClassesThatImplementTheInterface()
    {
        var handlers = i.GetAll<IMyGenericInterface<NotificationA>>().ToArray();
        Assert.Equal(2, handlers.Length);
        Assert.Contains(typeof(HandlerA1), handlers.Select(x => x.GetType()));
        Assert.Contains(typeof(HandlerA2), handlers.Select(x => x.GetType()));
        Assert.Equal(typeof(HandlerB1), Assert.Single(i.GetAll<IMyGenericInterface<NotificationB>>()).GetType());
    }

    [Fact]
    public void GetsClassesThatImplementMultipleInterfaces()
    {
        var typeofHandlersC = i.GetAll<IMyGenericInterface<NotificationC>>().Single().GetType();
        var typeofHandlersD = i.GetAll<IMyGenericInterface<NotificationD>>().Single().GetType();
        Assert.Equal(typeof(HandlerCxD), typeofHandlersC);
        Assert.Equal(typeof(HandlerCxD), typeofHandlersD);

        // Also verify that the memory has been updated:
        var memory = services.GetService<__IGetAll.IGetAllProvider.Memory>()!;
        Assert.Equal(2, memory.TypesPerInterface.Keys.Count);
    }

    [Fact]
    public void ReturnsEmptyResultForNotImplementedInterfaces()
    {
        Assert.Empty(i.GetAll<IMyGenericInterface<BasicTests>>());
        Assert.Empty(i.GetAll<IMyNotImplementedInterface>());

        // Also verify that the memory has been updated:
        var memory = services.GetService<__IGetAll.IGetAllProvider.Memory>()!;
        Assert.Equal(2, memory.TypesPerInterface.Keys.Count);
        Assert.Empty(memory.TypesPerInterface[typeof(IMyGenericInterface<BasicTests>)]);
        Assert.Empty(memory.TypesPerInterface[typeof(IMyNotImplementedInterface)]);
    }

    [Fact]
    public void GetsClassesWithNonGenericInterfaces()
    {
        var result = Assert.Single(i.GetAll<IMyNonGenericInterface>());
        Assert.Equal(typeof(MyClassWithNonGenericInterface), result.GetType());
    }

    [Fact]
    public void GetsClassesViaBaseClass()
    {
        var handlers = i.GetAll<MyBaseClass>().ToArray();
        Assert.Equal(2, handlers.Length);
        Assert.Contains(typeof(Inheriter1), handlers.Select(x => x.GetType()));
        Assert.Contains(typeof(Inheriter2), handlers.Select(x => x.GetType()));
    }

    [Fact]
    public void GetsClassesViaGenericBaseClass()
    {
        var handler = Assert.Single(i.GetAll<MyGenericBaseClass<string>>());
        Assert.Equal(typeof(GenericInheriter1), handler.GetType());
    }

    [Fact]
    public void ReturnsEmptyForNotImplementedGenericBaseClass()
    {
        Assert.Empty(i.GetAll<MyGenericBaseClass<bool>>());
    }

    [Fact]
    public void ReturnsEmptyForNotImplementedBaseClass()
    {
        Assert.Empty(i.GetAll<NonImplementedBaseClass>());
    }

    /* All classes defined below could have dependencies in their constructor and the results would be the same
     * if those dependencies are added to the service collection. */
    public abstract class NonImplementedBaseClass { }
    public abstract class MyBaseClass { }
    public class Inheriter1 : MyBaseClass { }
    public class Inheriter2 : MyBaseClass { }

    public abstract class MyGenericBaseClass<T> { }
    public class GenericInheriter1 : MyGenericBaseClass<string> { }

    public interface IMyGenericInterface<TRequest> { }
    public interface IMyNonGenericInterface { }
    public interface IMyNotImplementedInterface { }

    public class MyClassWithNonGenericInterface : IMyNonGenericInterface { }
    public class NotificationA { }
    public class HandlerA1 : IMyGenericInterface<NotificationA> { }
    public class HandlerA2 : IMyGenericInterface<NotificationA> { }

    public class NotificationB { }
    public class HandlerB1 : IMyGenericInterface<NotificationB> { }

    public class NotificationC {  }
    public class NotificationD { }
    public class HandlerCxD : IMyGenericInterface<NotificationC>, IMyGenericInterface<NotificationD> { }


    public void Dispose()
    {
    }
}