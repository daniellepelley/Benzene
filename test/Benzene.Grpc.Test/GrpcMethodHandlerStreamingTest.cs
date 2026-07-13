using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Core.Middleware;
using Benzene.Grpc.Test.Helpers;
using Benzene.Grpc.Test.Protos;
using Benzene.Microsoft.Dependencies;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Benzene.Grpc.Test;

public class GrpcMethodHandlerStreamingTest
{
    [Fact]
    public async Task ServerStreamingAsync_YieldsAllItemsFromHandler()
    {
        var pipeline = BuildPipeline(out var serviceResolverFactory);
        var handler = new GrpcMethodHandler(
            new GrpcMethodDefinition("/benzene.test.TestService/Subscribe", "grpc-test-subscribe-topic"), serviceResolverFactory, pipeline);
        var writer = new FakeServerStreamWriter<SubscribeReply>();

        await handler.ServerStreamingAsync<SubscribeRequest, SubscribeReply>(new SubscribeRequest { Topic = "t" }, writer, TestCallContext.Create());

        Assert.Equal(new[] { "t-0", "t-1", "t-2" }, writer.Written.Select(x => x.Item));
    }

    [Fact]
    public async Task ServerStreamingAsync_WhenHandlerYieldsPocoItems_ConvertsEachItemToProtobuf()
    {
        var pipeline = BuildPipeline(out var serviceResolverFactory);
        var handler = new GrpcMethodHandler(
            new GrpcMethodDefinition("/x/y", "grpc-test-subscribe-poco-topic"), serviceResolverFactory, pipeline);
        var writer = new FakeServerStreamWriter<SubscribeReply>();

        await handler.ServerStreamingAsync<SubscribeRequest, SubscribeReply>(new SubscribeRequest { Topic = "poco" }, writer, TestCallContext.Create());

        Assert.Equal(new[] { "poco-0", "poco-1", "poco-2" }, writer.Written.Select(x => x.Item));
    }

    [Fact]
    public async Task ServerStreamingAsync_WhenNoHandlerRegistered_ThrowsRpcExceptionWithoutWritingAnyItems()
    {
        var pipeline = BuildPipeline(out var serviceResolverFactory);
        var handler = new GrpcMethodHandler(
            new GrpcMethodDefinition("/benzene.test.TestService/Subscribe", "grpc-test-topic-with-no-handler"), serviceResolverFactory, pipeline);
        var writer = new FakeServerStreamWriter<SubscribeReply>();

        var exception = await Assert.ThrowsAsync<RpcException>(() =>
            handler.ServerStreamingAsync<SubscribeRequest, SubscribeReply>(new SubscribeRequest { Topic = "t" }, writer, TestCallContext.Create()));

        Assert.Equal(StatusCode.NotFound, exception.StatusCode);
        Assert.Empty(writer.Written);
    }

    [Fact]
    public async Task ClientStreamingAsync_SumsUploadedItems()
    {
        var pipeline = BuildPipeline(out var serviceResolverFactory);
        var handler = new GrpcMethodHandler(
            new GrpcMethodDefinition("/benzene.test.TestService/Upload", "grpc-test-upload-stream-topic"), serviceResolverFactory, pipeline);
        var reader = new FakeAsyncStreamReader<UploadItem>(new[]
        {
            new UploadItem { Value = 1 },
            new UploadItem { Value = 2 },
            new UploadItem { Value = 3 },
        });

        var summary = await handler.ClientStreamingAsync<UploadItem, UploadSummary>(reader, TestCallContext.Create());

        Assert.Equal(6, summary.Total);
    }

    [Fact]
    public async Task ClientStreamingAsync_WhenPipelineThrowsOperationCanceledException_ThrowsRpcExceptionCancelled()
    {
        var pipeline = BuildCancellingPipeline(out var serviceResolverFactory);
        var handler = new GrpcMethodHandler(new GrpcMethodDefinition("/x/y", "topic"), serviceResolverFactory, pipeline);
        var reader = new FakeAsyncStreamReader<UploadItem>(new[] { new UploadItem { Value = 1 } });

        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var callContext = TestCallContext.Create(cancellationToken: cts.Token);

        var exception = await Assert.ThrowsAsync<RpcException>(() =>
            handler.ClientStreamingAsync<UploadItem, UploadSummary>(reader, callContext));

        Assert.Equal(StatusCode.Cancelled, exception.StatusCode);
    }

    [Fact]
    public async Task DuplexStreamingAsync_EchoesEachMessage()
    {
        var pipeline = BuildPipeline(out var serviceResolverFactory);
        var handler = new GrpcMethodHandler(
            new GrpcMethodDefinition("/benzene.test.TestService/Chat", "grpc-test-chat-topic"), serviceResolverFactory, pipeline);
        var reader = new FakeAsyncStreamReader<ChatMessage>(new[]
        {
            new ChatMessage { Text = "a" },
            new ChatMessage { Text = "b" },
        });
        var writer = new FakeServerStreamWriter<ChatMessage>();

        await handler.DuplexStreamingAsync<ChatMessage, ChatMessage>(reader, writer, TestCallContext.Create());

        Assert.Equal(new[] { "Echo: a", "Echo: b" }, writer.Written.Select(x => x.Text));
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
