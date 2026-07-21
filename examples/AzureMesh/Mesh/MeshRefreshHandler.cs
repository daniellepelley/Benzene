using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.Core.MessageHandlers;
using Benzene.Http;
using Benzene.Results;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Examples.AzureMesh.Mesh;

/// <summary>
/// On-demand discovery + aggregation trigger: <c>POST /mesh/refresh</c> runs a pass and returns 201
/// with the number of services discovered (a pass creates/refreshes the catalog artifacts).
/// </summary>
[Message("mesh:refresh")]
[HttpEndpoint("POST", "/mesh/refresh")]
public class MeshRefreshHandler : IMessageHandler<Void, MeshRefreshResult>
{
    private readonly MeshAggregationService _aggregation;

    public MeshRefreshHandler(MeshAggregationService aggregation)
    {
        _aggregation = aggregation;
    }

    public async Task<IBenzeneResult<MeshRefreshResult>> HandleAsync(Void request)
    {
        try
        {
            var discovered = await _aggregation.RunAsync();
            return BenzeneResult.Created(new MeshRefreshResult(discovered));
        }
        catch (Exception ex)
        {
            // A transient ARM throttle / interrogation error must not surface as a raw 500. Report it as
            // a 503 (the background pass keeps retrying regardless) — never leak the exception message.
            return BenzeneResult.ServiceUnavailable<MeshRefreshResult>($"Mesh refresh failed: {ex.GetType().Name}");
        }
    }
}

/// <summary>The outcome of a mesh refresh.</summary>
public record MeshRefreshResult(int Discovered);
