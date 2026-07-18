using System;
using System.Threading;
using System.Threading.Tasks;
using Benzene.Idempotency;
using Xunit;

namespace Benzene.Test.Idempotency;

public class InMemoryIdempotencyStoreTest
{
    [Fact]
    public async Task TryClaim_FirstTime_Wins()
    {
        var store = new InMemoryIdempotencyStore();

        var claim = await store.TryClaimAsync("key-1");

        Assert.True(claim.Claimed);
        Assert.Null(claim.ExistingRecord);
    }

    [Fact]
    public async Task TryClaim_SecondTimeWhileInProgress_IsRefusedWithInProgressRecord()
    {
        var store = new InMemoryIdempotencyStore();
        await store.TryClaimAsync("key-1");

        var claim = await store.TryClaimAsync("key-1");

        Assert.False(claim.Claimed);
        Assert.NotNull(claim.ExistingRecord);
        Assert.Equal(IdempotencyStatus.InProgress, claim.ExistingRecord!.Status);
    }

    [Fact]
    public async Task TryClaim_AfterComplete_IsRefusedWithCompletedOutcome()
    {
        var store = new InMemoryIdempotencyStore();
        await store.TryClaimAsync("key-1");
        await store.CompleteAsync("key-1", wasSuccessful: true);

        var claim = await store.TryClaimAsync("key-1");

        Assert.False(claim.Claimed);
        Assert.Equal(IdempotencyStatus.Completed, claim.ExistingRecord!.Status);
        Assert.True(claim.ExistingRecord.WasSuccessful);
    }

    [Fact]
    public async Task Release_AllowsReclaim()
    {
        var store = new InMemoryIdempotencyStore();
        await store.TryClaimAsync("key-1");

        await store.ReleaseAsync("key-1");
        var claim = await store.TryClaimAsync("key-1");

        Assert.True(claim.Claimed);
    }

    [Fact]
    public async Task TryClaim_AfterTtlExpiry_AllowsReclaim()
    {
        var now = DateTimeOffset.UtcNow;
        var store = new InMemoryIdempotencyStore(timeToLive: TimeSpan.FromMinutes(10), now: () => now);
        await store.TryClaimAsync("key-1");

        // A duplicate within the TTL is still refused...
        Assert.False((await store.TryClaimAsync("key-1")).Claimed);

        // ...but once the record has expired, the key can be claimed again.
        now = now.AddMinutes(11);
        Assert.True((await store.TryClaimAsync("key-1")).Claimed);
    }

    [Fact]
    public async Task DifferentKeys_AreIndependent()
    {
        var store = new InMemoryIdempotencyStore();

        Assert.True((await store.TryClaimAsync("key-a")).Claimed);
        Assert.True((await store.TryClaimAsync("key-b")).Claimed);
    }

    [Fact]
    public async Task TryClaim_AlreadyCancelledToken_Throws()
    {
        var store = new InMemoryIdempotencyStore();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => store.TryClaimAsync("key-1", cts.Token));
    }

    [Fact]
    public async Task CompleteAndRelease_AlreadyCancelledToken_Throw()
    {
        var store = new InMemoryIdempotencyStore();
        await store.TryClaimAsync("key-1");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => store.CompleteAsync("key-1", wasSuccessful: true, cts.Token));
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => store.ReleaseAsync("key-1", cts.Token));
    }
}
