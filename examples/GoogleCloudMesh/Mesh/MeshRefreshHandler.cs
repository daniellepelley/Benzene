using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.Core.MessageHandlers;
using Benzene.Http;
using Benzene.Results;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Examples.GoogleCloudMesh.Mesh;

/// <summary>
/// On-demand aggregation trigger: <c>POST /mesh/refresh</c> runs a pass and returns 201 with the number
/// of services aggregated. Cloud Scheduler hits this on a schedule.
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
            var count = await _aggregation.RunAsync();
            return BenzeneResult.Created(new MeshRefreshResult(count));
        }
        catch (Exception ex)
        {
            return BenzeneResult.ServiceUnavailable<MeshRefreshResult>($"Mesh refresh failed: {ex.GetType().Name}");
        }
    }
}

/// <summary>The outcome of a mesh refresh.</summary>
public record MeshRefreshResult(int Aggregated);
