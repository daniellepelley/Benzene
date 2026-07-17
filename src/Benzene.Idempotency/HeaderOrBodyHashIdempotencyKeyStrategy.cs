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
            && headers.TryGetValue(_options.HeaderName, out var headerKey)
            && !string.IsNullOrWhiteSpace(headerKey))
        {
            return _options.KeyPrefix + headerKey;
        }

        if (!_options.HashBodyWhenNoHeader)
        {
            return null;
        }

        var topic = _topicGetter.GetTopic(context);
        var topicId = topic == null ? "" : $"{topic.Id}:{topic.Version}";
        var body = _bodyGetter.GetBody(context) ?? "";

        return _options.KeyPrefix + ComputeHash(topicId + "\n" + body);
    }

    private static string ComputeHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }
}
