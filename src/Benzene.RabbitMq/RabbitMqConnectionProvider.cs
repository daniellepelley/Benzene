using RabbitMQ.Client;

namespace Benzene.RabbitMq;

/// <summary>
/// Supplies the <see cref="IConnection"/> the <see cref="RabbitMqHealthCheck"/> uses. The health check
/// opens ONE connection through this provider and reuses it across probes — opening a connection per
/// probe would re-run the TCP + AMQP handshake at scrape cadence. The worker owns its own consume
/// connection privately, so the health check uses a dedicated (still single, still reused) connection
/// rather than sharing the worker's; the check opens only a cheap short-lived <em>channel</em> per probe
/// on this connection.
/// </summary>
public interface IRabbitMqConnectionProvider
{
    /// <summary>Returns the reused connection, opening (or re-opening a dropped) one as needed.</summary>
    /// <param name="cancellationToken">A token to abort the connect.</param>
    Task<IConnection> GetConnectionAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Default <see cref="IRabbitMqConnectionProvider"/>: lazily opens one connection via an
/// <see cref="IRabbitMqConnectionFactory"/> and reuses it. Unlike a cached <c>Lazy&lt;Task&gt;</c>, a
/// <b>failed</b> connect is not memoised — the next probe retries — and a connection that has since
/// dropped (and not auto-recovered) is disposed and re-opened.
/// </summary>
public class RabbitMqConnectionProvider : IRabbitMqConnectionProvider, IAsyncDisposable
{
    private readonly IRabbitMqConnectionFactory _connectionFactory;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IConnection? _connection;

    /// <summary>Initializes a new instance over <paramref name="connectionFactory"/>.</summary>
    /// <param name="connectionFactory">The factory used to open the connection.</param>
    public RabbitMqConnectionProvider(IRabbitMqConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    /// <inheritdoc />
    public async Task<IConnection> GetConnectionAsync(CancellationToken cancellationToken)
    {
        // Fast path: an already-open connection, no lock.
        var existing = _connection;
        if (existing is { IsOpen: true })
        {
            return existing;
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_connection is { IsOpen: true })
            {
                return _connection;
            }

            // Dispose a dropped connection that didn't auto-recover before re-opening.
            if (_connection is not null)
            {
                try { await _connection.DisposeAsync(); } catch { /* best effort */ }
                _connection = null;
            }

            // A throw here leaves _connection null, so the next probe retries rather than caching a fault.
            _connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
            return _connection;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            try { await _connection.DisposeAsync(); } catch { /* best effort */ }
            _connection = null;
        }
        _gate.Dispose();
        GC.SuppressFinalize(this);
    }
}
