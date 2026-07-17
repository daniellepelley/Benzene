using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService.Model;
using Benzene.Abstractions.Middleware;
using Benzene.Abstractions.Results;
using Benzene.Abstractions.Serialization;
using Benzene.Clients;
using Benzene.Results;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Clients.Aws.Sns;

/// <summary>
/// Converts between an outbound <see cref="OutboundContext"/> and an <see cref="SnsSendMessageContext"/>,
/// so an outbound route (<c>OutboundRoutingBuilder.Route</c>) can publish via SNS. The
/// <see cref="OutboundContext"/> counterpart of <see cref="SnsContextConverter{T}"/> - see
/// <c>work/benzene-clients-redesign-plan.md</c> §3.
/// </summary>
/// <remarks>
/// SNS has no request/response semantics beyond a publish acknowledgement, so the response this
/// converter produces is always <see cref="IBenzeneResult{Void}"/> - a topic routed here must be
/// sent via <c>IBenzeneMessageSender.SendAsync&lt;TRequest,Void&gt;</c>.
/// </remarks>
public class OutboundSnsContextConverter : IContextConverter<OutboundContext, SnsSendMessageContext>
{
    private readonly ISerializer _serializer;
    private readonly string _topicArn;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboundSnsContextConverter"/> class, using the
    /// default JSON serializer.
    /// </summary>
    /// <param name="topicArn">The ARN of the SNS topic to publish to.</param>
    public OutboundSnsContextConverter(string topicArn)
        : this(topicArn, new JsonSerializer())
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboundSnsContextConverter"/> class.
    /// </summary>
    /// <param name="topicArn">The ARN of the SNS topic to publish to.</param>
    /// <param name="serializer">The serializer used to serialize the message payload.</param>
    public OutboundSnsContextConverter(string topicArn, ISerializer serializer)
    {
        _topicArn = topicArn;
        _serializer = serializer;
    }

    /// <summary>
    /// Builds an SNS publish request from the outbound context.
    /// </summary>
    /// <param name="contextIn">The outbound context to convert.</param>
    /// <returns>A task that resolves to the SNS send message context.</returns>
    public Task<SnsSendMessageContext> CreateRequestAsync(OutboundContext contextIn)
    {
        var messageAttributes = new Dictionary<string, MessageAttributeValue>();
        foreach (var header in contextIn.Headers)
        {
            messageAttributes[header.Key] = new MessageAttributeValue { StringValue = header.Value, DataType = "String" };
        }

        return Task.FromResult(new SnsSendMessageContext(new PublishRequest
        {
            TopicArn = _topicArn,
            Message = _serializer.Serialize(contextIn.Request),
            MessageAttributes = messageAttributes
        }));
    }

    /// <summary>
    /// Maps the SNS publish response's HTTP status code back onto the outbound context as an
    /// <see cref="IBenzeneResult{Void}"/>.
    /// </summary>
    /// <param name="contextIn">The outbound context to update with the result.</param>
    /// <param name="contextOut">The SNS send message context containing the response.</param>
    /// <returns>A completed task.</returns>
    public Task MapResponseAsync(OutboundContext contextIn, SnsSendMessageContext contextOut)
    {
        contextIn.Response = contextOut.Response.HttpStatusCode.Convert<Void>();
        return Task.CompletedTask;
    }
}
