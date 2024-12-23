using System.Threading.Tasks;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandling;
using Benzene.Results;

namespace Benzene.Test.Examples;

[Message(Defaults.TopicWithGuid)]
public class ExampleMessageWithGuidHandler : IMessageHandler<ExampleRequestPayloadWithGuid, Void>
{
    public Task<IBenzeneResult<Void>> HandleAsync(ExampleRequestPayloadWithGuid request)
    {
        return Task.FromResult(BenzeneResult.Ok(new Void()));
    }
}


