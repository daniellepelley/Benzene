using System;
using System.Threading.Tasks;
using Benzene.Abstractions.DI;
using Benzene.Core.Middleware;
using Xunit;

namespace Benzene.Test.Core.Middleware;

/// <summary>
/// The <see cref="MiddlewareRouter{TRequest,TContext}"/> base used to hardcode <c>Name</c> to
/// <c>"MiddlewareRouter"</c>, so every flavour of router looked identical in tracing. It now defaults
/// to the concrete router's own type name, so each one is distinguishable without touching any of the
/// inheritors; an inheritor can still override with a custom name.
/// </summary>
public class MiddlewareRouterNameTest
{
    private sealed class FirstRouter : MiddlewareRouter<string, object>
    {
        public FirstRouter() : base(new NullServiceResolver()) { }
        protected override bool CanHandle(string request) => false;
        protected override Task HandleFunction(string request, object context, IServiceResolverFactory serviceResolverFactory) => Task.CompletedTask;
        protected override string TryExtractRequest(object context) => null;
    }

    private sealed class SecondRouter : MiddlewareRouter<string, object>
    {
        public SecondRouter() : base(new NullServiceResolver()) { }
        protected override bool CanHandle(string request) => false;
        protected override Task HandleFunction(string request, object context, IServiceResolverFactory serviceResolverFactory) => Task.CompletedTask;
        protected override string TryExtractRequest(object context) => null;
    }

    private sealed class NamedRouter : MiddlewareRouter<string, object>
    {
        public NamedRouter() : base(new NullServiceResolver()) { }
        public override string Name => "custom-name";
        protected override bool CanHandle(string request) => false;
        protected override Task HandleFunction(string request, object context, IServiceResolverFactory serviceResolverFactory) => Task.CompletedTask;
        protected override string TryExtractRequest(object context) => null;
    }

    [Fact]
    public void Name_DefaultsToTheConcreteRouterTypeName()
    {
        Assert.Equal("FirstRouter", new FirstRouter().Name);
        Assert.Equal("SecondRouter", new SecondRouter().Name);
    }

    [Fact]
    public void Name_CanStillBeOverridden()
    {
        Assert.Equal("custom-name", new NamedRouter().Name);
    }
}
