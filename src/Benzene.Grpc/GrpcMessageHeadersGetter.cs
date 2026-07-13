using Benzene.Abstractions.Messages.Mappers;

namespace Benzene.Grpc;

public class GrpcMessageHeadersGetter : IMessageHeadersGetter<GrpcContext>
{
    public IDictionary<string, string> GetHeaders(GrpcContext context)
    {
        var headers = new Dictionary<string, string>();

        foreach (var entry in context.CallContext.RequestHeaders)
        {
            if (entry.IsBinary)
            {
                continue;
            }

            headers[entry.Key] = entry.Value;
        }

        return headers;
    }
}
