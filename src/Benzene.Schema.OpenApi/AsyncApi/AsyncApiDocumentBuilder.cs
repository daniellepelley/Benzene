using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandlers.Info;
using Benzene.Abstractions.Messages;
using Benzene.Schema.OpenApi.Abstractions;
using ByteBard.AsyncAPI;
using ByteBard.AsyncAPI.Models;
using Microsoft.OpenApi.Models;

namespace Benzene.Schema.OpenApi.AsyncApi;

/// <summary>
/// Builds an AsyncAPI <b>3.0</b> document from a service's handlers, broadcast events, and message
/// senders.
/// </summary>
/// <remarks>
/// AsyncAPI 3.0 separates <em>channels</em> (addressable message containers) from <em>operations</em>
/// (what the application does with them), and names the direction explicitly with
/// <c>action: receive</c> / <c>action: send</c> from the application's perspective — so, unlike 2.x's
/// counter-intuitive publish/subscribe, there is no ambiguity. A handler <b>receives</b> its request
/// and its reply is modelled with the native <c>reply</c> object (rather than a second, loosely-linked
/// channel); broadcast events and egress message-senders are things the application <b>sends</b>.
/// </remarks>
public class AsyncApiDocumentBuilder :
    IConsumesMessageHandlerDefinitions<AsyncApiDocumentBuilder>,
    IConsumesBroadcastEventsDefinitions<AsyncApiDocumentBuilder>,
    IConsumesMessageSenderDefinitions<AsyncApiDocumentBuilder>,
    IConsumesApplicationInfo<AsyncApiDocumentBuilder>,
    IProducesJson,
    IProducesYaml
{
    /// <summary>
    /// The default suffix appended to a handled topic to name its reply channel's address
    /// (<c>&lt;topic&gt;:response</c>), e.g. <c>shipping:get-all:response</c>. Override per-app via
    /// <see cref="AsyncApiSpecOptions.ResponseTopicSuffix"/>.
    /// </summary>
    public const string DefaultResponseTopicSuffix = "response";

    private readonly ISchemaBuilder _schemaBuilder = new SchemaBuilder();
    private readonly string _responseTopicSuffix;
    private AsyncApiInfo _openApiInfo = new();
    private readonly List<AsyncApiTag> _tags = new();
    private readonly Dictionary<string, AsyncApiChannel> _channels = new();
    private readonly Dictionary<string, AsyncApiOperation> _operations = new();

    public AsyncApiDocumentBuilder(ISchemaBuilder? schemaBuilder = null, string? responseTopicSuffix = null)
    {
        if (schemaBuilder != null)
        {
            _schemaBuilder = schemaBuilder;
        }

        _responseTopicSuffix = string.IsNullOrWhiteSpace(responseTopicSuffix)
            ? DefaultResponseTopicSuffix
            : responseTopicSuffix;
    }

    public AsyncApiDocument Build()
    {
        _openApiInfo.Tags = _tags;

        var schemas = new Dictionary<string, AsyncApiMultiFormatSchema>();
        foreach (var schema in _schemaBuilder.Build())
        {
            var mapped = Mapper.Map(schema.Value);
            if (mapped != null)
            {
                schemas[schema.Key] = new AsyncApiMultiFormatSchema { Schema = mapped };
            }
        }

        return new AsyncApiDocument
        {
            // Document-root `id` and the content type every Benzene message body uses unless overridden.
            Id = BuildId(_openApiInfo.Title),
            DefaultContentType = "application/json",
            Info = _openApiInfo,
            Channels = _channels,
            Operations = _operations,
            Components = new AsyncApiComponents { Schemas = schemas }
        };
    }

    private static string BuildId(string? title)
    {
        var slug = new string((title ?? string.Empty).Trim().ToLowerInvariant()
            .Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray()).Trim('-');
        return string.IsNullOrEmpty(slug) ? "urn:benzene:service" : $"urn:benzene:service:{slug}";
    }

    public AsyncApiDocumentBuilder AddApplicationInfo(IApplicationInfo applicationInfo)
    {
        return AddInfo(new AsyncApiInfo
        {
            Title = applicationInfo.Name,
            Description = applicationInfo.Description,
            Version = applicationInfo.Version
        });
    }

    public AsyncApiDocumentBuilder AddInfo(AsyncApiInfo openApiInfo)
    {
        _openApiInfo = openApiInfo;
        return this;
    }

    public AsyncApiDocumentBuilder AddTag(AsyncApiTag openApiTag)
    {
        _tags.Add(openApiTag);
        return this;
    }

    public AsyncApiDocumentBuilder AddMessageHandlerDefinitions(IMessageHandlerDefinition[] messageHandlerDefinitions)
    {
        var messageDefinitionsDictionary = messageHandlerDefinitions.GroupBy(x => x.Topic.Id)
            .ToDictionary(x => x.Key, x => x.ToArray());

        foreach (var messageHandlerDefinition in messageDefinitionsDictionary)
        {
            AddMessageHandlerDefinition(messageHandlerDefinition.Key, messageHandlerDefinition.Value);
        }
        return this;
    }

    public void AddMessageHandlerDefinition(string topic, IMessageHandlerDefinition[] messageHandlerDefinitions)
    {
        // The application RECEIVES the request on the topic channel; the reply it sends back is
        // modelled with AsyncAPI 3.0's native `reply` (pointing at the `<topic>:<suffix>` channel).
        var requestChannelKey = GetOrAddChannel(topic);
        var requestChannel = _channels[requestChannelKey];
        var requestMessageRefs = messageHandlerDefinitions
            .Select(x => MessageRef(requestChannelKey, AddMessage(requestChannel, x.RequestType, MessageName(topic, x.Topic.Version))))
            .ToList();

        var replyChannelKey = GetOrAddChannel($"{topic}:{_responseTopicSuffix}");
        var replyChannel = _channels[replyChannelKey];
        var replyMessageRefs = messageHandlerDefinitions
            .Select(x => MessageRef(replyChannelKey, AddMessage(replyChannel, x.ResponseType, MessageName(topic, x.Topic.Version))))
            .ToList();

        AddOperation(topic, new AsyncApiOperation
        {
            Action = AsyncApiAction.Receive,
            Channel = new AsyncApiChannelReference($"#/channels/{requestChannelKey}"),
            Messages = requestMessageRefs,
            Reply = new AsyncApiOperationReply
            {
                Channel = new AsyncApiChannelReference($"#/channels/{replyChannelKey}"),
                Messages = replyMessageRefs
            }
        });
    }

    public AsyncApiDocumentBuilder AddBroadcastEventDefinitions(IMessageDefinition[] messageDefinitions)
    {
        foreach (var messageDefinition in messageDefinitions)
        {
            AddBroadcastEventDefinition(messageDefinition);
        }
        return this;
    }

    public AsyncApiDocumentBuilder AddBroadcastEventDefinition(IMessageDefinition messageDefinition)
    {
        AddSendOnly(messageDefinition.Topic.Id, messageDefinition.RequestType);
        return this;
    }

    public AsyncApiDocumentBuilder AddEventDefinition(string topic, string typeName, OpenApiSchema schema)
    {
        var channelKey = GetOrAddChannel(topic);
        var messageKey = AddMessage(_channels[channelKey], SanitizeKey(typeName), CreateMessage(topic, AddSchema(typeName, schema)));
        AddOperation(topic, new AsyncApiOperation
        {
            Action = AsyncApiAction.Send,
            Channel = new AsyncApiChannelReference($"#/channels/{channelKey}"),
            Messages = new List<AsyncApiMessageReference> { MessageRef(channelKey, messageKey) }
        });
        return this;
    }

    public AsyncApiDocumentBuilder AddMessageSenderDefinitions(IMessageDefinition[] messageDefinitions)
    {
        foreach (var messageDefinition in messageDefinitions)
        {
            AddMessageSenderDefinition(messageDefinition);
        }

        return this;
    }

    public AsyncApiDocumentBuilder AddMessageSenderDefinition(IMessageDefinition messageDefinition)
    {
        AddSendOnly(messageDefinition.Topic.Id, messageDefinition.RequestType);
        return this;
    }

    // A message the application produces/sends outbound (a broadcast event or an egress send) ⇒
    // action: send, no reply.
    private void AddSendOnly(string topic, Type payloadType)
    {
        var channelKey = GetOrAddChannel(topic);
        var messageKey = AddMessage(_channels[channelKey], payloadType, MessageName(topic, string.Empty));
        AddOperation(topic, new AsyncApiOperation
        {
            Action = AsyncApiAction.Send,
            Channel = new AsyncApiChannelReference($"#/channels/{channelKey}"),
            Messages = new List<AsyncApiMessageReference> { MessageRef(channelKey, messageKey) }
        });
    }

    // Returns the channels-map KEY for the given topic address, creating the channel if needed. The
    // real topic (which can contain ':' etc.) is kept as the channel's `address`; the map key is
    // sanitized because AsyncAPI 3.0 requires channel/operation keys to match ^[A-Za-z0-9.\-_]+$.
    private string GetOrAddChannel(string address)
    {
        foreach (var existing in _channels)
        {
            if (existing.Value.Address == address)
            {
                return existing.Key;
            }
        }

        var key = UniqueKey(SanitizeKey(address), _channels.ContainsKey);
        _channels[key] = new AsyncApiChannel { Address = address, Messages = new Dictionary<string, AsyncApiMessage>() };
        return key;
    }

    private void AddOperation(string topic, AsyncApiOperation operation)
    {
        _operations[UniqueKey(SanitizeKey(topic), _operations.ContainsKey)] = operation;
    }

    private static string UniqueKey(string key, Func<string, bool> exists)
    {
        var unique = key;
        var suffix = 2;
        while (exists(unique))
        {
            unique = $"{key}_{suffix++}";
        }

        return unique;
    }

    // AsyncAPI 3.0 restricts channels/operations/message map keys to ^[A-Za-z0-9.\-_]+$.
    private static string SanitizeKey(string value)
    {
        var chars = (value ?? string.Empty).Select(c =>
            (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '.' || c == '-' || c == '_'
                ? c : '_').ToArray();
        var key = new string(chars);
        return string.IsNullOrEmpty(key) ? "channel" : key;
    }

    private string AddMessage(AsyncApiChannel channel, Type payloadType, string messageName) =>
        AddMessage(channel, SanitizeKey(payloadType.Name), CreateMessage(messageName, AddSchema(payloadType)));

    private static string AddMessage(AsyncApiChannel channel, string key, AsyncApiMessage message)
    {
        var unique = UniqueKey(key, channel.Messages.ContainsKey);
        channel.Messages[unique] = message;
        return unique;
    }

    private static AsyncApiMessageReference MessageRef(string channelKey, string messageKey) =>
        new($"#/channels/{channelKey}/messages/{messageKey}");

    private static string MessageName(string topic, string version) =>
        string.IsNullOrEmpty(version) ? topic : $"{topic} v{version}";

    private static AsyncApiMessage CreateMessage(string name, AsyncApiJsonSchema? payload)
    {
        return new AsyncApiMessage
        {
            Name = name,
            Title = name,
            ContentType = "application/json",
            Payload = new AsyncApiMultiFormatSchema { Schema = payload }
        };
    }

    public AsyncApiJsonSchema? AddSchema(string key, OpenApiSchema openApiSchema)
    {
        return Mapper.Map(_schemaBuilder.AddSchema(key, openApiSchema));
    }

    public AsyncApiJsonSchema? AddSchema(Type type)
    {
        return Mapper.Map(_schemaBuilder.AddSchema(type));
    }

    public string GenerateJson()
    {
        return Build().SerializeAsJson(AsyncApiVersion.AsyncApi3_0);
    }

    public string GenerateYaml()
    {
        return Build().SerializeAsYaml(AsyncApiVersion.AsyncApi3_0);
    }
}
