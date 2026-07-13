using Grpc.Core;

namespace Benzene.Grpc.Client;

/// <summary>
/// Defines a contract for mapping a gRPC call outcome back to a Benzene result status.
/// </summary>
public interface IGrpcStatusReverseMapper
{
    /// <summary>
    /// Maps a gRPC <see cref="StatusCode"/> to a Benzene result status. When <paramref name="trailers"/>
    /// carries a <c>benzene-status</c> entry (set by a Benzene-hosted server, see
    /// <c>Benzene.Grpc.GrpcMethodHandler</c>), that raw value wins verbatim - it preserves distinctions
    /// (e.g. <c>Created</c> vs <c>Accepted</c>) that collapse to the same <see cref="StatusCode.OK"/> on
    /// the wire.
    /// </summary>
    string Map(StatusCode statusCode, Metadata? trailers);
}
