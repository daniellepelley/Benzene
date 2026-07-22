namespace Benzene.Mesh.Contracts;

/// <summary>
/// The <c>mesh:annotations:add</c> response: the annotated entity's full thread after the append,
/// so a UI can re-render the discussion it just posted into without re-fetching (or racing) the
/// <c>annotations.json</c> artifact.
/// </summary>
public class MeshAnnotationThread
{
    /// <summary>Initializes a new instance of the <see cref="MeshAnnotationThread"/> class.</summary>
    /// <param name="entity">The entity the thread belongs to.</param>
    /// <param name="annotations">Every annotation on that entity, oldest first.</param>
    public MeshAnnotationThread(string entity, MeshAnnotation[] annotations)
    {
        Entity = entity;
        Annotations = annotations;
    }

    /// <summary>The entity the thread belongs to.</summary>
    public string Entity { get; }

    /// <summary>Every annotation on that entity, oldest first.</summary>
    public MeshAnnotation[] Annotations { get; }
}
