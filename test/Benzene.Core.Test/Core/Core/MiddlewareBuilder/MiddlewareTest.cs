using System;
using System.Linq;
using System.Threading.Tasks;
using Benzene.Core.Middleware;
using Benzene.Microsoft.Dependencies;
using Benzene.Test.Logging.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Benzene.Test.Core.Core.MiddlewareBuilder;

public class MiddlewareTest
{
    [Fact]
    public async Task ExceptionHandler_CaughtException()
    {
        var services = new ServiceCollection();
        var container = new MicrosoftBenzeneServiceContainer(services);
        var builder = new MiddlewarePipelineBuilder<object>(container);

        var caught = false;
        builder.UseExceptionHandler((_, _) => caught = true);
        builder.Use((_, _) => throw new Exception("Test"));

        var pipeline = builder.Build();

        using var factory = new MicrosoftServiceResolverFactory(services);
        using var resolver = factory.CreateScope();

        await pipeline.HandleAsync(new object(), resolver);

        Assert.True(caught);
    }

    [Fact]
    public async Task ExceptionHandler_CaughtException_IsLogged()
    {
        var fakeLoggerFactory = new FakeLoggerFactory();
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(fakeLoggerFactory);
        var container = new MicrosoftBenzeneServiceContainer(services);
        var builder = new MiddlewarePipelineBuilder<object>(container);

        builder.UseExceptionHandler((_, _) => { });
        builder.Use((_, _) => throw new Exception("Test"));

        var pipeline = builder.Build();

        using var factory = new MicrosoftServiceResolverFactory(services);
        using var resolver = factory.CreateScope();

        await pipeline.HandleAsync(new object(), resolver);

        var entry = Assert.Single(fakeLoggerFactory.Collector.Entries.Where(x => x.Level == LogLevel.Error));
        Assert.Equal("Unhandled exception caught in middleware pipeline", entry.Message);
        Assert.Equal("Test", entry.Exception.Message);
    }

    [Fact]
    public async Task ExceptionHandler_ExceptionRethrownByHandler_IsStillLogged()
    {
        var fakeLoggerFactory = new FakeLoggerFactory();
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(fakeLoggerFactory);
        var container = new MicrosoftBenzeneServiceContainer(services);
        var builder = new MiddlewarePipelineBuilder<object>(container);

        builder.UseExceptionHandler((_, ex) => throw ex);
        builder.Use((_, _) => throw new Exception("Test"));

        var pipeline = builder.Build();

        using var factory = new MicrosoftServiceResolverFactory(services);
        using var resolver = factory.CreateScope();

        await Assert.ThrowsAsync<Exception>(() => pipeline.HandleAsync(new object(), resolver));

        Assert.Single(fakeLoggerFactory.Collector.Entries.Where(x => x.Level == LogLevel.Error));
    }

    [Fact]
    public async Task ShortCircuit_DoesNotResolveDownstreamMiddleware()
    {
        var services = new ServiceCollection();
        var container = new MicrosoftBenzeneServiceContainer(services);
        var builder = new MiddlewarePipelineBuilder<object>(container);

        var downstreamResolved = false;
        // First middleware short-circuits (never calls next).
        builder.Use((_, _) => Task.CompletedTask);
        // Second middleware's factory records whether it was ever resolved.
        builder.Use((Benzene.Abstractions.DI.IServiceResolver _) =>
        {
            downstreamResolved = true;
            return (_, next) => next();
        });

        var pipeline = builder.Build();

        using var factory = new MicrosoftServiceResolverFactory(services);
        using var resolver = factory.CreateScope();

        await pipeline.HandleAsync(new object(), resolver);

        // A middleware the pipeline never reaches must not be constructed - the chain used to resolve
        // (and factory-wrap) every middleware up front, so a short-circuited downstream still ran its
        // constructor/DI resolution, its ctor-injected dependencies had to be resolvable before the
        // pipeline even started, and UseExceptionHandler couldn't cover a downstream construction throw.
        Assert.False(downstreamResolved);
    }

    [Fact]
    public void Builder_Clear()
    {
        var services = new ServiceCollection();
        var container = new MicrosoftBenzeneServiceContainer(services);
        var builder = new MiddlewarePipelineBuilder<object>(container);

        builder.Use((_, _) => Task.CompletedTask);
        Assert.Single(builder.GetItems());

        builder.Clear();
        Assert.Empty(builder.GetItems());
    }
}
