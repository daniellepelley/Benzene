using System;
using System.Threading.Tasks;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Core.Messages.BenzeneMessage;
using Benzene.Core.Middleware;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Benzene.Test.Core.Core.MiddlewareBuilder;

public class ExtensionsTest
{
    private class InnerContext
    {
        public string Value { get; set; }
    }

    private class TestConverter : IContextConverter<BenzeneMessageContext, InnerContext>
    {
        public Task<InnerContext> CreateRequestAsync(BenzeneMessageContext contextIn)
            => Task.FromResult(new InnerContext { Value = contextIn.BenzeneMessageRequest.Body });

        public Task MapResponseAsync(BenzeneMessageContext contextIn, InnerContext contextOut)
        {
            contextIn.BenzeneMessageResponse.Body = contextOut.Value;
            return Task.CompletedTask;
        }
    }

    private class AlwaysTruePredicate : IContextPredicate<BenzeneMessageContext>
    {
        public bool Check(BenzeneMessageContext context, IServiceResolver serviceResolver) => true;
    }

    private class AlwaysFalsePredicate : IContextPredicate<BenzeneMessageContext>
    {
        public bool Check(BenzeneMessageContext context, IServiceResolver serviceResolver) => false;
    }

    private static (MiddlewarePipelineBuilder<BenzeneMessageContext> Builder, ServiceCollection Services) CreateBuilder()
    {
        var services = new ServiceCollection();
        var builder = new MiddlewarePipelineBuilder<BenzeneMessageContext>(new MicrosoftBenzeneServiceContainer(services));
        return (builder, services);
    }

    [Fact]
    public async Task Use_ServiceResolverFunc_AddsMiddleware()
    {
        var (builder, services) = CreateBuilder();

        Func<IServiceResolver, Func<BenzeneMessageContext, Func<Task>, Task>> func = _ => (context, next) =>
        {
            context.BenzeneMessageResponse.Body = "used";
            return next();
        };

        builder.Use(func);

        using var factory = new MicrosoftServiceResolverFactory(services);
        using var serviceResolver = factory.CreateScope();

        var context = new BenzeneMessageContext(new BenzeneMessageRequest());
        await builder.Build().HandleAsync(context, serviceResolver);

        Assert.Equal("used", context.BenzeneMessageResponse.Body);
    }

    [Fact]
    public async Task Split_FuncPredicate_True_ExecutesBranchPipelineInsteadOfNext()
    {
        var (builder, services) = CreateBuilder();

        builder
            .Split(_ => true, branch => branch.OnRequest(ctx => ctx.BenzeneMessageResponse.Body = "branch"))
            .OnRequest(ctx => ctx.BenzeneMessageResponse.Body = "main");

        using var factory = new MicrosoftServiceResolverFactory(services);
        using var serviceResolver = factory.CreateScope();

        var context = new BenzeneMessageContext(new BenzeneMessageRequest());
        await builder.Build().HandleAsync(context, serviceResolver);

        Assert.Equal("branch", context.BenzeneMessageResponse.Body);
    }

    [Fact]
    public async Task Split_FuncPredicate_False_ContinuesToNextMiddleware()
    {
        var (builder, services) = CreateBuilder();

        builder
            .Split(_ => false, branch => branch.OnRequest(ctx => ctx.BenzeneMessageResponse.Body = "branch"))
            .OnRequest(ctx => ctx.BenzeneMessageResponse.Body = "main");

        using var factory = new MicrosoftServiceResolverFactory(services);
        using var serviceResolver = factory.CreateScope();

        var context = new BenzeneMessageContext(new BenzeneMessageRequest());
        await builder.Build().HandleAsync(context, serviceResolver);

        Assert.Equal("main", context.BenzeneMessageResponse.Body);
    }

    [Fact]
    public async Task Split_ContextPredicate_True_ExecutesBranchPipelineInsteadOfNext()
    {
        var (builder, services) = CreateBuilder();

        builder
            .Split(new AlwaysTruePredicate(), branch => branch.OnRequest(ctx => ctx.BenzeneMessageResponse.Body = "branch"))
            .OnRequest(ctx => ctx.BenzeneMessageResponse.Body = "main");

        using var factory = new MicrosoftServiceResolverFactory(services);
        using var serviceResolver = factory.CreateScope();

        var context = new BenzeneMessageContext(new BenzeneMessageRequest());
        await builder.Build().HandleAsync(context, serviceResolver);

        Assert.Equal("branch", context.BenzeneMessageResponse.Body);
    }

