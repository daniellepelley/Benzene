namespace Benzene.Grpc.Serialization;

public interface IGrpcMessageAdapter
{
    /// <summary>
    /// Converts an incoming protobuf request message into the handler's request type.
    /// Returns the same instance untouched when the handler already declares the protobuf type.
    /// </summary>
    /// <remarks>
    /// Unconstrained (not <c>where TRequest : class</c>): <see cref="Benzene.Grpc.Client.GrpcBenzeneMessageClient"/>
    /// calls this with the caller's <c>TResponse</c> from <c>IBenzeneMessageClient.SendMessageAsync</c>,
    /// which is itself unconstrained to stay consistent with every other transport's client.
    /// </remarks>
    TRequest? ConvertRequest<TRequest>(object? protobufMessage);

    /// <summary>
    /// Converts a handler's response payload into the outgoing protobuf response type.
    /// Returns the same instance untouched when the payload already is the protobuf type.
    /// </summary>
    TResponse ConvertResponse<TResponse>(object? payload);
}
