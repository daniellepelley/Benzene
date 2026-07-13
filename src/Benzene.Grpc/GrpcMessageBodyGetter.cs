using Benzene.Abstractions.Messages.Mappers;
using Google.Protobuf;

namespace Benzene.Grpc;

public class GrpcMessageBodyGetter : IMessageBodyGetter<GrpcContext>
{
    public string? GetBody(GrpcContext context)
    {
        return context.RequestAsObject is IMessage message
            ? JsonFormatter.Default.Format(message)
            : System.Text.Json.JsonSerializer.Serialize(context.RequestAsObject);
    }
}