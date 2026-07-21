using System.Security.Cryptography;
using System.Text;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.Messages.Mappers;

namespace Benzene.Idempotency;

/// <summary>
/// The default <see cref="IIdempotencyKeyStrategy{TContext}"/>: prefers a caller-supplied key from
/// the configured header, and otherwise (when <see cref="IdempotencyOptions.HashBodyWhenNoHeader"/>
/// is enabled) derives a deterministic key by hashing the message topic and body, so identical
/// redeliveries produce the same key.
/// </summary>
/// <typeparam name="TContext">The transport-specific message context type.</typeparam>
public class HeaderOrBodyHashIdempotencyKeyStrategy<TContext> : IIdempotencyKeyStrategy<TContext>
{
    private readonly IMessageHeadersGetter<TContext> _headersGetter;
    private readonly IMessageBodyGetter<TContext> _bodyGetter;
    private readonly IMessageTopicGetter<TContext> _topicGetter;
    private readonly IdempotencyOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="HeaderOrBodyHashIdempotencyKeyStrategy{TContext}"/> class.
    /// </summary>
    public HeaderOrBodyHashIdempotencyKeyStrategy(
        IMessageHeadersGetter<TContext> headersGetter,
        IMessageBodyGetter<TContext> bodyGetter,
        IMessageTopicGetter<TContext> topicGetter,
        IdempotencyOptions options)
    {
        _headersGetter = headersGetter;
        _bodyGetter = bodyGetter;
        _topicGetter = topicGetter;
        _options = options;
    }

    /// <inheritdoc />
    public string? GetKey(TContext context)
    {
        var headers = _headersGetter.GetHeaders(context);
        if (headers != null
            && TryGetHeader(headers, _options.HeaderName, out var headerKey)
            && !string.IsNullOrWhiteSpace(headerKey))
        {
            return _options.KeyPrefix + headerKey;
        }

        if (!_options.HashBodyWhenNoHeader)
        {
            return null;
        }

        var topic = _topicGetter.GetTopic(context);
        var id = topic?.Id ?? "";
        var version = topic?.Version ?? "";
        var body = _bodyGetter.GetBody(context) ?? "";

        // Length-prefix each field so distinct (id, version, body) triples can't collide through
        // separator ambiguity - e.g. id="order"/version="v2:create" and id="order:v2"/version="create"
        // used to flatten to the same "order:v2:create\n..." string, hashing identical and dropping one
        // message as a false duplicate. With lengths, the field boundaries are unambiguous.
        return _options.KeyPrefix + ComputeHash($"{id.Length}:{id}|{version.Length}:{version}|{body.Length}:{body}");
    }

    // Header keys are case-insensitive on read regardless of the concrete IMessageHeadersGetter's
    // dictionary comparer (wire-contracts.md §2). Without this, a canonically-cased header (e.g.
    // "Idempotency-Key") in an ordinal dictionary would be missed and the caller's explicit key
    // silently ignored in favour of body-hashing.
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

    private static string ComputeHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }
}
