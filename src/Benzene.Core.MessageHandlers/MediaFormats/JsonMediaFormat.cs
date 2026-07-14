using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.MediaFormats;
using Benzene.Abstractions.Serialization;
using Benzene.Core.MessageHandlers.Serialization;

namespace Benzene.Core.MessageHandlers.MediaFormats;

/// <summary>
/// The process default <see cref="IMediaFormat{TContext}"/>, wrapping the shared
/// <see cref="JsonSerializer"/> singleton. Injected directly into <see cref="MediaFormatNegotiator{TContext}"/>
/// as its fallback rather than registered as one of the negotiated <c>IMediaFormat{TContext}</c>
/// candidates - <see cref="CanRead"/>/<see cref="CanWrite"/> always return <c>false</c> since it's
/// never "discovered" through the matching loop, it's the format used when nothing else matches.
/// </summary>
/// <typeparam name="TContext">The transport-specific context type this format can apply to.</typeparam>
public class JsonMediaFormat<TContext> : IMediaFormat<TContext>
{
    private readonly JsonSerializer _jsonSerializer;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonMediaFormat{TContext}"/> class.
    /// </summary>
    /// <param name="jsonSerializer">The shared JSON serializer this format wraps.</param>
    public JsonMediaFormat(JsonSerializer jsonSerializer)
    {
        _jsonSerializer = jsonSerializer;
    }

    /// <inheritdoc />
    public string ContentType => "application/json";

    /// <inheritdoc />
    public bool CanRead(TContext context, IServiceResolver serviceResolver) => false;

    /// <inheritdoc />
    public bool CanWrite(TContext context, IServiceResolver serviceResolver) => false;

    /// <inheritdoc />
    public ISerializer GetSerializer(IServiceResolver serviceResolver) => _jsonSerializer;
}
