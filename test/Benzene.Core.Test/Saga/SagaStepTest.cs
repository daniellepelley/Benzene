using System;
using System.Threading.Tasks;
using Benzene.Abstractions.Results;
using Benzene.Results;
using Benzene.Saga;
using Xunit;

namespace Benzene.Test.Saga;

public class SagaStepTest
{
    [Fact]
    public async Task ExecuteAsync_ReusedAcrossAttempts_DoesNotLeakAnEarlierAttemptsException()
    {
        // A saga retries by re-running the same step instances. If attempt 1 THREW and attempt 2 fails
        // by RETURNING a failed result, the step must not still report attempt 1's exception - that
        // made SagaResult.FailureException claim the final attempt threw when it did not.
        var calls = 0;
        var step = new SagaStep<string>(_ =>
        {
            calls++;
            if (calls == 1)
            {
                throw new InvalidOperationException("attempt-1-threw");
            }

            return Task.FromResult(BenzeneResult.ServiceUnavailable<string>());
        });

        await step.ExecuteAsync(new SagaContext());
        Assert.NotNull(step.Exception);

        await step.ExecuteAsync(new SagaContext());

        Assert.Equal(SagaStepState.Failed, step.State);
        Assert.Null(step.Exception);
    }
}
