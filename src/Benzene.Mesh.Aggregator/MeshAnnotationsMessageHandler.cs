using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.Core.MessageHandlers;
using Benzene.Http;
using Benzene.Mesh.Contracts;
using Benzene.Results;

namespace Benzene.Mesh.Aggregator;

/// <summary>
/// The discussion write endpoint - attaches one note to one entity of the estate and returns that
/// entity's full thread. Reachable on whatever transport the host already runs, the same
/// dogfooded shape as <see cref="MeshReportMessageHandler"/>, and the same opt-in: only
/// discoverable if the host's own <c>.AddMessageHandlers()</c> includes it, so a deployment that
/// never wires it up has a strictly read-only (static-artifact) discussion surface.
/// </summary>
/// <remarks>
/// Identity is deliberately out of scope here: <see cref="MeshAnnotationRequest.Author"/> is a
/// self-declared display name, and authenticating who may post (and verifying who they are) is
/// the fronting gateway's job - the <c>Benzene.RateLimiting</c> boundary ruling applied to
/// writes. The handler enforces only shape: required fields and size bounds, so one post can't
/// bloat the artifact every reader downloads.
/// </remarks>
[HttpEndpoint("POST", "/mesh/annotations")]
[Message("mesh:annotations:add")]
public class MeshAnnotationsMessageHandler : IMessageHandler<MeshAnnotationRequest, MeshAnnotationThread>
{
    /// <summary>Maximum accepted length of <see cref="MeshAnnotationRequest.Entity"/>.</summary>
    public const int MaxEntityLength = 200;

    /// <summary>Maximum accepted length of <see cref="MeshAnnotationRequest.Author"/>.</summary>
    public const int MaxAuthorLength = 80;

    /// <summary>Maximum accepted length of <see cref="MeshAnnotationRequest.Text"/>.</summary>
    public const int MaxTextLength = 4000;

    private readonly MeshAnnotationPublisher _publisher;

    /// <summary>Initializes a new instance of the <see cref="MeshAnnotationsMessageHandler"/> class.</summary>
    /// <param name="publisher">Appends the note to the annotations artifact.</param>
    public MeshAnnotationsMessageHandler(MeshAnnotationPublisher publisher)
    {
        _publisher = publisher;
    }

    /// <inheritdoc />
    public async Task<IBenzeneResult<MeshAnnotationThread>> HandleAsync(MeshAnnotationRequest request)
    {
        var entity = request.Entity?.Trim();
        var author = request.Author?.Trim();
        var text = request.Text?.Trim();

        if (string.IsNullOrEmpty(entity))
        {
            return BenzeneResult.BadRequest<MeshAnnotationThread>("entity is required");
        }
        if (string.IsNullOrEmpty(author))
        {
            return BenzeneResult.BadRequest<MeshAnnotationThread>("author is required");
        }
        if (string.IsNullOrEmpty(text))
        {
            return BenzeneResult.BadRequest<MeshAnnotationThread>("text is required");
        }
        if (entity!.Length > MaxEntityLength || author!.Length > MaxAuthorLength || text!.Length > MaxTextLength)
        {
            return BenzeneResult.BadRequest<MeshAnnotationThread>(
                $"bounds exceeded: entity <= {MaxEntityLength}, author <= {MaxAuthorLength}, text <= {MaxTextLength} characters");
        }

        var thread = await _publisher.AddAsync(entity!, author!, text!);
        return BenzeneResult.Created(thread);
    }
}
