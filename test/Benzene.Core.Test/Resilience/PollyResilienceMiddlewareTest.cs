using System;
using System.Threading.Tasks;
using Benzene.Resilience.Polly;
using Polly;
using Polly.Retry;
using Xunit;

namespace Benzene.Test.Resilience;

public class PollyResilienceMiddlewareTest
{
    private sealed class TestContext
    {
        public bool Failed { get; set; }
    }

    private static ResiliencePipeline RetryPipeline(int retries, PredicateBuilder<object> shouldHandle)
    {
        return new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = retries,
                Delay = TimeSpan.Zero,
                BackoffType = DelayBackoffType.Constant,
                ShouldHandle = shouldHandle,
            })
            .Build();
    }

    [Fact]
    public async Task HandleAsync_Success_RunsNextOnce()
    {
        var attempts = 0;
        var middleware = new PollyResilienceMiddleware<TestContext>(
            RetryPipeline(3, new PredicateBuilder().Handle<Exception>()));

        await middleware.HandleAsync(new TestContext(), () =>
        {
            attempts++;
            return Task.CompletedTask;
        });

        Assert.Equal(1, attempts);
    }

    [Fact]
    public async Task HandleAsync_ThrowsThenSucceeds_RetriesUntilSuccess()
    {
        var attempts = 0;
        var middleware = new PollyResilienceMiddleware<TestContext>(
            RetryPipeline(3, new PredicateBuilder().Handle<InvalidOperationException>()));

        await middleware.HandleAsync(new TestContext(), () =>
        {
            attempts++;
            if (attempts < 3)
            {
                throw new InvalidOperationException("transient");
            }

            return Task.CompletedTask;
        });

        Assert.Equal(3, attempts);
    }

    [Fact]
    public async Task HandleAsync_AlwaysThrows_ExhaustsRetriesThenPropagatesRealException()
    {
        var attempts = 0;
        var middleware = new PollyResilienceMiddleware<TestContext>(
            RetryPipeline(2, new PredicateBuilder().Handle<InvalidOperationException>()));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            middleware.HandleAsync(new TestContext(), () =>
            {
                attempts++;
                throw new InvalidOperationException("always");
            }));

        Assert.Equal(3, attempts); // initial + 2 retries
    }

    [Fact]
    public async Task HandleAsync_FailureResult_WithIsFailure_RetriesOnTheResult()
    {
        var attempts = 0;
        // Pipeline retries on the sentinel the middleware throws for a failure result.
        var middleware = new PollyResilienceMiddleware<TestContext>(
            RetryPipeline(3, new PredicateBuilder().Handle<BenzeneFailureResultException>()),
            isFailure: ctx => ctx.Failed);

        var context = new TestContext();
        await middleware.HandleAsync(context, () =>
        {
            attempts++;
            context.Failed = attempts < 3; // fails (as a result, not a throw) the first two attempts
            return Task.CompletedTask;
        });

        Assert.Equal(3, attempts);
        Assert.False(context.Failed);
    }

    [Fact]
    public async Task HandleAsync_FailureResult_RetriesExhausted_SwallowsSentinel_LeavesResultOnContext()
    {
        var attempts = 0;
        var middleware = new PollyResilienceMiddleware<TestContext>(
            RetryPipeline(2, new PredicateBuilder().Handle<BenzeneFailureResultException>()),
            isFailure: ctx => ctx.Failed);

        var context = new TestContext();

        // No exception escapes even though every attempt "failed": the sentinel is swallowed and the
        // failure result remains observable on the context.
        await middleware.HandleAsync(context, () =>
        {
            attempts++;
            context.Failed = true;
            return Task.CompletedTask;
        });

        Assert.Equal(3, attempts);
        Assert.True(context.Failed);
    }

    [Fact]
    public async Task HandleAsync_NoIsFailure_DoesNotRetryOnFailureResult()
    {
        var attempts = 0;
        var middleware = new PollyResilienceMiddleware<TestContext>(
            RetryPipeline(3, new PredicateBuilder().Handle<Exception>()));

        var context = new TestContext();
        await middleware.HandleAsync(context, () =>
        {
            attempts++;
            context.Failed = true; // a failure result, but no predicate wired -> not retried
            return Task.CompletedTask;
        });

        Assert.Equal(1, attempts);
    }
}
