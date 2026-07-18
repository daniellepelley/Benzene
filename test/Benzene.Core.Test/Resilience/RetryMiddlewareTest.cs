using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Benzene.Resilience;
using Xunit;

namespace Benzene.Test.Resilience;

public class RetryMiddlewareTest
{
    private static Func<TimeSpan, Task> NoDelay => _ => Task.CompletedTask;

    [Fact]
    public async Task HandleAsync_SucceedsAfterTransientFailures()
    {
        var attempts = 0;
        var middleware = new RetryMiddleware<object>(numberOfRetries: 3, delay: NoDelay);

        await middleware.HandleAsync(new object(), () =>
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
    public async Task HandleAsync_ExhaustsRetries_PropagatesException()
    {
        var attempts = 0;
        var middleware = new RetryMiddleware<object>(numberOfRetries: 2, delay: NoDelay);

        await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.HandleAsync(new object(), () =>
        {
            attempts++;
            throw new InvalidOperationException("always fails");
        }));

        Assert.Equal(3, attempts);
    }

    [Fact]
    public async Task HandleAsync_OperationCanceledException_NotRetriedByDefault()
    {
        var attempts = 0;
        var middleware = new RetryMiddleware<object>(numberOfRetries: 3, delay: NoDelay);

        await Assert.ThrowsAsync<OperationCanceledException>(() => middleware.HandleAsync(new object(), () =>
        {
            attempts++;
            throw new OperationCanceledException();
        }));

        Assert.Equal(1, attempts);
    }

    [Fact]
    public async Task HandleAsync_CustomShouldRetry_NarrowsDefaultBehavior()
    {
        var attempts = 0;
        var middleware = new RetryMiddleware<object>(
            numberOfRetries: 3,
            delay: NoDelay,
            shouldRetry: ex => ex is ArgumentException);

        await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.HandleAsync(new object(), () =>
        {
            attempts++;
            throw new InvalidOperationException("not retryable per custom predicate");
        }));

        Assert.Equal(1, attempts);
    }

    [Fact]
    public async Task HandleAsync_CustomShouldRetry_CanWidenToRetryCancellation()
    {
        var attempts = 0;
        var middleware = new RetryMiddleware<object>(
            numberOfRetries: 2,
            delay: NoDelay,
            shouldRetry: _ => true);

        await Assert.ThrowsAsync<OperationCanceledException>(() => middleware.HandleAsync(new object(), () =>
        {
            attempts++;
            throw new OperationCanceledException();
        }));

        Assert.Equal(3, attempts);
    }

    private class FakeSendContext
    {
        public bool Succeeded { get; set; }
    }

    [Fact]
    public async Task HandleAsync_ShouldRetryContext_RetriesNonThrowingFailure()
    {
        var attempts = 0;
        var context = new FakeSendContext();
        var middleware = new RetryMiddleware<FakeSendContext>(
            numberOfRetries: 3,
            delay: NoDelay,
            shouldRetryContext: c => !c.Succeeded);

        await middleware.HandleAsync(context, () =>
        {
            attempts++;
            context.Succeeded = attempts >= 3;
            return Task.CompletedTask;
        });

        Assert.Equal(3, attempts);
        Assert.True(context.Succeeded);
    }

    [Fact]
    public async Task HandleAsync_ShouldRetryContext_ExhaustsRetries_ReturnsWithoutThrowing()
    {
        var attempts = 0;
        var context = new FakeSendContext();
        var middleware = new RetryMiddleware<FakeSendContext>(
            numberOfRetries: 2,
            delay: NoDelay,
            shouldRetryContext: c => !c.Succeeded);

        await middleware.HandleAsync(context, () =>
        {
            attempts++;
            return Task.CompletedTask;
        });

        Assert.Equal(3, attempts);
        Assert.False(context.Succeeded);
    }

    [Fact]
    public async Task HandleAsync_DefaultShouldRetryContext_NeverRetriesOnSuccess()
    {
        var attempts = 0;
        var middleware = new RetryMiddleware<object>(numberOfRetries: 3, delay: NoDelay);

        await middleware.HandleAsync(new object(), () =>
        {
            attempts++;
            return Task.CompletedTask;
        });

        Assert.Equal(1, attempts);
    }

