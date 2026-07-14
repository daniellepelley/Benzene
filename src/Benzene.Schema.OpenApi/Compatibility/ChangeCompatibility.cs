namespace Benzene.Schema.OpenApi.Compatibility;

/// <summary>
/// How a single schema change affects contract compatibility between a client and the service it
/// was generated against.
/// </summary>
public enum ChangeCompatibility
{
    /// <summary>The change is safe — the existing client keeps working.</summary>
    Compatible,

    /// <summary>The change is probably safe but worth surfacing (a judgement call).</summary>
    Warning,

    /// <summary>The change breaks the existing client's contract.</summary>
    Breaking
}
