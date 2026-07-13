using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Core.Middleware;
using Benzene.Grpc.Test.Helpers;
using Benzene.Grpc.Test.Protos;
using Benzene.Microsoft.Dependencies;
using Benzene.Results;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Benzene.Grpc.Test;

public class GrpcMethodHandlerTest
{
    [Fact]
    public async Task HandleAsync_WhenNoHandlerRegistered_ThrowsRpcExceptionNotFoundWithTrailer()
    {
        var pipeline = BuildPipeline(out var serviceResolverFactory);
        var handler = new GrpcMethodHandler(
            new GrpcMethodDefinition("/benzene.test.TestService/Echo", "grpc-test-topic-with-no-handler"), serviceResolverFactory, pipeline);
        var callContext = TestCallContext.Create();

        var exception = await Assert.ThrowsAsync<RpcException>(() =>
            handler.HandleAsync<EchoRequest, EchoReply>(new EchoRequest { Name = "x" }, callContext));

        Assert.Equal(StatusCode.NotFound, exception.StatusCode);
        Assert.Contains(callContext.ResponseTrailers, e => e.Key == "benzene-status" && e.Value == BenzeneResultStatus.NotFound);
    }

    [Fact]
    public async Task HandleAsync_WhenPipelineThrowsOperationCanceledException_ThrowsRpcExceptionCancelled()
    {
        var pipeline = BuildCancellingPipeline(out var serviceResolverFactory);
        var handler = new GrpcMethodHandler(new GrpcMethodDefinition("/x/y", "topic"), serviceResolverFactory, pipeline);

        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var callContext = TestCallContext.Create(cancellationToken: cts.Token);

        var exception = await Assert.ThrowsAsync<RpcException>(() =>
            handler.HandleAsync<EchoRequest, EchoReply>(new EchoRequest { Name = "x" }, callContext));

        Assert.Equal(StatusCode.Cancelled, exception.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_WhenPipelineThrowsOperationCanceledExceptionPastDeadline_ThrowsRpcExceptionDeadlineExceeded()
    {
        var pipeline = BuildCancellingPipeline(out var serviceResolverFactory);
        var handler = new GrpcMethodHandler(new GrpcMethodDefinition("/x/y", "topic"), serviceResolverFactory, pipeline);

        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var callContext = TestCallContext.Create(cancellationToken: cts.Token, deadline: DateTime.UtcNow.AddMilliseconds(-1));

        var exception = await Assert.ThrowsAsync<RpcException>(() =>
            handler.HandleAsync<EchoRequest, EchoReply>(new EchoRequest { Name = "x" }, callContext));

        Assert.Equal(StatusCode.DeadlineExceeded, exception.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_HandlerCanObserveCancellationTokenViaAccessor()
    {
        var pipeline = BuildPipeline(out var serviceResolverFactory);
        var handler = new GrpcMethodHandler(
            new GrpcMethodDefinition("/benzene.test.TestService/Echo", "grpc-test-accessor-topic"), serviceResolverFactory, pipeline);

        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var callContext = TestCallContext.Create(cancellationToken: cts.Token);

        var reply = await handler.HandleAsync<EchoRequest, EchoReply>(new EchoRequest { Name = "x" }, callContext);

        Assert.Equal("cancelled", reply.Message);
    }

    private static IMiddlewarePipeline<GrpcContext> BuildPipeline(out IServiceResolverFactory serviceResolverFactory)
    {
        var services = new ServiceCollection();
        services.AddLogging();

        var container = new MicrosoftBenzeneServiceContainer(services);
        container.AddBenzene().AddBenzeneMessage().AddGrpcMessageHandlers();

        var pipelineBuilder = new MiddlewarePipelineBuilder<GrpcContext>(container);
        pipelineBuilder.UseMessageHandlers();

        serviceResolverFactory = container.CreateServiceResolverFactory();
        return pipelineBuilder.Build();
    }

    private static IMiddlewarePipeline<GrpcContext> BuildCancellingPipeline(out IServiceResolverFactory serviceResolverFactory)
    {
        var services = new ServiceCollection();
        services.AddLogging();

        var container = new MicrosoftBenzeneServiceContainer(services);
        container.AddBenzene().AddBenzeneMessage().AddGrpcMessageHandlers();

        var pipelineBuilder = new MiddlewarePipelineBuilder<GrpcContext>(container);
        pipelineBuilder.Use((context, next) =>
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            return next();
        });

        serviceResolverFactory = container.CreateServiceResolverFactory();
        return pipelineBuilder.Build();
    }
}
