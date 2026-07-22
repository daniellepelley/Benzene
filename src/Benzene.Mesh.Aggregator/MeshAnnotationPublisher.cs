using System.Text.Json;
using Benzene.Mesh.Contracts;

namespace Benzene.Mesh.Aggregator;

/// <summary>
/// Appends discussion notes to the <c>annotations.json</c> artifact - the write half of the mesh
/// discussion feature. The read half is the artifact itself: it lives in the same
/// <see cref="IMeshArtifactStore"/> as <c>manifest.json</c>, so any static host serves recorded
/// discussion with zero backend, and only posting a new note needs this publisher's handler
/// (<see cref="MeshAnnotationsMessageHandler"/>) running somewhere.
/// </summary>
/// <remarks>
/// Read-modify-write of the log is serialized per process with a semaphore; concurrent writers in
/// <em>separate</em> hosts are out of scope (the same single-writer assumption the aggregator's
/// drift comparison already makes about its store). An unparseable existing log starts fresh
/// rather than failing - but never silently: the previous content is preserved to a timestamped
/// sibling artifact first.
/// </remarks>
public class MeshAnnotationPublisher
{
    private const string ArtifactPath = "annotations.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    private readonly IMeshArtifactStore _store;
    private readonly Func<DateTimeOffset> _clock;
    private readonly Func<string> _ids;
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    /// <summary>Initializes a new instance of the <see cref="MeshAnnotationPublisher"/> class.</summary>
    /// <param name="store">The artifact store the log lives in - the same one the aggregator publishes to.</param>
    /// <param name="clock">Supplies the current time; defaults to <see cref="DateTimeOffset.UtcNow"/>. Overridable for deterministic tests.</param>
    /// <param name="ids">Supplies new annotation ids; defaults to <see cref="Guid.NewGuid"/>. Overridable for deterministic tests.</param>
    public MeshAnnotationPublisher(IMeshArtifactStore store, Func<DateTimeOffset>? clock = null, Func<string>? ids = null)
    {
        _store = store;
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
        _ids = ids ?? (() => Guid.NewGuid().ToString("n"));
    }

    /// <summary>
    /// Appends one note to the log and republishes <c>annotations.json</c>, returning the
    /// annotated entity's full thread (oldest first) so a caller can render the discussion it
    /// just posted into. Inputs are assumed already validated (the handler's job).
    /// </summary>
    /// <param name="entity">The entity to annotate.</param>
    /// <param name="author">The author's self-declared display name.</param>
    /// <param name="text">The note text.</param>
    public async Task<MeshAnnotationThread> AddAsync(string entity, string author, string text)
    {
        await _writeLock.WaitAsync();
        try
        {
            var existing = await ReadLogAsync();
            var annotation = new MeshAnnotation(_ids(), entity, author, text, _clock());
            var annotations = existing.Append(annotation).ToArray();

            await _store.PublishAsync(ArtifactPath, JsonSerializer.Serialize(new MeshAnnotationLog(_clock(), annotations), JsonOptions));

            return new MeshAnnotationThread(entity,
                annotations.Where(a => a.Entity == entity).OrderBy(a => a.CreatedAtUtc).ToArray());
        }
        finally
        {
            _writeLock.Release();
        }
    }

    private async Task<MeshAnnotation[]> ReadLogAsync()
    {
        string? json = null;
        try
        {
            json = await _store.TryReadAsync(ArtifactPath);
            if (json == null)
            {
                return Array.Empty<MeshAnnotation>();
            }
            return JsonSerializer.Deserialize<MeshAnnotationLog>(json, JsonOptions)?.Annotations ?? Array.Empty<MeshAnnotation>();
        }
        catch
        {
            // A corrupt log must not brick discussion forever - but user-authored notes are the
            // one artifact that can't be regenerated from the fleet, so park the unreadable
            // content aside (best-effort) instead of overwriting it.
            if (json != null)
            {
                try
                {
                    await _store.PublishAsync(
                        $"annotations.unreadable-{_clock().ToUnixTimeSeconds()}.json", json);
                }
                catch
                {
                    // Preserving the corrupt log is best-effort only.
                }
            }
            return Array.Empty<MeshAnnotation>();
        }
    }
}
