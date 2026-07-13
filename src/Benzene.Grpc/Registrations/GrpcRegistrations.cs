using Benzene.Core.DI;

namespace Benzene.Grpc.Registrations;

/// <summary>
/// Provides automatic registration of gRPC services for Benzene applications.
/// </summary>
/// <remarks>
/// This class extends <see cref="RegistrationsBase"/> to automatically register gRPC-related
/// services when the application discovers and loads Benzene packages. It ensures that gRPC
/// message handler routing, request mapping, and serialization services are available in the
/// dependency injection container.
/// </remarks>
public class GrpcRegistrations : RegistrationsBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GrpcRegistrations"/> class and registers gRPC services.
    /// </summary>
    public GrpcRegistrations()
    {
        Add(".AddGrpcMessageHandlers()", x => x.AddGrpcMessageHandlers());
    }
}
