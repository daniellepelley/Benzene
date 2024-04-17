using System.Threading.Tasks;
using Benzene.Abstractions.MessageHandling;
using Benzene.Abstractions.Results;
using Benzene.Core.Results;
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
