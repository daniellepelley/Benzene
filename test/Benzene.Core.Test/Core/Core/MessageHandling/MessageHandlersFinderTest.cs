using System.Linq;
using Benzene.Core.MessageHandling;
using Benzene.Core.Results;
using Benzene.Results;
using Benzene.Test.Examples;
using Xunit;

namespace Benzene.Test.Core.Core.MessageHandling;

public class MessageHandlersFinderTest
{
    [Fact]
    public void FindHandlers()
    {
        var httpEndpointFinder = new ReflectionMessageHandlersFinder(typeof(ExampleRequestPayload).Assembly);

        var handlers = httpEndpointFinder.FindDefinitions();

        var handlerDefinition = handlers.First(x => x.Topic == Defaults.Topic);

        Assert.Equal(typeof(ExampleMessageHandler), handlerDefinition.HandlerType);
        Assert.Equal(typeof(ExampleRequestPayload), handlerDefinition.RequestType);
        Assert.Equal(typeof(Void), handlerDefinition.ResponseType);

        var handlerDefinition2 = handlers.First(x => x.Topic == Defaults.TopicNoResponse);

        Assert.Equal(typeof(ExampleNoResponseMessageHandler), handlerDefinition2.HandlerType);
        Assert.Equal(typeof(ExampleRequestPayload), handlerDefinition2.RequestType);
        Assert.Equal(typeof(Void), handlerDefinition2.ResponseType);
    }
    
    [Fact]
    public void FindHandlers_Deduplicates()
    {
        var httpEndpointFinder = new ReflectionMessageHandlersFinder(typeof(ExampleMessageHandler), typeof(ExampleMessageHandler));

        var handlerDefinitions = httpEndpointFinder.FindDefinitions();
        Assert.Single(handlerDefinitions);

        var handlerDefinition = handlerDefinitions.First(x => x.Topic == Defaults.Topic);

        Assert.Equal(typeof(ExampleMessageHandler), handlerDefinition.HandlerType);
        Assert.Equal(typeof(ExampleRequestPayload), handlerDefinition.RequestType);
        Assert.Equal(typeof(Void), handlerDefinition.ResponseType);
    }
}
