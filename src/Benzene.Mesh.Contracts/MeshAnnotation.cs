namespace Benzene.Mesh.Contracts;

/// <summary>
/// One recorded note in the mesh's discussion log - a human's remark attached to an entity of the
/// estate ("retire this after finance signs off", "drift here was intentional, v2 migration").
/// The decisions the explorer's evidence provokes get recorded next to the evidence, instead of
/// evaporating into chat.
/// </summary>
/// <remarks>
/// <see cref="Author"/> is a self-declared display name, deliberately not an authenticated
/// identity: Benzene services run behind the deployment's own gateway, and authenticating
/// who may annotate (and verifying who they are) is that gateway's job - the same boundary
/// ruling as <c>Benzene.RateLimiting</c>'s "authoritative limiting belongs at the gateway".
/// A deployment wanting verified attribution fronts the annotations endpoint with its
/// IdP-protected gateway; the mesh packages stay identity-free.
/// </remarks>
public class MeshAnnotation
{
    /// <summary>Initializes a new instance of the <see cref="MeshAnnotation"/> class.</summary>
    /// <param name="id">The annotation's unique id.</param>
    /// <param name="entity">The entity the note is attached to - <c>"service:&lt;name&gt;"</c> or <c>"topic:&lt;topicId&gt;"</c>, mirroring the explorer's own entity model.</param>
    /// <param name="author">The self-declared display name of whoever wrote the note (see remarks).</param>
    /// <param name="text">The note itself, plain text.</param>
    /// <param name="createdAtUtc">When the note was recorded.</param>
    public MeshAnnotation(string id, string entity, string author, string text, DateTimeOffset createdAtUtc)
    {
        Id = id;
        Entity = entity;
        Author = author;
        Text = text;
        CreatedAtUtc = createdAtUtc;
    }

    /// <summary>The annotation's unique id.</summary>
    public string Id { get; }

    /// <summary>The entity the note is attached to - <c>"service:&lt;name&gt;"</c> or <c>"topic:&lt;topicId&gt;"</c>.</summary>
    public string Entity { get; }

    /// <summary>The self-declared display name of whoever wrote the note.</summary>
    public string Author { get; }

    /// <summary>The note itself, plain text.</summary>
    public string Text { get; }

    /// <summary>When the note was recorded.</summary>
    public DateTimeOffset CreatedAtUtc { get; }
}
