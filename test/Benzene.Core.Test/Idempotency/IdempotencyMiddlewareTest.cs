using System;
using Benzene.Abstractions.Results;
using Benzene.Results;
using System.Threading.Tasks;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Core.MessageHandlers;
using Benzene.Idempotency;
using Xunit;

namespace Benzene.Test.Idempotency;

public class IdempotencyMiddlewareTest
{
    private class TestContext : IHasMessageResult
    {
        public IBenzeneResult MessageResult { get; set; } = null!;
    }

    private class FixedKeyStrategy : IIdempotencyKeyStrategy<TestContext>
    {
        private readonly string? _key;
        public FixedKeyStrategy(string? key) => _key = key;
        public string? GetKey(TestContext context) => _key;
    }

    private static IdempotencyMiddleware<TestContext> Middleware(
        IIdempotencyStore store, string? key = "key-1", IdempotencyOptions? options = null)
        => new(store, new FixedKeyStrategy(key), options ?? new IdempotencyOptions());

    [Fact]
    public async Task FirstMessage_InvokesHandler_AndRecordsCompletion()
    {
        var store = new InMemoryIdempotencyStore();
        var calls = 0;

        await Middleware(store).HandleAsync(new TestContext(), () => { calls++; return Task.CompletedTask; });

        Assert.Equal(1, calls);
        var claim = await store.TryClaimAsync("key-1");
        Assert.Equal(IdempotencyStatus.Completed, claim.ExistingRecord!.Status);
    }

    [Fact]
    public async Task DuplicateMessage_ShortCircuitsHandler()
    {
        var store = new InMemoryIdempotencyStore();
        var calls = 0;
        Func<Task> next = () => { calls++; return Task.CompletedTask; };

        await Middleware(store).HandleAsync(new TestContext(), next);
        await Middleware(store).HandleAsync(new TestContext(), next);

        Assert.Equal(1, calls); // handler ran only for the first copy
    }

    [Fact]
    public async Task DuplicateOfCompleted_ReplaysSuccessfulResult()
    {
        var store = new InMemoryIdempotencyStore();
        await Middleware(store).HandleAsync(new TestContext(), () => Task.CompletedTask);

        var duplicate = new TestContext();
        await Middleware(store).HandleAsync(duplicate, () => Task.CompletedTask);

        Assert.NotNull(duplicate.MessageResult);
        Assert.True(duplicate.MessageResult.IsSuccessful);
    }

    [Fact]
    public async Task HandlerThrows_ReleasesClaim_SoRedeliveryReprocesses()
    {
        var store = new InMemoryIdempotencyStore();
        var calls = 0;

        await Assert.ThrowsAsync<System.InvalidOperationException>(() =>
            Middleware(store).HandleAsync(new TestContext(), () =>
            {
                calls++;
                throw new System.InvalidOperationException("boom");
            }));

        // Claim was released: a redelivery gets a fresh claim and reprocesses.
        var reclaim = await store.TryClaimAsync("key-1");
        Assert.True(reclaim.Claimed);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task HandlerReportsFailureViaResult_ReleasesClaim_SoRedeliveryReprocesses()
    {
        var store = new InMemoryIdempotencyStore();
        var ctx = new TestContext();

        // Handler runs without throwing but the pipeline reports an unsuccessful result.
        await Middleware(store).HandleAsync(ctx, () =>
        {
            ctx.MessageResult = BenzeneResult.UnexpectedError();
            return Task.CompletedTask;
        });

        // The claim was released rather than marked completed, so a redelivery reprocesses.
        Assert.True((await store.TryClaimAsync("key-1")).Claimed);
    }

    [Fact]
    public async Task NoKey_ProcessesNormally_WithoutTouchingStore()
    {
        var store = new InMemoryIdempotencyStore();
        var calls = 0;
        Func<Task> next = () => { calls++; return Task.CompletedTask; };

        await Middleware(store, key: null).HandleAsync(new TestContext(), next);
        await Middleware(store, key: null).HandleAsync(new TestContext(), next);

        Assert.Equal(2, calls); // no de-duplication when there is no key
    }

    [Fact]
    public async Task InProgressDuplicate_WithThrowBehavior_ThrowsConflict()
    {
        var store = new InMemoryIdempotencyStore();
        await store.TryClaimAsync("key-1"); // simulate a sibling still in progress

        var options = new IdempotencyOptions { InProgressBehavior = InProgressBehavior.Throw };

        await Assert.ThrowsAsync<IdempotencyConflictException>(() =>
            Middleware(store, options: options).HandleAsync(new TestContext(), () => Task.CompletedTask));
    }

    [Fact]
    public async Task InProgressDuplicate_WithSkipBehavior_DropsSilently()
    {
        var store = new InMemoryIdempotencyStore();
        await store.TryClaimAsync("key-1"); // sibling in progress
        var calls = 0;

        await Middleware(store).HandleAsync(new TestContext(), () => { calls++; return Task.CompletedTask; });

        Assert.Equal(0, calls); // duplicate dropped, handler not invoked
    }
}
