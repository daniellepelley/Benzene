using System.Linq;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.Core.MessageHandlers;
using Benzene.Results;

namespace Benzene.Test.Examples;

public static class Mother
{
    public static ExampleRequestPayload[] CreateRequests(int number)
    {
        return Enumerable.Range(0, number)
            .Select(x =>
                CreateRequest(x, $"name-{x}"))
            .ToArray();
    }

    public static ExampleRequestPayload CreateRequest(int id = 0, string name = "name-0")
    {
        return new ExampleRequestPayload
        {
            Id = id,
            Name = name
        };
    }


    public static ExampleResponsePayload CreateResponse(string name)
    {
        return new ExampleResponsePayload
        {
            Name = name,
            Values = new[] { "1", "2", "3" }
        };
    }

    public static IMessageHandlerDefinition CreateMessageHandlerDefinitionV1()
    {
        return MessageHandlerDefinition.CreateInstance(Defaults.Topic,
            typeof(ExampleRequestPayload),
            typeof(Void),
            typeof(ExampleMessageHandler));
    }

    public static IMessageHandlerDefinition CreateMessageHandlerDefinitionV2()
    {
        return MessageHandlerDefinition.CreateInstance(Defaults.Topic,
            Defaults.Version2,
            typeof(ExampleRequestPayload),
            typeof(ExampleResponsePayload),
            typeof(ExampleMessageHandler));
    }
}
