using System.Threading.Tasks;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.Core.MessageHandlers;
using Benzene.Results;

namespace Benzene.Test.Examples;

[Message(Defaults.Topic, "2.0")]
public class ExampleMessageHandlerV2 : IMessageHandler<ExampleRequestPayload, ExampleResponsePayload>
{
    public Task<IBenzeneResult<ExampleResponsePayload>> HandleAsync(ExampleRequestPayload request)
    {
        return Task.FromResult(BenzeneResult.Deleted(Mother.CreateResponse(request.Name)));
    }
}
