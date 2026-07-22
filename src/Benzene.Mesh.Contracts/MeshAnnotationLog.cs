namespace Benzene.Mesh.Contracts;

/// <summary>
/// The <c>annotations.json</c> shape - every recorded <see cref="MeshAnnotation"/>, published
/// into the same artifact store as <c>manifest.json</c>/<c>topics.json</c>. That placement is the
/// vessel decision for discussion (the vision doc's "hard constraint"): the <em>read</em> path is
/// a plain static artifact any static host serves - the explorer renders recorded discussion with
/// zero backend - and only the <em>write</em> path (the <c>mesh:annotations:add</c> handler)
/// needs a live endpoint, which the UI feature-detects and degrades to read-only without.
/// </summary>
public class MeshAnnotationLog
{
    /// <summary>Initializes a new instance of the <see cref="MeshAnnotationLog"/> class.</summary>
    /// <param name="generatedAtUtc">When this log was last written.</param>
    /// <param name="annotations">Every recorded annotation, oldest first; readers group by <see cref="MeshAnnotation.Entity"/>.</param>
    public MeshAnnotationLog(DateTimeOffset generatedAtUtc, MeshAnnotation[] annotations)
    {
        GeneratedAtUtc = generatedAtUtc;
        Annotations = annotations;
    }

    /// <summary>When this log was last written.</summary>
    public DateTimeOffset GeneratedAtUtc { get; }

    /// <summary>Every recorded annotation, oldest first; readers group by <see cref="MeshAnnotation.Entity"/>.</summary>
    public MeshAnnotation[] Annotations { get; }
}
