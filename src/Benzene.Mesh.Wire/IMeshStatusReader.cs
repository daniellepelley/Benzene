using Benzene.Core.Messages.BenzeneMessage;

namespace Benzene.Mesh.Wire;

/// <summary>
/// Reads the invocation's final Benzene status back from a transport context after the pipeline
/// has run - the per-transport adapter the trace middleware uses to fill
/// <see cref="MeshTraceEvent.Status"/>, following the same per-context mapper idiom as
/// <c>IMessageGetter&lt;TContext&gt;</c>. A transport without a reader degrades to an empty status
/// (docs/specification/mesh.md §3's "no result produced" reading), never an error.
/// </summary>
public interface IMeshStatusReader<in TContext>
{
    string? GetStatus(TContext context);
}

/// <summary>The <see cref="BenzeneMessageContext"/> reader: the response's status code is the wire status.</summary>
public class BenzeneMessageMeshStatusReader : IMeshStatusReader<BenzeneMessageContext>
{
    public string? GetStatus(BenzeneMessageContext context)
    {
        return context.BenzeneMessageResponse?.StatusCode;
    }
}
