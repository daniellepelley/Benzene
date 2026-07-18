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
        var entries = new List<MeshServiceRegistryEntry>();
        string? marker = null;

        do
        {
            var response = await _lambda.ListFunctionsAsync(
                new ListFunctionsRequest { Marker = marker }, cancellationToken);

            foreach (var function in response.Functions ?? new List<FunctionConfiguration>())
            {
                var tagsResponse = await _lambda.ListTagsAsync(
                    new ListTagsRequest { Resource = function.FunctionArn }, cancellationToken);
                var tags = tagsResponse.Tags ?? new Dictionary<string, string>();

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

            marker = response.NextMarker;
        }
        while (!string.IsNullOrEmpty(marker));

        return entries;
    }
}
