using Benzene.Core.BenzeneMessage;
using Newtonsoft.Json;

namespace Benzene.Examples.Aws.Tests.Helpers;

public static class BenzeneMessageBuilder
{
    public static BenzeneMessageRequest Create(string topic, object message)
    {
        return new BenzeneMessageRequest
        {
            Topic = topic,
            Body = JsonConvert.SerializeObject(message)
        };
    }
}