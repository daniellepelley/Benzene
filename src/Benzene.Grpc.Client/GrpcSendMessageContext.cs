using Grpc.Core;

namespace Benzene.Grpc.Client;

public class GrpcSendMessageContext
{
    public GrpcSendMessageContext(string topic, object message, Metadata headers, DateTime? deadline, CancellationToken cancellationToken)
    {
        Topic = topic;
        Message = message;
        Headers = headers;
        Deadline = deadline;
        CancellationToken = cancellationToken;
    }

    public string Topic { get; }
    public object Message { get; }
    public Metadata Headers { get; }
    public DateTime? Deadline { get; }
    public CancellationToken CancellationToken { get; }
    public object? Response { get; set; }
    public Status Status { get; set; }
    public Metadata? ResponseTrailers { get; set; }
}
