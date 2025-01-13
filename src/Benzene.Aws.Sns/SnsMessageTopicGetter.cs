using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.Messages;
using Benzene.Core.MessageHandlers;
using Benzene.Core.Messages;

namespace Benzene.Aws.Sns;

public class SnsMessageTopicGetter : IMessageTopicGetter<SnsRecordContext>
{
    public ITopic GetTopic(SnsRecordContext context)
    {
        return new Topic(SnsUtils.GetFromAttributes(context, "topic"));
    }
}
