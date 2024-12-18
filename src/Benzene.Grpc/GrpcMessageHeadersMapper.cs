using Benzene.Abstractions.Mappers;

namespace Benzene.Grpc;

public class GrpcMessageHeadersMapper : IMessageHeadersMapper<GrpcContext>
{
    public IDictionary<string, string> GetHeaders(GrpcContext context)
    {
        return new Dictionary<string, string>();
    }
}