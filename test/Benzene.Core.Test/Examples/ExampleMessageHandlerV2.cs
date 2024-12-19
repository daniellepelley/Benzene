using System.Threading.Tasks;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandling;
using Benzene.Results;

namespace Benzene.Test.Examples;

[Message(Defaults.Topic, "2.0")]
public class ExampleMessageHandlerV2 : IMessageHandler<ExampleRequestPayload, ExampleResponsePayload>
{
    public Task<IServiceResult<ExampleResponsePayload>> HandleAsync(ExampleRequestPayload request)
    {
        return Task.FromResult(ServiceResult.Deleted(Mother.CreateResponse(request.Name)));
    }
}
