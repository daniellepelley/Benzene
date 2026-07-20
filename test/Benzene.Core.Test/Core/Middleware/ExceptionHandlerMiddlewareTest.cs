using System;
using System.Threading;
using System.Threading.Tasks;
using Benzene.Core.Middleware;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Benzene.Test.Core.Middleware;

public class ExceptionHandlerMiddlewareTest
{
    private class FakeContext { }

    [Fact]
    public async Task HandleAsync_OrdinaryException_IsHandledNotRethrown()
    {
        Exception handled = null;
        var middleware = new ExceptionHandlerMiddleware<FakeContext>((_, ex) => handled = ex, NullLogger.Instance);

        await middleware.HandleAsync(new FakeContext(), () => throw new InvalidOperationException("boom"));

        Assert.IsType<InvalidOperationException>(handled);
    }

    [Fact]
    public async Task HandleAsync_GenuineCancellation_Propagates_SoTheMessageIsNotAckedAsSuccess()
    {
        // A cancellation from a fired token (host shutdown / drain) must NOT be swallowed into a
        // "success" - otherwise a settle/ack/checkpoint transport treats the interrupted work as done
        // and drops the message.
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var onExceptionCalled = false;
        var middleware = new ExceptionHandlerMiddleware<FakeContext>((_, _) => onExceptionCalled = true, NullLogger.Instance);

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            middleware.HandleAsync(new FakeContext(), () => throw new OperationCanceledException(cts.Token)));

        Assert.False(onExceptionCalled, "A genuine cancellation must propagate, not be swallowed into a handled 'success'.");
    }

    [Fact]
    public async Task HandleAsync_OperationCanceledWithoutAFiredToken_IsTreatedAsAnOrdinaryException()
    {
        // An OCE not tied to a cancelled token is ambiguous; it's handled like any other exception
        // rather than propagated (only a genuine, requested cancellation short-circuits).
        Exception handled = null;
        var middleware = new ExceptionHandlerMiddleware<FakeContext>((_, ex) => handled = ex, NullLogger.Instance);

        await middleware.HandleAsync(new FakeContext(), () => throw new OperationCanceledException(CancellationToken.None));

        Assert.IsType<OperationCanceledException>(handled);
    }
}
