using Benzene.Abstractions.Messages;
using Benzene.Abstractions.Messages.Mappers;
using Benzene.Abstractions.MessageHandlers.Mappers;

namespace Benzene.Core.MessageHandlers;

/// <summary>
/// Default <see cref="IMessageVersionGetter{TContext}"/>: reads the payload schema version from the
/// context's header dictionary (<see cref="IMessageHeadersGetter{TContext}"/>), trying each name in
/// <see cref="HeaderNames"/> in order and returning the first one present.
/// </summary>
/// <typeparam name="TContext">
/// The transport-specific context type. No special shape is required beyond what
/// <see cref="IMessageHeadersGetter{TContext}"/> already needs, so this one implementation serves
/// every transport that has no richer version signal of its own (docs/specification/versioning.md
/// §2.3) - HTTP layers a route-parameter check in front of an instance of this class instead of
/// writing its own header scan.
/// </typeparam>
public class HeaderMessageVersionGetter<TContext> : IMessageVersionGetter<TContext>
{
    /// <summary>The default, ordered header-name fallback (docs/specification/versioning.md §2.1). The
    /// primary name is <see cref="MessageVersionHeaders.Default"/>, the same name the outbound helpers write.</summary>
    public static readonly IReadOnlyList<string> DefaultHeaderNames = [MessageVersionHeaders.Default, "version", "x-version"];

    private readonly IMessageHeadersGetter<TContext> _headersGetter;
    private readonly IReadOnlyList<string> _headerNames;

    /// <summary>
    /// Initializes a new instance of the <see cref="HeaderMessageVersionGetter{TContext}"/> class.
    /// </summary>
    /// <param name="headersGetter">Extracts the header dictionary from the context.</param>
    /// <param name="headerNames">
    /// The header names to try, in order; the first one present in the header dictionary wins. An
    /// application with a pre-existing, differently-meaning <c>version</c>/<c>x-version</c> header
    /// MUST narrow or replace this list (docs/specification/versioning.md §2.1). Defaults to
    /// <see cref="DefaultHeaderNames"/> when not supplied.
    /// </param>
    public HeaderMessageVersionGetter(IMessageHeadersGetter<TContext> headersGetter, IReadOnlyList<string>? headerNames = null)
    {
        _headersGetter = headersGetter;
        _headerNames = headerNames ?? DefaultHeaderNames;
    }

    /// <inheritdoc />
    public string? GetVersion(TContext context)
    {
        var headers = _headersGetter.GetHeaders(context);
        if (headers == null)
        {
            return null;
        }

        // Matched case-insensitively regardless of the concrete IMessageHeadersGetter's dictionary
        // comparer (wire-contracts.md §2: "header keys are case-insensitive on read").
        foreach (var headerName in _headerNames)
        {
            foreach (var header in headers)
            {
                if (string.Equals(header.Key, headerName, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(header.Value))
                {
                    return header.Value;
                }
            }
        }

        return null;
    }
}
