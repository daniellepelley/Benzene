using System.Text;
using Benzene.Abstractions.Messages.Mappers;

namespace Benzene.Kafka.Core.Kafka;

public class KafkaSendMessageHeadersGetter : IMessageHeadersGetter<KafkaSendMessageContext>
{
    public IDictionary<string, string> GetHeaders(KafkaSendMessageContext context)
    {
        return context.Message.Headers
            .ToDictionary(x => x.Key, x => Encoding.UTF8.GetString(x.GetValueBytes() ?? System.Array.Empty<byte>()));
    }
}