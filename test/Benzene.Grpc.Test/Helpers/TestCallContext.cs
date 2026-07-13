using Grpc.Core;

namespace Benzene.Grpc.Test.Helpers;

/// <summary>
/// A minimal, hand-rolled <see cref="ServerCallContext"/> for unit tests that don't need a real
/// in-process host (see <see cref="Benzene.Grpc.Test.GrpcHostingTest"/> for those). Only the members
/// Benzene.Grpc actually reads (<see cref="Method"/>, <see cref="Deadline"/>, <see cref="RequestHeaders"/>,
/// <see cref="CancellationToken"/>, <see cref="ResponseTrailers"/>, <see cref="WriteResponseHeadersAsync"/>)
/// are meaningfully implemented; anything else throws if touched.
/// </summary>
public class TestCallContext : ServerCallContext
{
    private readonly string _method;
    private readonly Metadata _requestHeaders;
    private readonly CancellationToken _cancellationToken;
    private readonly DateTime _deadline;
    private readonly Metadata _responseTrailers = new();

    public static TestCallContext Create(
        string method = "/benzene.test.TestService/Echo",
        Metadata? requestHeaders = null,
        CancellationToken cancellationToken = default,
        DateTime? deadline = null)
    {
        return new TestCallContext(method, requestHeaders ?? new Metadata(), cancellationToken, deadline ?? DateTime.MaxValue);
    }

    private TestCallContext(string method, Metadata requestHeaders, CancellationToken cancellationToken, DateTime deadline)
    {
        _method = method;
        _requestHeaders = requestHeaders;
        _cancellationToken = cancellationToken;
        _deadline = deadline;
    }

    public Metadata WrittenResponseHeaders { get; private set; } = new();

    protected override string MethodCore => _method;

    protected override string HostCore => "test-host";

    protected override string PeerCore => "test-peer";

    protected override DateTime DeadlineCore => _deadline;

    protected override Metadata RequestHeadersCore => _requestHeaders;

    protected override CancellationToken CancellationTokenCore => _cancellationToken;

    protected override Metadata ResponseTrailersCore => _responseTrailers;

    protected override Status StatusCore { get; set; }

    protected override WriteOptions? WriteOptionsCore { get; set; }

    protected override AuthContext AuthContextCore => throw new NotImplementedException();

    protected override IDictionary<object, object> UserStateCore => throw new NotImplementedException();

    protected override ContextPropagationToken? CreatePropagationTokenCore(ContextPropagationOptions? options)
    {
        throw new NotImplementedException();
    }

    protected override Task WriteResponseHeadersAsyncCore(Metadata responseHeaders)
    {
        WrittenResponseHeaders = responseHeaders;
        return Task.CompletedTask;
    }
}
