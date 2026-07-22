using Benzene.HealthChecks.Core;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace Benzene.RabbitMq;

/// <summary>
/// Verifies a RabbitMQ consumer can reach its broker and that the consumed queue exists, with a
/// <b>passive</b> queue declare — the RabbitMQ analogue of the other reachability checks, non-destructive:
/// a passive declare neither creates nor mutates the queue (it returns the queue's message/consumer
/// counts, or a channel-level <c>404</c> if the queue is gone). Reported on the <b>dependency</b> category
/// (deep <c>healthcheck</c> layer only — a broker being unreachable is shared-fate; see
/// <see cref="IDependencyHealthCheck"/>). A permission failure (AMQP <c>403 access-refused</c>) is a
/// <see cref="HealthCheckStatus.Warning"/> (§3.9); the exception message is never included.
/// </summary>
public class RabbitMqHealthCheck : IHealthCheck
{
    /// <summary>The default timeout for the connect + passive-declare round-trip.</summary>
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

    private readonly IRabbitMqConnectionProvider _connectionProvider;
    private readonly string _queueName;
    private readonly TimeSpan _timeout;

    /// <summary>Initializes a new instance of the <see cref="RabbitMqHealthCheck"/> class.</summary>
    /// <param name="connectionProvider">Supplies the reused connection the check opens a channel on.</param>
    /// <param name="queueName">The queue whose existence to verify (reported as the dependency).</param>
    /// <param name="timeout">The connect + declare timeout. Defaults to <see cref="DefaultTimeout"/>.</param>
    public RabbitMqHealthCheck(IRabbitMqConnectionProvider connectionProvider, string queueName, TimeSpan? timeout = null)
    {
        _connectionProvider = connectionProvider;
        _queueName = queueName;
        _timeout = timeout ?? DefaultTimeout;
    }

    /// <summary>The check's identifier: <c>"RabbitMq"</c>.</summary>
    public string Type => "RabbitMq";

    /// <summary>Runs the check and reports the outcome.</summary>
    public async Task<IHealthCheckResult> ExecuteAsync()
    {
        var dependencies = new[] { new HealthCheckDependency("Queue", _queueName) };
        // Bound the broker round-trip so a half-open connection can't hang past the check's budget.
        using var cts = new CancellationTokenSource(_timeout);
        IChannel? channel = null;

        try
        {
            var connection = await _connectionProvider.GetConnectionAsync(cts.Token);
            channel = await connection.CreateChannelAsync(cancellationToken: cts.Token);
            // Passive declare: read-only existence + reachability, no create/mutate.
            await channel.QueueDeclarePassiveAsync(_queueName, cts.Token);
            return HealthCheckResult.CreateInstance(true, Type,
                new Dictionary<string, object> { { "Queue", _queueName } }, dependencies);
        }
        catch (Exception ex)
        {
            // Expected failures (broker unreachable, queue missing, no permission) are a classified result,
            // not a throw. HealthCheckError applies the shared policy: 401/403 -> Warning, else Failed,
            // enriched with the AMQP reply code, never the exception message.
            var (errorCode, statusCode) = RabbitMqErrorDetails(ex);
            return HealthCheckError.Classify(Type, ex, dependencies, errorCode, statusCode,
                new Dictionary<string, object> { { "Queue", _queueName } });
        }
        finally
        {
            if (channel is not null)
            {
                // A channel-level exception (e.g. 404) has already closed the channel; dispose best-effort.
                try { await channel.DisposeAsync(); } catch { /* already closed */ }
            }
        }
    }

    // RabbitMQ is not HTTP, but AMQP reply codes are 3-digit and align with the shared policy: 403
    // (access-refused) -> Warning, 404 (not-found) -> Failed. Report the reply code as both the error
    // code and the status; null for a non-AMQP exception (e.g. a raw socket failure or a timeout).
    private static (string? ErrorCode, int? StatusCode) RabbitMqErrorDetails(Exception ex)
    {
        if (ex is OperationInterruptedException oie && oie.ShutdownReason is not null)
        {
            int replyCode = oie.ShutdownReason.ReplyCode;
            return (replyCode.ToString(), replyCode);
        }

        return (null, null);
    }
}
