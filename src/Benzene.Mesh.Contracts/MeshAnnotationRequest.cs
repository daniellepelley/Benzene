namespace Benzene.Mesh.Contracts;

/// <summary>
/// The <c>mesh:annotations:add</c> payload: attach one note to one entity. Plain settable
/// properties (wire-deserialized input, the <c>MeshServiceReport</c> convention) - validation
/// and bounds are the handler's job, not the shape's.
/// </summary>
public class MeshAnnotationRequest
{
    /// <summary>The entity to annotate - <c>"service:&lt;name&gt;"</c> or <c>"topic:&lt;topicId&gt;"</c>.</summary>
    public string? Entity { get; set; }

    /// <summary>The author's self-declared display name (see <see cref="MeshAnnotation"/> on identity).</summary>
    public string? Author { get; set; }

    /// <summary>The note, plain text.</summary>
    public string? Text { get; set; }
}
