using Grpc.Net.Client;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;

namespace Benzene.Grpc.TestHelpers;

/// <summary>
/// An in-memory gRPC host built by <see cref="GrpcTestHostBuilderExtensions.BuildGrpcHost{TStartUp}"/>,
/// wrapping an ASP.NET Core <see cref="TestServer"/>. Use <see cref="CreateChannel"/> to get a
/// <see cref="GrpcChannel"/> for a generated client stub.
/// </summary>
public class GrpcTestHost : IDisposable
{
    private readonly IHost _host;

    internal GrpcTestHost(IHost host)
    {
        _host = host;
    }

    /// <summary>Creates a <see cref="GrpcChannel"/> wired directly to this host's in-memory <see cref="TestServer"/>.</summary>
    public GrpcChannel CreateChannel()
    {
        var testServer = _host.GetTestServer();
        return GrpcChannel.ForAddress(testServer.BaseAddress ?? new Uri("http://localhost"), new GrpcChannelOptions
        {
            HttpHandler = testServer.CreateHandler()
        });
    }

    public void Dispose()
    {
        _host.Dispose();
    }
}
