namespace Benzene.Grpc.Serialization;

public interface IGrpcMessageAdapter
{
    /// <summary>
    /// Converts an incoming protobuf request message into the handler's request type.
    /// Returns the same instance untouched when the handler already declares the protobuf type.
    /// </summary>
    TRequest? ConvertRequest<TRequest>(object? protobufMessage) where TRequest : class;

    /// <summary>
    /// Converts a handler's response payload into the outgoing protobuf response type.
    /// Returns the same instance untouched when the payload already is the protobuf type.
    /// </summary>
    TResponse ConvertResponse<TResponse>(object? payload) where TResponse : class;
}
