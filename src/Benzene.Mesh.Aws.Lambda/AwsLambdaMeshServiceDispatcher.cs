using System;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Lambda;
using Benzene.Clients;
using Benzene.Clients.Aws.Lambda;
using Benzene.Mesh.Contracts;
using Benzene.Mesh.Dispatch;

namespace Benzene.Mesh.Aws.Lambda;

/// <summary>
/// Dispatches to an AWS-Lambda service via a synchronous <see cref="InvocationType.RequestResponse"/>
/// <c>Invoke</c> - reusing the exact client stack (<see cref="IAwsLambdaClient"/>) and
/// <c>lambda:InvokeFunction</c> grant <see cref="LambdaMeshServiceSource"/> already uses to interrogate
/// each service, changing only the topic/body. Requires <c>SourceOptions["functionName"]</c>.
/// </summary>
public class AwsLambdaMeshServiceDispatcher : IMeshServiceDispatcher
{
    private readonly Lazy<IAwsLambdaClient> _client;

    /// <summary>Initializes a new instance of the <see cref="AwsLambdaMeshServiceDispatcher"/> class.</summary>
    public AwsLambdaMeshServiceDispatcher(IAwsLambdaClient client)
        : this(new Lazy<IAwsLambdaClient>(() => client))
    {
    }

    /// <summary>Initializes a new instance with a lazily-built client (see <see cref="LambdaMeshServiceSource"/> for why).</summary>
    public AwsLambdaMeshServiceDispatcher(Lazy<IAwsLambdaClient> client)
    {
        _client = client;
    }

    /// <inheritdoc />
    public string Key => MeshServiceSource.AwsLambdaInvoke;

    /// <inheritdoc />
    public async Task<MeshDispatchResult> DispatchAsync(MeshServiceRegistryEntry entry, MeshDispatchEnvelope envelope, CancellationToken cancellationToken)
    {
        var functionName = ResolveFunctionName(entry);
        var request = new BenzeneMessageClientRequest(envelope.Topic, envelope.Headers, envelope.Body);

        var response = await _client.Value
            .SendMessageAsync<BenzeneMessageClientRequest, BenzeneMessageClientResponse>(request, functionName, InvocationType.RequestResponse)
            .WaitAsync(cancellationToken);

        return new MeshDispatchResult(response.StatusCode, response.Body, response.Headers);
    }

    private static string ResolveFunctionName(MeshServiceRegistryEntry entry)
    {
        if (entry.SourceOptions != null
            && entry.SourceOptions.TryGetValue(LambdaMeshServiceSource.FunctionNameOption, out var functionName))
        {
            return functionName;
        }

        throw new InvalidOperationException(
            $"Mesh service \"{entry.Name}\" uses source \"{MeshServiceSource.AwsLambdaInvoke}\" but has no "
            + $"\"{LambdaMeshServiceSource.FunctionNameOption}\" in SourceOptions.");
    }
}
