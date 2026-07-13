using Grpc.Core;

namespace Benzene.Grpc.Test.Helpers;

/// <summary>
/// A hand-rolled <see cref="CallInvoker"/> for unit tests (Grpc.Core.Testing is deliberately not a
/// dependency). Only <see cref="AsyncUnaryCall{TRequest,TResponse}"/> is exercised by
/// <c>Benzene.Grpc.Client</c> today; the other call shapes throw if reached.
/// </summary>
public class TestCallInvoker : CallInvoker
{
    public IMethod? CapturedMethod { get; private set; }
    public string? CapturedHost { get; private set; }
    public CallOptions CapturedOptions { get; private set; }
    public object? CapturedRequest { get; private set; }

    public object? Response { get; set; }
    public Status Status { get; set; } = new(StatusCode.OK, string.Empty);
    public Metadata Trailers { get; set; } = new();
    public RpcException? RpcExceptionToThrow { get; set; }

    public override TResponse BlockingUnaryCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string? host, CallOptions options, TRequest request)
        => throw new NotSupportedException();

    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string? host, CallOptions options, TRequest request)
    {
        CapturedMethod = method;
        CapturedHost = host;
        CapturedOptions = options;
        CapturedRequest = request;

        var responseTask = RpcExceptionToThrow != null
            ? Task.FromException<TResponse>(RpcExceptionToThrow)
            : Task.FromResult((TResponse)Response!);

        return new AsyncUnaryCall<TResponse>(
            responseTask,
            Task.FromResult(new Metadata()),
            () => Status,
            () => Trailers,
            () => { });
    }

    public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string? host, CallOptions options)
        => throw new NotSupportedException();

    public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string? host, CallOptions options, TRequest request)
        => throw new NotSupportedException();

    public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string? host, CallOptions options)
        => throw new NotSupportedException();
}