    [Fact]
    public async Task Split_ContextPredicate_False_ContinuesToNextMiddleware()
    {
        var (builder, services) = CreateBuilder();

        builder
            .Split(new AlwaysFalsePredicate(), branch => branch.OnRequest(ctx => ctx.BenzeneMessageResponse.Body = "branch"))
            .OnRequest(ctx => ctx.BenzeneMessageResponse.Body = "main");

        using var factory = new MicrosoftServiceResolverFactory(services);
        using var serviceResolver = factory.CreateScope();

        var context = new BenzeneMessageContext(new BenzeneMessageRequest());
        await builder.Build().HandleAsync(context, serviceResolver);

        Assert.Equal("main", context.BenzeneMessageResponse.Body);
    }

    [Fact]
    public async Task Convert_WithConverterAndBuiltPipeline_ConvertsAndMapsResponseBack()
    {
        var (builder, services) = CreateBuilder();

        var innerPipeline = builder.Create<InnerContext>()
            .OnRequest(inner => inner.Value = inner.Value + "-handled")
            .Build();

        builder.Convert(new TestConverter(), innerPipeline);

        using var factory = new MicrosoftServiceResolverFactory(services);
        using var serviceResolver = factory.CreateScope();

        var context = new BenzeneMessageContext(new BenzeneMessageRequest { Body = "request" });
        await builder.Build().HandleAsync(context, serviceResolver);

        Assert.Equal("request-handled", context.BenzeneMessageResponse.Body);
    }

    [Fact]
    public async Task Convert_WithConverterAndInlineBuilder_ConvertsAndMapsResponseBack()
    {
        var (builder, services) = CreateBuilder();

        builder.Convert(new TestConverter(), (IMiddlewarePipelineBuilder<InnerContext> inner) =>
            inner.OnRequest(x => x.Value = x.Value + "-handled"));

        using var factory = new MicrosoftServiceResolverFactory(services);
        using var serviceResolver = factory.CreateScope();

        var context = new BenzeneMessageContext(new BenzeneMessageRequest { Body = "request" });
        await builder.Build().HandleAsync(context, serviceResolver);

        Assert.Equal("request-handled", context.BenzeneMessageResponse.Body);
    }

    [Fact]
    public async Task Convert_WithInlineFuncsAndBuiltPipeline_ConvertsAndMapsResponseBack()
    {
        var (builder, services) = CreateBuilder();

        var innerPipeline = builder.Create<InnerContext>()
            .OnRequest(inner => inner.Value = inner.Value + "-handled")
            .Build();

        builder.Convert(
            (BenzeneMessageContext ctx) => new InnerContext { Value = ctx.BenzeneMessageRequest.Body },
            (ctx, inner) => ctx.BenzeneMessageResponse.Body = inner.Value,
            innerPipeline);

        using var factory = new MicrosoftServiceResolverFactory(services);
        using var serviceResolver = factory.CreateScope();

        var context = new BenzeneMessageContext(new BenzeneMessageRequest { Body = "request" });
        await builder.Build().HandleAsync(context, serviceResolver);

        Assert.Equal("request-handled", context.BenzeneMessageResponse.Body);
    }

    [Fact]
    public async Task Convert_WithInlineFuncsAndInlineBuilder_ConvertsAndMapsResponseBack()
    {
        var (builder, services) = CreateBuilder();

        builder.Convert(
            (BenzeneMessageContext ctx) => new InnerContext { Value = ctx.BenzeneMessageRequest.Body },
            (ctx, inner) => ctx.BenzeneMessageResponse.Body = inner.Value,
            (IMiddlewarePipelineBuilder<InnerContext> inner) =>
                inner.OnRequest(x => x.Value = x.Value + "-handled"));

        using var factory = new MicrosoftServiceResolverFactory(services);
        using var serviceResolver = factory.CreateScope();

        var context = new BenzeneMessageContext(new BenzeneMessageRequest { Body = "request" });
        await builder.Build().HandleAsync(context, serviceResolver);

        Assert.Equal("request-handled", context.BenzeneMessageResponse.Body);
    }
}