    [Fact]
    public async Task HandleAsync_BackoffGrowsExponentially()
    {
        var recordedDelays = new List<TimeSpan>();
        var middleware = new RetryMiddleware<object>(
            numberOfRetries: 3,
            initialDelay: TimeSpan.FromMilliseconds(10),
            backoffFactor: 3.0,
            delay: delay =>
            {
                recordedDelays.Add(delay);
                return Task.CompletedTask;
            });

        await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.HandleAsync(new object(), () =>
            throw new InvalidOperationException("always fails")));

        Assert.Equal(
            new[]
            {
                TimeSpan.FromMilliseconds(10),
                TimeSpan.FromMilliseconds(30),
                TimeSpan.FromMilliseconds(90),
            },
            recordedDelays);
    }

    [Fact]
    public async Task HandleAsync_MaxDelay_CapsTheActualSleepButNotTheUnderlyingGrowth()
    {
        var recordedDelays = new List<TimeSpan>();
        var middleware = new RetryMiddleware<object>(
            numberOfRetries: 3,
            initialDelay: TimeSpan.FromMilliseconds(10),
            backoffFactor: 3.0,
            maxDelay: TimeSpan.FromMilliseconds(50),
            delay: delay =>
            {
                recordedDelays.Add(delay);
                return Task.CompletedTask;
            });

        await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.HandleAsync(new object(), () =>
            throw new InvalidOperationException("always fails")));

        // Uncapped growth would be 10ms, 30ms, 90ms - the third attempt is capped at 50ms.
        Assert.Equal(
            new[]
            {
                TimeSpan.FromMilliseconds(10),
                TimeSpan.FromMilliseconds(30),
                TimeSpan.FromMilliseconds(50),
            },
            recordedDelays);
    }

    [Fact]
    public async Task HandleAsync_Jitter_AppliedToTheCappedDelayBeforeSleeping()
    {
        var recordedDelays = new List<TimeSpan>();
        var middleware = new RetryMiddleware<object>(
            numberOfRetries: 2,
            initialDelay: TimeSpan.FromMilliseconds(10),
            backoffFactor: 2.0,
            maxDelay: TimeSpan.FromMilliseconds(15),
            jitter: delay => delay + TimeSpan.FromMilliseconds(1),
            delay: delay =>
            {
                recordedDelays.Add(delay);
                return Task.CompletedTask;
            });

        await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.HandleAsync(new object(), () =>
            throw new InvalidOperationException("always fails")));

        // Uncapped/unjittered growth would be 10ms, 20ms; capped at 15ms, then +1ms jitter.
        Assert.Equal(
            new[]
            {
                TimeSpan.FromMilliseconds(11),
                TimeSpan.FromMilliseconds(16),
            },
            recordedDelays);
    }

    [Fact]
    public async Task HandleAsync_NoJitterOrMaxDelaySpecified_BehavesExactlyAsBefore()
    {
        var recordedDelays = new List<TimeSpan>();
        var middleware = new RetryMiddleware<object>(
            numberOfRetries: 2,
            initialDelay: TimeSpan.FromMilliseconds(10),
            backoffFactor: 2.0,
            delay: delay =>
            {
                recordedDelays.Add(delay);
                return Task.CompletedTask;
            });

        await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.HandleAsync(new object(), () =>
            throw new InvalidOperationException("always fails")));

        Assert.Equal(
            new[] { TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(20) },
            recordedDelays);
    }

    [Fact]
    public void FullJitter_ReturnsADurationBetweenZeroAndTheInputDelay()
    {
        var jitter = RetryMiddleware.FullJitter(new Random(42));
        var input = TimeSpan.FromMilliseconds(100);

        for (var i = 0; i < 20; i++)
        {
            var result = jitter(input);
            Assert.True(result >= TimeSpan.Zero);
            Assert.True(result <= input);
        }
    }
}
