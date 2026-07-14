using Benzene.Core.Messages.Helper;

namespace Benzene.Core.Messages.Predicates;

/// <summary>
/// <see cref="HeaderContextPredicate{TContext}"/> for matching a media-type header (e.g.
/// <c>content-type</c>) tolerant of <c>;</c>-delimited parameters and casing, via
/// <see cref="MediaType.Matches"/> — unlike the base class's exact-equality default,
/// <c>"application/xml; charset=utf-8"</c> matches a predicate constructed for
/// <c>"application/xml"</c>.
/// </summary>
/// <typeparam name="TContext">The transport-specific context type this predicate can apply to.</typeparam>
public class MediaTypeHeaderContextPredicate<TContext> : HeaderContextPredicate<TContext>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MediaTypeHeaderContextPredicate{TContext}"/> class.
    /// </summary>
    /// <param name="headerKey">The header name to check (e.g. <c>"content-type"</c>).</param>
    /// <param name="mediaType">The target media type to match, ignoring parameters and casing.</param>
    public MediaTypeHeaderContextPredicate(string headerKey, string mediaType)
        : base(headerKey, mediaType)
    { }

    /// <inheritdoc />
    protected override bool IsMatch(string actualValue, string expectedValue)
    {
        return MediaType.Matches(actualValue, expectedValue);
    }
}
