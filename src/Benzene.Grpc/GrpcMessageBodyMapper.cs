using Benzene.Abstractions.Mappers;

namespace Benzene.Grpc;

public class GrpcMessageBodyMapper : IMessageBodyMapper<GrpcContext>
{
    public string? GetBody(GrpcContext context)
    {
        return System.Text.Json.JsonSerializer.Serialize(context.RequestAsObject);
    }
}