using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Abstractions.Serialization;
using Benzene.Core.MessageHandlers.BenzeneMessage;
using Benzene.Core.Messages.BenzeneMessage;
using Benzene.Core.Middleware;

namespace Benzene.Azure.Function.QueueStorage;

/// <summary>
/// Routes a Queue Storage message whose text deserializes into a <see cref="BenzeneMessageRequest"/>
/// to the direct-message middleware pipeline.
/// </summary>
/// <remarks>
/// Added to the Queue Storage pipeline by <see cref="Extensions.UseBenzeneMessage(IMiddlewarePipelineBuilder{QueueStorageContext}, Action{IMiddlewarePipelineBuilder{BenzeneMessageContext}})"/>.
/// It only handles the message if its text deserializes into a <see cref="BenzeneMessageRequest"/>
/// with a non-null topic; otherwise it defers to the next middleware. Mirrors
/// <c>BenzeneMessageEventHubHandler</c> - like Event Hub events (and unlike Service Bus messages),
/// Queue Storage messages have no transport properties for a topic to ride on, so the envelope in
/// the body is where routing comes from.
/// </remarks>
public class BenzeneMessageQueueStorageHandler : MiddlewareRouter<BenzeneMessageRequest, QueueStorageContext>
{
    private readonly BenzeneMessageResultApplication _directMessageApplication;
    private readonly ISerializer _serializer;

    /// <summary>
    /// Initializes a new instance of the <see cref="BenzeneMessageQueueStorageHandler"/> class.
    /// </summary>
    /// <param name="pipeline">The direct-message middleware pipeline to dispatch matching messages to.</param>
    /// <param name="serviceResolver">The service resolver for the current invocation scope.</param>
    public BenzeneMessageQueueStorageHandler(
        IMiddlewarePipeline<BenzeneMessageContext> pipeline,
        IServiceResolver serviceResolver)
        : base(serviceResolver)
    {
        _serializer = serviceResolver.GetService<ISerializer>();
        _directMessageApplication = new BenzeneMessageResultApplication(pipeline);
    }

    /// <summary>
    /// Determines whether the deserialized request looks like a direct Benzene message.
    /// </summary>
    /// <param name="request">The deserialized request.</param>
    /// <returns>True if the request has a non-null topic; otherwise, false.</returns>
    protected override bool CanHandle(BenzeneMessageRequest request)
    {
        return request?.Topic != null;
    }

    /// <summary>
    /// Handles the message by running it through the direct-message application and surfacing the inner
    /// handler's result onto the outer <see cref="QueueStorageContext.MessageResult"/>, so
    /// <see cref="QueueStorageOptions.RaiseOnFailureStatus"/> escalation sees a failure that occurred
    /// inside the (response-suppressed) envelope pipeline.
    /// </summary>
    /// <param name="request">The deserialized Benzene message request.</param>
    /// <param name="context">The Queue Storage context for this invocation.</param>
    /// <param name="serviceResolverFactory">The service resolver factory for the current invocation.</param>
    protected override async Task HandleFunction(BenzeneMessageRequest request, QueueStorageContext context, IServiceResolverFactory serviceResolverFactory)
    {
        context.MessageResult = await _directMessageApplication.HandleAsync(request, serviceResolverFactory);
    }

    /// <summary>
    /// Attempts to deserialize the Queue Storage message text into a <see cref="BenzeneMessageRequest"/>.
    /// </summary>
    /// <param name="context">The Queue Storage context to extract the request from.</param>
    /// <returns>The deserialized request, or <c>default</c> if deserialization fails.</returns>
    protected override BenzeneMessageRequest TryExtractRequest(QueueStorageContext context)
    {
        try
        {
            return _serializer.Deserialize<BenzeneMessageRequest>(context.Message.MessageText);
        }
        catch
        {
            return default;
        }
    }
}
