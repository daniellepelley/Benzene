using Benzene.Abstractions.Mappers;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Core.MessageHandlers;

namespace Benzene.Aws.Sns;

public class SnsMessageTopicMapper : IMessageTopicMapper<SnsRecordContext>
{
    public ITopic GetTopic(SnsRecordContext context)
    {
        return new Topic(SnsUtils.GetFromAttributes(context, "topic"));
    }
}
