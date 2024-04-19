using System.Threading.Tasks;
using Benzene.Abstractions.MessageHandling;
using Benzene.Results;

namespace Benzene.Core.MessageHandling;


public class NotFoundMessageHandler : IMessageHandler
{
    private readonly string _topic;

    public NotFoundMessageHandler(string topic)
    {
        _topic = topic;
    }

    public Task<IServiceResult> HandlerAsync(IRequestFactory requestFactory)
    {
        var serviceResult = ServiceResult.NotFound($"No handler for message topic: {_topic}");
        return Task.FromResult(serviceResult);
    }
}
