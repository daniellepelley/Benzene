using Grpc.Core;

namespace Benzene.Grpc;

/// <summary>
/// Gives message handlers access to the current gRPC call's <see cref="ServerCallContext"/>, notably its
/// <see cref="CancellationToken"/> and deadline, without coupling the handler to Benzene's transport types.
/// Analogous to ASP.NET Core's <c>IHttpContextAccessor</c>.
/// </summary>
public interface IGrpcServerCallAccessor
{
    /// <summary>The current call's context, or <c>null</c> outside of a gRPC call.</summary>
    ServerCallContext? CallContext { get; }

    /// <summary>The current call's cancellation token, or <see cref="System.Threading.CancellationToken.None"/> outside of a gRPC call.</summary>
    CancellationToken CancellationToken { get; }
}
