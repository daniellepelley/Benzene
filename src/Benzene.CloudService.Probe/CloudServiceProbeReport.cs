namespace Benzene.CloudService.Probe;

/// <summary>
/// The three verdicts an external, black-box probe can honestly reach for one requirement of the
/// Cloud Service Profile (docs/specification/cloud-service-profile.md §2). Unlike
/// <c>Benzene.CloudService.CloudServiceRequirement</c>'s simple bool (a wiring-time self-assessment
/// that trusts the service's own setup code), a probe run from outside the service genuinely cannot
/// always tell "yes" from "no" - some requirements (R8 in full, half of R6) are not observable over
/// HTTP from a single service at all. Collapsing that into a bool would silently overclaim, which
/// this codebase's "no silent caps" convention rules out. <see cref="Inconclusive"/> exists so the
/// probe can say "I don't know, and here is exactly why" instead of guessing.
/// </summary>
public enum CloudServiceProbeVerdict
{
    /// <summary>The probe independently observed evidence that the requirement holds.</summary>
    Satisfied,

    /// <summary>The probe reached the service and observed evidence that the requirement does not hold.</summary>
    NotSatisfied,

    /// <summary>
    /// The probe cannot determine this requirement from outside the service alone - either the
    /// relevant surface wasn't reachable at all (cascading from another failure), or the
    /// requirement is inherently unobservable by a single-service HTTP probe (see R8's and R6's
    /// remarks). Never treat this as a pass.
    /// </summary>
    Inconclusive
}

/// <summary>
/// One requirement of the Cloud Service Profile as independently assessed by an external
/// <see cref="CloudServiceProbe"/> run - not by the target service's own wiring code.
/// </summary>
public sealed class CloudServiceProbeRequirement
{
    public CloudServiceProbeRequirement(string id, string description, CloudServiceProbeVerdict verdict, string reason)
    {
        Id = id;
        Description = description;
        Verdict = verdict;
        Reason = reason;
    }

    /// <summary>The requirement id (R1-R8).</summary>
    public string Id { get; }

    /// <summary>What the requirement mandates, in one line.</summary>
    public string Description { get; }

    /// <summary>The probe's independently-observed verdict for this requirement.</summary>
    public CloudServiceProbeVerdict Verdict { get; }

    /// <summary>
    /// Why the verdict is what it is - always populated (unlike the self-check's optional
    /// <c>Note</c>), because an outside observer explaining its reasoning matters even more when
    /// it doesn't have the service's own word to fall back on.
    /// </summary>
    public string Reason { get; }
}

/// <summary>
/// The result of one <see cref="CloudServiceProbe"/> run against a live service: an external,
/// black-box, tri-state assessment of R1-R8 (docs/specification/cloud-service-profile.md §2) built
/// entirely from what the probe itself observed over real HTTP - it never trusts anything the
/// target service claims about itself (e.g. a <c>profile</c> field on its own descriptor). This is
/// the distinct, harder half of conformance checking described in the profile spec's §5: the
/// self-check (<c>Benzene.CloudService.CloudServiceProfileReport</c>) answers "what did this
/// service's own setup call provision"; this report answers "what can an outside operator or CI job
/// actually verify by hitting the service".
/// </summary>
public sealed class CloudServiceProbeReport
{
    public CloudServiceProbeReport(IReadOnlyList<CloudServiceProbeRequirement> requirements)
    {
        Requirements = requirements;
    }

    /// <summary>Every profile requirement with its independently-observed assessment, in R1-R8 order.</summary>
    public IReadOnlyList<CloudServiceProbeRequirement> Requirements { get; }

    /// <summary>The ids the probe positively observed as unmet.</summary>
    public IReadOnlyList<string> NotSatisfied =>
        Requirements.Where(x => x.Verdict == CloudServiceProbeVerdict.NotSatisfied).Select(x => x.Id).ToArray();

    /// <summary>The ids the probe could not determine from outside the service.</summary>
    public IReadOnlyList<string> Inconclusive =>
        Requirements.Where(x => x.Verdict == CloudServiceProbeVerdict.Inconclusive).Select(x => x.Id).ToArray();

    /// <summary>
    /// True when every requirement was independently observed as <see cref="CloudServiceProbeVerdict.Satisfied"/>.
    /// Given R8 (and half of R6) are structurally unobservable by a single-service probe (see their
    /// reasons), this will essentially never be true in practice for a real service - that's
    /// expected and correct, not a bug: it's the honest ceiling of what an external HTTP probe can
    /// claim. Check <see cref="NotSatisfied"/> and <see cref="Inconclusive"/> for the actual picture.
    /// </summary>
    public bool IsFullyConformant => Requirements.All(x => x.Verdict == CloudServiceProbeVerdict.Satisfied);
}
