using Amazon.Lambda;
using Benzene.Clients;
using Benzene.Clients.Aws.Lambda;
using Benzene.Mesh.Aggregator;
using Benzene.Mesh.Contracts;

namespace Benzene.Mesh.Aws.Lambda;

/// <summary>
/// Fetches a service's spec/health via a synchronous AWS Lambda <see cref="InvocationType.RequestResponse"/>
/// <c>Invoke</c> (<see cref="IAwsLambdaClient"/>), for services with no public HTTP surface. Sends the
/// literal topics <c>"spec"</c>/<c>"healthcheck"</c> - any service already wired the normal Benzene way
/// (<c>UseBenzeneMessage()</c> + <c>.UseSpec()</c> + <c>.UseHealthCheck(...)</c>) already answers a
/// direct Lambda invocation carrying either topic, with zero target-side changes, via
/// <c>Benzene.Aws.Lambda.Core.BenzeneMessage.BenzeneMessageLambdaHandler</c>.
/// </summary>
/// <remarks>
/// Invoking a Lambda synchronously *causes* a cold start if nothing is warm - it does not require
/// the function to already be running - so this is subject to the same "wake it on demand and wait"
/// latency as an HTTP health endpoint on a Lambda, not a fundamentally slower or riskier operation.
/// </remarks>
public class LambdaMeshServiceSource : IMeshServiceSource
{
    /// <summary>The <see cref="MeshServiceRegistryEntry.SourceOptions"/> key naming the target Lambda function.</summary>
    public const string FunctionNameOption = "functionName";

    // Deliberately hardcoded, not referencing Benzene.Schema.OpenApi.Constants.DefaultSpecTopic /
    // Benzene.HealthChecks.Constants.DefaultHealthCheckTopic directly - keeps this adapter's
    // dependency graph to just Benzene.Mesh.Aggregator + Benzene.Clients.Aws + the AWS Lambda SDK.
    // LambdaMeshServiceSourceTest's two "...MatchingBenzene..." tests pin these against both
    // constants so a future rename of either fails loudly here instead of silently breaking this adapter.
    private const string SpecTopic = "spec";
    private const string HealthTopic = "healthcheck";

    private readonly IAwsLambdaClient _client;

    /// <summary>Initializes a new instance of the <see cref="LambdaMeshServiceSource"/> class.</summary>
    /// <param name="client">The client used to invoke each service's Lambda function.</param>
    public LambdaMeshServiceSource(IAwsLambdaClient client)
    {
        _client = client;
    }

    /// <inheritdoc />
    public string Key => MeshServiceSource.AwsLambdaInvoke;

    /// <inheritdoc />
    public Task<string> FetchSpecAsync(MeshServiceRegistryEntry entry, CancellationToken cancellationToken) =>
        InvokeAsync(entry, SpecTopic, cancellationToken);

    /// <inheritdoc />
    public Task<string> FetchHealthAsync(MeshServiceRegistryEntry entry, CancellationToken cancellationToken) =>
        InvokeAsync(entry, HealthTopic, cancellationToken);

    private async Task<string> InvokeAsync(MeshServiceRegistryEntry entry, string topic, CancellationToken cancellationToken)
    {
        var functionName = ResolveFunctionName(entry);
        var request = new BenzeneMessageClientRequest(topic, new Dictionary<string, string>(), string.Empty);

        // IAwsLambdaClient.SendMessageAsync has no CancellationToken parameter - WaitAsync races
        // the call against the caller's token so MeshAggregator's PerServiceFetchTimeout is still
        // honored from the caller's point of view, even though the underlying Invoke itself can't
        // be aborted mid-flight (the same limitation any fire-and-await network call has once the
        // request is already in flight).
        var response = await _client
            .SendMessageAsync<BenzeneMessageClientRequest, BenzeneMessageClientResponse>(request, functionName, InvocationType.RequestResponse)
            .WaitAsync(cancellationToken);

        return response.Body;
    }

    private static string ResolveFunctionName(MeshServiceRegistryEntry entry)
    {
        if (entry.SourceOptions != null && entry.SourceOptions.TryGetValue(FunctionNameOption, out var functionName))
        {
            return functionName;
        }

        throw new InvalidOperationException(
            $"Mesh service \"{entry.Name}\" uses source \"{MeshServiceSource.AwsLambdaInvoke}\" but has no \"{FunctionNameOption}\" in SourceOptions.");
    }
}
