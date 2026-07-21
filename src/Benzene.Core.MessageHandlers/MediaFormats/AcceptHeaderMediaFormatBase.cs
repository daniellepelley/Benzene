using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.MediaFormats;
using Benzene.Abstractions.Messages.Mappers;
using Benzene.Abstractions.Serialization;
using Benzene.Core.Messages.Helper;

namespace Benzene.Core.MessageHandlers.MediaFormats;

/// <summary>
/// Base <see cref="IMediaFormat{TContext}"/> implementation for header-negotiated formats: reads are
/// selected by a <c>content-type</c> match, writes by an <c>accept</c> match, both tolerant of
/// <c>;</c>-delimited parameters and casing via <see cref="MediaType.Matches"/>.
/// </summary>
/// <typeparam name="TContext">The transport-specific context type this format can apply to.</typeparam>
/// <remarks>
/// <c>accept</c> is split on <c>,</c> and each token is compared individually, so a multi-value header
/// (<c>"application/json, application/xml;q=0.9"</c>) matches whichever registered format's content
/// type appears anywhere in the list. A bare <c>*/*</c> token intentionally does <b>not</b> match any
/// specific format - it signals "no preference", which the negotiator already expresses by falling
/// back to the request's read format (see <see cref="IMediaFormatNegotiator{TContext}.SelectWrite"/>);
/// treating it as a positive match here would make every registered format hit-first-wins on ordering
/// again, exactly what R3 (media-format unification) set out to remove.
/// </remarks>
public abstract class AcceptHeaderMediaFormatBase<TContext> : IMediaFormat<TContext>
{
    private const string ContentTypeHeader = "content-type";
    private const string AcceptHeader = "accept";

    /// <inheritdoc />
    public abstract string ContentType { get; }

    /// <inheritdoc />
    public abstract ISerializer GetSerializer(IServiceResolver serviceResolver);

    /// <inheritdoc />
    public bool CanRead(TContext context, IServiceResolver serviceResolver)
    {
        return HeaderMatches(context, serviceResolver, ContentTypeHeader);
    }

    /// <inheritdoc />
    public bool CanWrite(TContext context, IServiceResolver serviceResolver)
    {
        var headers = GetHeaders(context, serviceResolver);
        if (headers == null || !TryGetHeader(headers, AcceptHeader, out var accept) || string.IsNullOrEmpty(accept))
        {
            return false;
        }

        return accept.Split(',').Any(token => MediaType.Matches(token, ContentType));
    }

    private bool HeaderMatches(TContext context, IServiceResolver serviceResolver, string headerKey)
    {
        var headers = GetHeaders(context, serviceResolver);
        return headers != null
               && TryGetHeader(headers, headerKey, out var value)
               && MediaType.Matches(value, ContentType);
    }

    // Header keys are case-insensitive on read regardless of the concrete IMessageHeadersGetter's
    // dictionary comparer (wire-contracts.md §2), matching HeaderMessageVersionGetter. The fast path
    // hits when the comparer is already case-insensitive or the key is lower-case as SHOULD-written.
    private static bool TryGetHeader(IDictionary<string, string> headers, string key, out string? value)
    {
        if (headers.TryGetValue(key, out value))
        {
            return true;
        }

        foreach (var header in headers)
        {
            if (string.Equals(header.Key, key, StringComparison.OrdinalIgnoreCase))
            {
                value = header.Value;
                return true;
            }
        }

        value = null;
        return false;
    }

    private static IDictionary<string, string>? GetHeaders(TContext context, IServiceResolver serviceResolver)
    {
        return serviceResolver.GetService<IMessageHeadersGetter<TContext>>().GetHeaders(context);
    }
}
