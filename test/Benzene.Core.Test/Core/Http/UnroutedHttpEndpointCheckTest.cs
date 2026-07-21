using System;
using System.Linq;
using System.Threading.Tasks;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.Core.Exceptions;
using Benzene.Core.MessageHandlers;
using Benzene.Http;
using Benzene.Http.Routing;
using Benzene.Results;
using Benzene.Test.Examples;
using Xunit;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Test.Core.Http;

public class UnroutedHttpEndpointCheckTest
{
    [Fact]
    public void Throws_WhenHandlerHasHttpEndpointButNoMessageAttribute()
    {
        var check = CreateCheck(typeof(UnroutedExampleHandler));

        var exception = Assert.Throws<BenzeneException>(() => check.FindDefinitions());

        Assert.Contains(nameof(UnroutedExampleHandler), exception.Message);
        Assert.Contains("[Message(", exception.Message);
    }

    [Fact]
    public void DoesNotThrow_WhenHandlerHasBothAttributes()
    {
        var check = CreateCheck(typeof(ExampleMessageHandler));

        var definitions = check.FindDefinitions();

        Assert.Empty(definitions);
    }

    [Fact]
    public void DoesNotThrow_WhenHandlerIsRegisteredExplicitly()
    {
        var handlersList = new MessageHandlersList();
        handlersList.Add(MessageHandlerDefinition.CreateInstance(
            "example:unrouted", string.Empty, typeof(ExampleRequestPayload), typeof(Void), typeof(UnroutedExampleHandler)));
        var check = new UnroutedHttpEndpointCheck(
            new[] { new MessageHandlerCandidateTypes(new[] { typeof(UnroutedExampleHandler) }) },
            handlersList);

        var definitions = check.FindDefinitions();

        Assert.Empty(definitions);
    }

    [Fact]
    public void IgnoresAbstractHandlers()
    {
        var check = CreateCheck(typeof(AbstractUnroutedExampleHandler));

        var definitions = check.FindDefinitions();

        Assert.Empty(definitions);
    }

    [Fact]
    public void IgnoresNonHandlerTypes()
    {
        var check = CreateCheck(typeof(NotAHandler));

        var definitions = check.FindDefinitions();

        Assert.Empty(definitions);
    }

    [Fact]
    public void NamesEveryUnroutedHandler()
    {
        var check = CreateCheck(typeof(UnroutedExampleHandler), typeof(SecondUnroutedExampleHandler));

        var exception = Assert.Throws<BenzeneException>(() => check.FindDefinitions());

        Assert.Contains(nameof(UnroutedExampleHandler), exception.Message);
        Assert.Contains(nameof(SecondUnroutedExampleHandler), exception.Message);
    }

    private static UnroutedHttpEndpointCheck CreateCheck(params Type[] candidateTypes)
    {
        return new UnroutedHttpEndpointCheck(
            new[] { new MessageHandlerCandidateTypes(candidateTypes) },
            new ReflectionMessageHandlersFinder(candidateTypes));
    }

    [HttpEndpoint("POST", "/unrouted")]
    private class UnroutedExampleHandler : IMessageHandler<ExampleRequestPayload, Void>
    {
        public Task<IBenzeneResult<Void>> HandleAsync(ExampleRequestPayload request)
            => Task.FromResult(BenzeneResult.Ok(new Void()));
    }

    [HttpEndpoint("POST", "/unrouted-second")]
    private class SecondUnroutedExampleHandler : IMessageHandler<ExampleRequestPayload, Void>
    {
        public Task<IBenzeneResult<Void>> HandleAsync(ExampleRequestPayload request)
            => Task.FromResult(BenzeneResult.Ok(new Void()));
    }

    [HttpEndpoint("POST", "/unrouted-abstract")]
    private abstract class AbstractUnroutedExampleHandler : IMessageHandler<ExampleRequestPayload, Void>
    {
        public abstract Task<IBenzeneResult<Void>> HandleAsync(ExampleRequestPayload request);
    }

    [HttpEndpoint("POST", "/not-a-handler")]
    private class NotAHandler
    {
    }
}
