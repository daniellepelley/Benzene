using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Benzene.Mesh.Aggregator;
using Benzene.Mesh.Contracts;
using Benzene.Results;
using Xunit;

namespace Benzene.Mesh.Test;

/// <summary>
/// The discussion feature's write path: <see cref="MeshAnnotationPublisher"/> (append + artifact
/// round-trip + corrupt-log preservation) and <see cref="MeshAnnotationsMessageHandler"/>
/// (validation bounds, thread response). The read path has no code to test - it's the
/// <c>annotations.json</c> artifact itself.
/// </summary>
public class MeshAnnotationsTest : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly string _rootDirectory = Path.Combine(Path.GetTempPath(), "benzene-mesh-annotations-test-" + Guid.NewGuid());

    private FileSystemMeshArtifactStore Store() => new(_rootDirectory);

    private static MeshAnnotationRequest Request(string? entity = "topic:order:legacy-export",
        string? author = "dani", string? text = "No consumers since May - propose retiring.")
    {
        return new MeshAnnotationRequest { Entity = entity, Author = author, Text = text };
    }

    [Fact]
    public async Task AddAsync_FirstNote_CreatesTheArtifactAndReturnsTheThread()
    {
        var at = new DateTimeOffset(2026, 7, 22, 9, 0, 0, TimeSpan.Zero);
        var store = Store();
        var publisher = new MeshAnnotationPublisher(store, () => at, () => "id-1");

        var thread = await publisher.AddAsync("topic:order:legacy-export", "dani", "propose retiring");

        var note = Assert.Single(thread.Annotations);
        Assert.Equal("id-1", note.Id);
        Assert.Equal("topic:order:legacy-export", note.Entity);
        Assert.Equal(at, note.CreatedAtUtc);

        var log = JsonSerializer.Deserialize<MeshAnnotationLog>((await store.TryReadAsync("annotations.json"))!, JsonOptions)!;
        Assert.Equal("propose retiring", Assert.Single(log.Annotations).Text);
    }

    [Fact]
    public async Task AddAsync_AppendsAcrossPublisherInstances_AndThreadsFilterPerEntity()
    {
        // Durability is the artifact store's: a fresh publisher (host restart) reads the existing
        // log. The returned thread carries only the asked-for entity, oldest first.
        var store = Store();
        await new MeshAnnotationPublisher(store).AddAsync("service:payments-api", "sam", "drift here was intentional");
        var thread = await new MeshAnnotationPublisher(store).AddAsync("topic:order:legacy-export", "dani", "propose retiring");

        Assert.Equal("topic:order:legacy-export", Assert.Single(thread.Annotations).Entity);

        var log = JsonSerializer.Deserialize<MeshAnnotationLog>((await store.TryReadAsync("annotations.json"))!, JsonOptions)!;
        Assert.Equal(2, log.Annotations.Length);
    }

    [Fact]
    public async Task AddAsync_CorruptExistingLog_IsParkedAsideNotOverwrittenSilently()
    {
        // Notes are the one artifact that can't be regenerated from the fleet - an unreadable log
        // must not brick discussion, but must not be silently discarded either.
        var store = Store();
        await store.PublishAsync("annotations.json", "{not json");
        var at = new DateTimeOffset(2026, 7, 22, 9, 0, 0, TimeSpan.Zero);

        var thread = await new MeshAnnotationPublisher(store, () => at).AddAsync("service:payments-api", "sam", "first note after corruption");

        Assert.Single(thread.Annotations);
        Assert.Equal("{not json", await store.TryReadAsync($"annotations.unreadable-{at.ToUnixTimeSeconds()}.json"));
    }

    [Theory]
    [InlineData(null, "dani", "text", "entity")]
    [InlineData("topic:t", null, "text", "author")]
    [InlineData("topic:t", "dani", null, "text")]
    [InlineData("topic:t", "dani", "   ", "text")]
    public async Task HandleAsync_MissingField_IsBadRequest(string? entity, string? author, string? text, string named)
    {
        var handler = new MeshAnnotationsMessageHandler(new MeshAnnotationPublisher(Store()));

        var result = await handler.HandleAsync(Request(entity, author, text));

        Assert.Equal(BenzeneResultStatus.BadRequest, result.Status);
        Assert.Contains(named, string.Join(" ", result.Errors));
    }

    [Fact]
    public async Task HandleAsync_OversizedText_IsBadRequest()
    {
        var handler = new MeshAnnotationsMessageHandler(new MeshAnnotationPublisher(Store()));

        var result = await handler.HandleAsync(Request(text: new string('x', MeshAnnotationsMessageHandler.MaxTextLength + 1)));

        Assert.Equal(BenzeneResultStatus.BadRequest, result.Status);
    }

    [Fact]
    public async Task HandleAsync_ValidNote_ReturnsCreatedWithTheEntityThread()
    {
        var handler = new MeshAnnotationsMessageHandler(new MeshAnnotationPublisher(Store()));

        var result = await handler.HandleAsync(Request());

        Assert.Equal(BenzeneResultStatus.Created, result.Status);
        Assert.Equal("topic:order:legacy-export", result.Payload!.Entity);
        Assert.Equal("dani", Assert.Single(result.Payload.Annotations).Author);
    }

    [Fact]
    public async Task HandleAsync_TrimsWhitespace_BeforeStoringAndBounding()
    {
        var handler = new MeshAnnotationsMessageHandler(new MeshAnnotationPublisher(Store()));

        var result = await handler.HandleAsync(Request(entity: "  topic:t  ", author: "  dani  ", text: "  note  "));

        var note = Assert.Single(result.Payload!.Annotations);
        Assert.Equal("topic:t", note.Entity);
        Assert.Equal("dani", note.Author);
        Assert.Equal("note", note.Text);
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootDirectory))
        {
            Directory.Delete(_rootDirectory, true);
        }
    }
}
