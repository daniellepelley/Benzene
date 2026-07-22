using Benzene.HealthChecks.Core;
using Grpc.Core;
using Grpc.Net.Client;

namespace Benzene.Grpc.Client;

/// <summary>
/// Verifies the gRPC channel can establish a connection to its target, with
/// <see cref="GrpcChannel.ConnectAsync"/> — a <b>transport reachability</b> check (it opens the HTTP/2
/// connection without issuing an application RPC, so it is non-destructive). Reported on the
/// <b>dependency</b> category (deep <c>healthcheck</c> layer only — a downstream being unreachable is
/// shared-fate; see <see cref="IDependencyHealthCheck"/>).
/// <para>
/// This is deliberately NOT the downstream's <c>grpc.health.v1</c> <c>Check</c>: that asks whether the
/// <em>downstream itself</em> is serving, which is <b>transitive</b> (the downstream aggregates its own
/// dependencies) — the same hazard the CodeGen contract-drift check has. A grpc.health.v1 probe therefore
/// belongs on the diagnostic <c>contracts</c> topic, never a probe and never this auto-wired default; it
/// also needs the <c>Grpc.HealthCheck</c> package, so it is intentionally out of scope here.
/// </para>
/// </summary>
public class GrpcHealthCheck : IHealthCheck
{
    /// <summary>The default connect timeout.</summary>
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

    private readonly GrpcChannel _channel;
    private readonly TimeSpan _timeout;

    /// <summary>Initializes a new instance of the <see cref="GrpcHealthCheck"/> class.</summary>
    /// <param name="channel">The gRPC channel to probe (the caller owns its lifetime).</param>
    /// <param name="timeout">The connect timeout. Defaults to <see cref="DefaultTimeout"/>.</param>
    public GrpcHealthCheck(GrpcChannel channel, TimeSpan? timeout = null)
    {
        _channel = channel;
        _timeout = timeout ?? DefaultTimeout;
    }

    /// <summary>The check's identifier: <c>"Grpc"</c>.</summary>
    public string Type => "Grpc";

    /// <summary>Runs the check and reports the outcome.</summary>
    public async Task<IHealthCheckResult> ExecuteAsync()
    {
        var dependencies = new[] { new HealthCheckDependency("Grpc", _channel.Target) };
        // ConnectAsync completes when the channel reaches Ready, or the token cancels — so an unreachable
        // target surfaces as a timeout rather than hanging.
        using var cts = new CancellationTokenSource(_timeout);

        try
        {
            await _channel.ConnectAsync(cts.Token);
            return HealthCheckResult.CreateInstance(true, Type,
                new Dictionary<string, object> { { "Target", _channel.Target } }, dependencies);
        }
        catch (Exception ex)
        {
            // Unreachable target -> the timeout fires (OperationCanceledException) -> transient Failed. Any
            // RpcException is classified by its gRPC status (PermissionDenied -> 403 / Unauthenticated -> 401,
            // both a persistent Failed), never its message.
            var (errorCode, statusCode) = GrpcErrorDetails(ex);
            return HealthCheckError.Classify(Type, ex, dependencies, errorCode, statusCode,
                new Dictionary<string, object> { { "Target", _channel.Target } });
        }
    }

    private static (string? ErrorCode, int? StatusCode) GrpcErrorDetails(Exception ex)
    {
        if (ex is RpcException rpc)
        {
            int? statusCode = rpc.StatusCode switch
            {
                StatusCode.PermissionDenied => 403,
                StatusCode.Unauthenticated => 401,
                _ => null,
            };
            return (rpc.StatusCode.ToString(), statusCode);
        }

        return (null, null);
    }
}
