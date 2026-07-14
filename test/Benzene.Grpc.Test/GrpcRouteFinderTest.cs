using Benzene.Abstractions.MessageHandlers;
using Benzene.Core.Exceptions;
using Benzene.Core.Messages;
using Benzene.Grpc;
using Moq;
using Xunit;

namespace Benzene.Grpc.Test;

public class GrpcRouteFinderTest
{
    [Fact]
    public void Find_WhenExactMatch_ReturnsDefinition()
    {
        var definitionA = new GrpcMethodDefinition("/pkg.Service/MethodA", "topic-a");
        var definitionB = new GrpcMethodDefinition("/pkg.Service/MethodB", "topic-b");
        var routeFinder = CreateRouteFinder(definitionA, definitionB);

        var result = routeFinder.Find("/pkg.Service/MethodA");

        Assert.Same(definitionA, result);
    }

    [Fact]
    public void Find_WhenCaseDiffers_ReturnsDefinition()
    {
        var definitionA = new GrpcMethodDefinition("/pkg.Service/MethodA", "topic-a");
        var routeFinder = CreateRouteFinder(definitionA);

        var result = routeFinder.Find("/PKG.SERVICE/METHODA");

        Assert.Same(definitionA, result);
    }

    [Fact]
    public void Find_WhenNoMatch_ReturnsNull()
    {
        var definitionA = new GrpcMethodDefinition("/pkg.Service/MethodA", "topic-a");
        var routeFinder = CreateRouteFinder(definitionA);

        var result = routeFinder.Find("/pkg.Service/Missing");

        Assert.Null(result);
    }

    [Fact]
    public void ReflectionGrpcMethodFinder_WhenTwoHandlersShareAGrpcMethod_ThrowsBenzeneException()
    {
        var definitionA = new Mock<IMessageHandlerDefinition>();
        definitionA.Setup(x => x.HandlerType).Returns(typeof(HandlerA));
        definitionA.Setup(x => x.Topic).Returns(new Topic("topic-a"));

        var definitionB = new Mock<IMessageHandlerDefinition>();
        definitionB.Setup(x => x.HandlerType).Returns(typeof(HandlerB));
        definitionB.Setup(x => x.Topic).Returns(new Topic("topic-b"));

        var messageHandlersFinder = new Mock<IMessageHandlersFinder>();
        messageHandlersFinder.Setup(x => x.FindDefinitions())
            .Returns(new[] { definitionA.Object, definitionB.Object });

        var methodFinder = new ReflectionGrpcMethodFinder(messageHandlersFinder.Object);

        Assert.Throws<BenzeneException>(() => methodFinder.FindDefinitions());
    }

    private static GrpcRouteFinder CreateRouteFinder(params IGrpcMethodDefinition[] definitions)
    {
        var methodFinder = new Mock<IGrpcMethodFinder>();
        methodFinder.Setup(x => x.FindDefinitions()).Returns(definitions);

        return new GrpcRouteFinder(methodFinder.Object);
    }

    [GrpcMethod("/pkg.Service/SharedMethod")]
    private class HandlerA
    {
    }

    [GrpcMethod("/pkg.Service/SharedMethod")]
    private class HandlerB
    {
    }
}
