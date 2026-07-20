using System;
using System.Threading.Tasks;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Azure.Function.Core;
using Benzene.Core.Exceptions;
using Moq;
using Xunit;

namespace Benzene.Test.Azure;

/// <summary>
/// Covers the discriminator-key dispatch added for #26: two entry point applications of the *same*
/// request type coexist in one app when registered under different names, and <c>HandleAsync(..., name)</c>
/// routes to the one whose key matches. A null name keeps the original type-only first-match behaviour.
/// </summary>
public class AzureFunctionAppDispatchTest
{
    [Fact]
    public async Task HandleAsync_WithName_RoutesToTheKeyedEntryPointOfTheSameType()
    {
        var appA = new FakeEntryPoint<string[]>();
        var appB = new FakeEntryPoint<string[]>();

        var app = new AzureFunctionApp(
            new (string?, Func<IServiceResolverFactory, IEntryPointMiddlewareApplication>)[]
            {
                ("a", _ => appA),
                ("b", _ => appB)
            },
            Mock.Of<IServiceResolverFactory>());

        await app.HandleAsync(new[] { "for-b" }, "b");

        Assert.Null(appA.Received);
        Assert.Equal(new[] { "for-b" }, appB.Received);
    }

    [Fact]
    public async Task HandleAsync_WithTheOtherName_RoutesToTheOtherKeyedEntryPoint()
    {
        var appA = new FakeEntryPoint<string[]>();
        var appB = new FakeEntryPoint<string[]>();

        var app = new AzureFunctionApp(
            new (string?, Func<IServiceResolverFactory, IEntryPointMiddlewareApplication>)[]
            {
                ("a", _ => appA),
                ("b", _ => appB)
            },
            Mock.Of<IServiceResolverFactory>());

        await app.HandleAsync(new[] { "for-a" }, "a");

        Assert.Equal(new[] { "for-a" }, appA.Received);
        Assert.Null(appB.Received);
    }

    [Fact]
    public async Task HandleAsync_WithNoName_FallsBackToTheFirstTypeMatch()
    {
        var appA = new FakeEntryPoint<string[]>();
        var appB = new FakeEntryPoint<string[]>();

        var app = new AzureFunctionApp(
            new (string?, Func<IServiceResolverFactory, IEntryPointMiddlewareApplication>)[]
            {
                ("a", _ => appA),
                ("b", _ => appB)
            },
            Mock.Of<IServiceResolverFactory>());

        await app.HandleAsync(new[] { "no-name" });

        // Type-only match keeps the pre-#26 single-registration behaviour: first wins.
        Assert.Equal(new[] { "no-name" }, appA.Received);
        Assert.Null(appB.Received);
    }

    [Fact]
    public async Task HandleAsync_WithAnUnknownName_ThrowsNamingTheKey()
    {
        var app = new AzureFunctionApp(
            new (string?, Func<IServiceResolverFactory, IEntryPointMiddlewareApplication>)[]
            {
                ("a", _ => new FakeEntryPoint<string[]>())
            },
            Mock.Of<IServiceResolverFactory>());

        var exception = await Assert.ThrowsAsync<BenzeneException>(() =>
            app.HandleAsync(new[] { "x" }, "missing"));

        // The requested key is surfaced so a name typo is self-diagnosing.
        Assert.Contains("missing", exception.Message);
        // ...and the registered key is listed.
        Assert.Contains("a:", exception.Message);
    }

    private class FakeEntryPoint<TEvent> : IEntryPointMiddlewareApplication<TEvent>
    {
        public TEvent Received { get; private set; }

        public Task SendAsync(TEvent @event)
        {
            Received = @event;
            return Task.CompletedTask;
        }
    }
}
