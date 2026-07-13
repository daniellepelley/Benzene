using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Core.Middleware;
using Benzene.Grpc.Test.Handlers;
using Benzene.Grpc.Test.Helpers;
using Benzene.Grpc.Test.Protos;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Benzene.Grpc.Test;

public class GrpcMethodPipelineTest
{
    [Fact]
    public async Task HandleAsync_RoutesDifferentGrpcServicesThroughTheSharedPipeline()
    {
        var pipeline = BuildPipeline(out var serviceResolverFactory);

        // Two handlers behind two unrelated gRPC service methods, sharing one pipeline and no ServiceDescriptor.
        var echoHandler = new GrpcMethodHandler(
            new GrpcMethodDefinition("/benzene.test.TestService/Echo", "grpc-test-echo-topic"), serviceResolverFactory, pipeline);
        var uploadHandler = new GrpcMethodHandler(
            new GrpcMethodDefinition("/other.package.OtherService/Summarize", "grpc-test-upload-topic"), serviceResolverFactory, pipeline);

        var echoReply = await echoHandler.HandleAsync<EchoRequest, EchoReply>(new EchoRequest { Name = "world" }, TestCallContext.Create());
        var uploadSummary = await uploadHandler.HandleAsync<UploadItem, UploadSummary>(new UploadItem { Value = 21 }, TestCallContext.Create());

        Assert.Equal("Hello world", echoReply.Message);
        Assert.Equal(42, uploadSummary.Total);
    }

    [Fact]
    public async Task HandleAsync_WhenHandlerReturnsPoco_ConvertsToProtobufResponse()
    {
        var pipeline = BuildPipeline(out var serviceResolverFactory);

        var handler = new GrpcMethodHandler(
            new GrpcMethodDefinition("/benzene.test.TestService/Echo", "grpc-test-echo-poco-topic"), serviceResolverFactory, pipeline);

        var reply = await handler.HandleAsync<EchoRequest, EchoReply>(new EchoRequest { Name = "poco" }, TestCallContext.Create());

        Assert.Equal("Hello poco", reply.Message);
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
}
