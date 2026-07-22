namespace Benzene.Mesh.Dispatch;

/// <summary>Decides whether a dispatch is currently permitted, and gives the reason when it isn't.</summary>
public class MeshDispatchGate
{
    private readonly MeshDispatchOptions _options;
    private readonly IMeshDispatchEnvironment _environment;

    /// <summary>Initializes a new instance of the <see cref="MeshDispatchGate"/> class.</summary>
    public MeshDispatchGate(MeshDispatchOptions options, IMeshDispatchEnvironment environment)
    {
        _options = options;
        _environment = environment;
    }

    /// <summary>
    /// Dispatch is allowed only outside Production, unless
    /// <see cref="MeshDispatchOptions.AllowInProduction"/> is explicitly set. Firing a real handler with
    /// test data has real side-effects, so the default is off (an unset environment counts as Production).
    /// </summary>
    public bool IsAllowed => !_environment.IsProduction || _options.AllowInProduction;

    /// <summary>A human-readable reason dispatch is blocked, returned when <see cref="IsAllowed"/> is false.</summary>
    public string BlockedReason =>
        "Mesh dispatch is disabled in this environment. It invokes a service's real handler with the "
        + "supplied payload (real side-effects execute), so it is off in Production unless "
        + "MeshDispatchOptions.AllowInProduction is explicitly set.";
}
