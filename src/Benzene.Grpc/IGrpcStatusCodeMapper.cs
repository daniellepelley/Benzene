using Grpc.Core;

namespace Benzene.Grpc;

/// <summary>
/// Defines a contract for mapping Benzene result status codes to gRPC status codes.
/// </summary>
public interface IGrpcStatusCodeMapper
{
    /// <summary>
    /// Maps a Benzene result status to a gRPC status code.
    /// </summary>
    /// <param name="benzeneResultStatus">The Benzene result status string to map.</param>
    /// <returns>The corresponding <see cref="StatusCode"/>.</returns>
    StatusCode Map(string? benzeneResultStatus);
}
