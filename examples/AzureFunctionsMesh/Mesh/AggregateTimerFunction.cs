using Benzene.Azure.Function.Core;
using Benzene.Azure.Function.Timer;
using Microsoft.Azure.Functions.Worker;

namespace Benzene.Examples.AzureFunctionsMesh.Mesh;

/// <summary>
/// The scheduled discovery + aggregation pass — the Azure Functions replacement for the Web App mesh's
/// <c>BackgroundService</c> (a Consumption-plan Function has no always-on background thread to rely on).
/// Every minute it dispatches a timer tick into the Benzene app, whose timer pipeline runs one
/// <see cref="MeshAggregationService"/> pass. <c>RunOnStartup</c> refreshes the catalog immediately on a
/// cold start rather than waiting for the first tick.
/// </summary>
public class AggregateTimerFunction
{
    private readonly IAzureFunctionApp _app;

    public AggregateTimerFunction(IAzureFunctionApp app)
    {
        _app = app;
    }

    [Function("aggregate")]
    public Task Run([TimerTrigger("0 */1 * * * *", RunOnStartup = true)] TimerInfo timer)
    {
        return _app.HandleTimer();
    }
}
