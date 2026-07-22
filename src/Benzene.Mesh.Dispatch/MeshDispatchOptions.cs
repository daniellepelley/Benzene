namespace Benzene.Mesh.Dispatch;

/// <summary>Configuration for the opt-in mesh dispatch feature.</summary>
public class MeshDispatchOptions
{
    /// <summary>
    /// Whether dispatch is permitted when the process runs in a Production environment. Defaults to
    /// <c>false</c>: a dispatch invokes a target service's REAL handler with caller-supplied data (real
    /// side-effects execute - DB writes, downstream calls, the handler's own publishes), so it stays off
    /// in Production unless a human deliberately opts in here.
    /// </summary>
    public bool AllowInProduction { get; set; }
}
