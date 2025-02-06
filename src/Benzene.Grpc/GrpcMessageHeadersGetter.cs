using Benzene.Abstractions.Messages.Mappers;

namespace Benzene.Grpc;

public class GrpcMessageHeadersGetter : IMessageHeadersGetter<GrpcContext>
{
    public IDictionary<string, string> GetHeaders(GrpcContext context)
    {
        return new Dictionary<string, string>();
    }
}