using Amazon.Lambda;
using Amazon.Lambda.Model;
using Benzene.Mesh.Contracts;

namespace Benzene.Mesh.Discovery.Aws;

/// <summary>
/// Discovers Benzene services by enumerating the AWS Lambda functions in the account (paginated
/// <c>ListFunctions</c>), reading each function's tags (<c>ListTags</c>), keeping those that match the
/// <see cref="MeshDiscoveryFilter"/> (by default, carry the <c>benzene</c> tag), and emitting a
/// registry entry bound to the AWS-Lambda-Invoke interrogation source
/// (<see cref="MeshServiceSource.AwsLambdaInvoke"/>) so the existing <c>LambdaMeshServiceSource</c>
/// interrogates each without any HTTP surface.
/// </summary>
/// <remarks>
/// Uses <c>ListFunctions</c> + per-function <c>ListTags</c> only (no ResourceGroupsTagging API), so it
/// needs no dependency beyond the already-approved <c>AWSSDK.Lambda</c>. IAM: <c>lambda:ListFunctions</c>,
/// <c>lambda:ListTags</c>, and <c>lambda:InvokeFunction</c> (for the later interrogation). An optional
/// <c>benzene:mesh-path</c> tag is carried into <c>SourceOptions</c> for services that serve the
/// descriptor at a non-default path.
/// </remarks>
public class AwsLambdaDiscoveryProvider : IMeshDiscoveryProvider
{
    /// <summary>The tag whose value (when present) overrides the mesh descriptor path for a service.</summary>
    public const string MeshPathTag = "benzene:mesh-path";

    /// <summary>
    /// Upper bound on concurrent <c>ListTags</c> calls during discovery. Keeps a large account from
    /// firing hundreds of tag reads at once and hitting the Lambda control-plane's request-rate limit,
    /// while still collapsing the previously-sequential per-function reads into a handful of round-trips.
    /// </summary>
    private const int MaxConcurrentTagReads = 8;

    private readonly IAmazonLambda _lambda;

    /// <summary>Initializes the provider over an AWS Lambda client.</summary>
    /// <param name="lambda">The AWS Lambda client used to list functions and their tags.</param>
    public AwsLambdaDiscoveryProvider(IAmazonLambda lambda)
    {
        _lambda = lambda;
    }

    /// <inheritdoc />
    public string Key => MeshServiceSource.AwsLambdaInvoke;

    /// <inheritdoc />
    public async Task<IReadOnlyList<MeshServiceRegistryEntry>> DiscoverAsync(
        MeshDiscoveryFilter filter, CancellationToken cancellationToken = default)
    {
        // Enumerate every function first (paginated), then read their tags concurrently. The per-function
        // ListTags call was previously awaited one-at-a-time, so discovery cost N sequential round-trips
        // across the whole account - the dominant part of a mesh refresh. Concurrency is bounded so a
        // large account can't fire hundreds of ListTags at once and trip the Lambda control-plane's
        // request-rate limit.
        var functions = new List<FunctionConfiguration>();
        string? marker = null;

        do
        {
            var response = await _lambda.ListFunctionsAsync(
                new ListFunctionsRequest { Marker = marker }, cancellationToken);

            if (response.Functions != null)
            {
                functions.AddRange(response.Functions);
            }

            marker = response.NextMarker;
        }
        while (!string.IsNullOrEmpty(marker));

        using var throttle = new SemaphoreSlim(MaxConcurrentTagReads);
        var tagged = await Task.WhenAll(functions.Select(async function =>
        {
            await throttle.WaitAsync(cancellationToken);
            try
            {
                var tagsResponse = await _lambda.ListTagsAsync(
                    new ListTagsRequest { Resource = function.FunctionArn }, cancellationToken);
                return (function, tags: tagsResponse.Tags ?? new Dictionary<string, string>());
            }
            finally
            {
                throttle.Release();
            }
        }));

        // Order-preserving (Task.WhenAll keeps source order), so the discovered registry is stable
        // across runs regardless of which tag read completes first.
        var entries = new List<MeshServiceRegistryEntry>();
        foreach (var (function, tags) in tagged)
        {
            if (!filter.Matches(tags))
            {
                continue;
            }

            var options = new Dictionary<string, string> { ["functionName"] = function.FunctionName };
            if (tags.TryGetValue(MeshPathTag, out var meshPath) && !string.IsNullOrWhiteSpace(meshPath))
            {
                options["meshPath"] = meshPath;
            }

            entries.Add(new MeshServiceRegistryEntry(
                function.FunctionName,
                specUrl: string.Empty,
                healthUrl: string.Empty,
                MeshServiceSource.AwsLambdaInvoke,
                options));
        }

        return entries;
    }
}
