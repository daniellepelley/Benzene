using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.MediaFormats;

namespace Benzene.Core.MessageHandlers.MediaFormats;

/// <summary>
/// Default <see cref="IMediaFormatNegotiator{TContext}"/> implementation. Registered scoped (one
/// instance per message), so its memoized selections are safe: every caller within the same message
/// negotiates against the same single <c>TContext</c> instance flowing through the whole pipeline, so
/// caching the first computed decision for the lifetime of this instance is correct, not just an
/// optimization - it's also what guarantees <see cref="SelectWrite"/> and <see cref="SelectRead"/> are
/// each evaluated exactly once per message regardless of how many response/request-mapping
/// collaborators ask.
/// </summary>
/// <typeparam name="TContext">The transport-specific context type this negotiator applies to.</typeparam>
public class MediaFormatNegotiator<TContext> : IMediaFormatNegotiator<TContext>
{
    private readonly IMediaFormat<TContext>[] _formats;
    private readonly JsonMediaFormat<TContext> _defaultFormat;
    private readonly IServiceResolver _serviceResolver;

    private IMediaFormat<TContext>? _selectedRead;
    private IMediaFormat<TContext>? _selectedWrite;

    /// <summary>
    /// Initializes a new instance of the <see cref="MediaFormatNegotiator{TContext}"/> class.
    /// </summary>
    /// <param name="formats">Every registered candidate format to evaluate.</param>
    /// <param name="defaultFormat">The process default format, used when no candidate matches.</param>
    /// <param name="serviceResolver">Resolver passed to each format's applicability checks and serializer resolution.</param>
    public MediaFormatNegotiator(IEnumerable<IMediaFormat<TContext>> formats, JsonMediaFormat<TContext> defaultFormat, IServiceResolver serviceResolver)
    {
        _formats = formats as IMediaFormat<TContext>[] ?? formats.ToArray();
        _defaultFormat = defaultFormat;
        _serviceResolver = serviceResolver;
    }

    /// <inheritdoc />
    public IMediaFormat<TContext> SelectRead(TContext context)
    {
        return _selectedRead ??= _formats.FirstOrDefault(format => format.CanRead(context, _serviceResolver)) ?? _defaultFormat;
    }

    /// <inheritdoc />
    public IMediaFormat<TContext> SelectWrite(TContext context)
    {
        return _selectedWrite ??= _formats.FirstOrDefault(format => format.CanWrite(context, _serviceResolver)) ?? SelectRead(context);
    }
}
