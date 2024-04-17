using System.Threading.Tasks;
using Benzene.Abstractions.MessageHandling;
using Benzene.Abstractions.Results;
using Benzene.Core.Results;
using Benzene.Results;

namespace Benzene.Test.Examples;

[Message(Defaults.TopicWithGuid)]
public class ExampleMessageWithGuidHandler : IMessageHandler<ExampleRequestPayloadWithGuid, Void>
{
    public Task<IServiceResult<Void>> HandleAsync(ExampleRequestPayloadWithGuid request)
    {
        return Task.FromResult(ServiceResult.Ok(new Void()));
    }
}


