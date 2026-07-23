using System;
using System.Collections.Generic;
using System.Linq;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Messages;
using Benzene.Abstractions.Serialization;
using Benzene.Abstractions.WarmUp;
using Benzene.Core.MessageHandlers.WarmUp;
using Benzene.Core.Messages;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Benzene.Test.Core.Core.WarmUp;

public class WarmUpTest
{
    private sealed class FakeTask : IWarmUpTask
    {
        private readonly Action _onRun;
        public FakeTask(Action onRun) => _onRun = onRun;
        public void WarmUp(IServiceResolver resolver) => _onRun();
    }

    private sealed class SpySerializer : ISerializer
    {
        public List<Type> WarmedTypes { get; } = new();
        public string Serialize(Type type, object payload) => "{}";
        public string Serialize<T>(T payload) => "{}";
        public object? Deserialize(Type type, string payload)
        {
            WarmedTypes.Add(type);
            return Activator.CreateInstance(type);
        }
        public T? Deserialize<T>(string payload) => default;
    }

    private sealed class FakeDefinition : IMessageHandlerDefinition
    {
        public FakeDefinition(Type requestType, Type? responseType = null)
        {
            RequestType = requestType;
            ResponseType = responseType ?? typeof(object);
        }
        public ITopic Topic => new Topic("t");
        public Type RequestType { get; }
        public Type ResponseType { get; }
        public Type HandlerType => typeof(object);
    }

    private sealed class FakeFinder : IMessageHandlersFinder
    {
        private readonly IMessageHandlerDefinition[] _definitions;
        public FakeFinder(params Type[] requestTypes) => _definitions = requestTypes.Select(t => (IMessageHandlerDefinition)new FakeDefinition(t)).ToArray();
        public FakeFinder(params IMessageHandlerDefinition[] definitions) => _definitions = definitions;
        public IMessageHandlerDefinition[] FindDefinitions() => _definitions;
    }

    public class FooRequest { }
    public class BarRequest { }
    public class FooResponse { }

    private static IServiceResolverFactory FactoryFor(Action<MicrosoftBenzeneServiceContainer> configure)
    {
        var services = new ServiceCollection();
        var container = new MicrosoftBenzeneServiceContainer(services);
        configure(container);
        return new MicrosoftServiceResolverFactory(services.BuildServiceProvider());
    }

    [Fact]
    public void WarmUp_WithoutOptIn_DoesNotRunTasks()
    {
        var ran = false;
        var factory = FactoryFor(c => c.AddSingleton<IWarmUpTask>(_ => new FakeTask(() => ran = true)));

        factory.WarmUp();

        Assert.False(ran); // AddBenzeneWarmUp was not called
    }

    [Fact]
    public void WarmUp_WithOptIn_RunsRegisteredTasks()
    {
        var ran = false;
        var factory = FactoryFor(c =>
        {
            c.AddBenzeneWarmUp();
            c.AddSingleton<IWarmUpTask>(_ => new FakeTask(() => ran = true));
        });

        factory.WarmUp();

        Assert.True(ran);
    }

    [Fact]
    public void WarmUp_TaskThrows_IsSwallowed_AndOtherTasksStillRun()
    {
        var secondRan = false;
        var factory = FactoryFor(c =>
        {
            c.AddBenzeneWarmUp();
            c.AddSingleton<IWarmUpTask>(_ => new FakeTask(() => throw new InvalidOperationException("boom")));
            c.AddSingleton<IWarmUpTask>(_ => new FakeTask(() => secondRan = true));
        });

        factory.WarmUp(); // must not throw

        Assert.True(secondRan);
    }

    [Fact]
    public void SerializationWarmUpTask_WarmsEveryHandlerRequestType()
    {
        var spy = new SpySerializer();
        var factory = FactoryFor(c =>
        {
            c.AddSingleton<ISerializer>(_ => spy);
            c.AddSingleton<IMessageHandlersFinder>(_ => new FakeFinder(typeof(FooRequest), typeof(BarRequest)));
        });
        using var resolver = factory.CreateScope();

        new SerializationWarmUpTask().WarmUp(resolver);

        Assert.Contains(typeof(FooRequest), spy.WarmedTypes);
        Assert.Contains(typeof(BarRequest), spy.WarmedTypes);
    }

    [Fact]
    public void SerializationWarmUpTask_WarmsHandlerResponseType()
    {
        // A read-shaped handler: a trivial request but a distinct (potentially large) response payload.
        // The cold-start trace showed the response serialization was the dominant unwarmed cost.
        var spy = new SpySerializer();
        var factory = FactoryFor(c =>
        {
            c.AddSingleton<ISerializer>(_ => spy);
            c.AddSingleton<IMessageHandlersFinder>(_ =>
                new FakeFinder(new FakeDefinition(typeof(FooRequest), typeof(FooResponse))));
        });
        using var resolver = factory.CreateScope();

        new SerializationWarmUpTask().WarmUp(resolver);

        Assert.Contains(typeof(FooRequest), spy.WarmedTypes);
        Assert.Contains(typeof(FooResponse), spy.WarmedTypes);
    }
}
