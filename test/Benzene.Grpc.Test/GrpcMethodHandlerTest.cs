using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Core.Middleware;
using Benzene.Grpc.TestHelpers;
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
        var callContext = TestServerCallContext.Create();

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
        var callContext = TestServerCallContext.Create(cancellationToken: cts.Token);

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
        var callContext = TestServerCallContext.Create(cancellationToken: cts.Token, deadline: DateTime.UtcNow.AddMilliseconds(-1));

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
        var callContext = TestServerCallContext.Create(cancellationToken: cts.Token);

        var reply = await handler.HandleAsync<EchoRequest, EchoReply>(new EchoRequest { Name = "x" }, callContext);

        Assert.Equal("cancelled", reply.Message);
    }

    [Fact]
    public async Task HandleAsync_ValidationError_AttachesGoogleRpcStatusWithFieldViolations()
    {
        var pipeline = BuildResultPipeline(BenzeneResult.ValidationError("Name is required"), out var serviceResolverFactory);
        var handler = new GrpcMethodHandler(new GrpcMethodDefinition("/x/y", "topic"), serviceResolverFactory, pipeline);
        var callContext = TestServerCallContext.Create();

        var exception = await Assert.ThrowsAsync<RpcException>(() =>
            handler.HandleAsync<EchoRequest, EchoReply>(new EchoRequest { Name = "x" }, callContext));

        Assert.Equal(StatusCode.InvalidArgument, exception.StatusCode);
        // The flat trailer is still there...
        Assert.Contains(callContext.ResponseTrailers, e => e.Key == "benzene-status" && e.Value == BenzeneResultStatus.ValidationError);

        // ...and the structured google.rpc.Status carries the field violations.
        var detailsBytes = callContext.ResponseTrailers.GetValueBytes("grpc-status-details-bin");
        Assert.NotNull(detailsBytes);
        var richStatus = Google.Rpc.Status.Parser.ParseFrom(detailsBytes);
        var badRequest = richStatus.Details[0].Unpack<Google.Rpc.BadRequest>();
        Assert.Equal("Name is required", badRequest.FieldViolations[0].Description);
    }

    private static IMiddlewarePipeline<GrpcContext> BuildResultPipeline(Benzene.Abstractions.Results.IBenzeneResult result, out IServiceResolverFactory serviceResolverFactory)
    {
        var services = new ServiceCollection();
        services.AddLogging();

        var container = new MicrosoftBenzeneServiceContainer(services);
        container.AddBenzene().AddBenzeneMessage().AddGrpcMessageHandlers();

        var pipelineBuilder = new MiddlewarePipelineBuilder<GrpcContext>(container);
        pipelineBuilder.Use((context, _) =>
        {
            context.MessageHandlerResult = new MessageHandlerResult(result);
            return Task.CompletedTask;
        });

        serviceResolverFactory = container.CreateServiceResolverFactory();
        return pipelineBuilder.Build();
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
